using System;

namespace hold_em_smooth_uct
{
    class Program
    {
        static void Train(LHEPolicy lhePolicy, uint nSaves=100, uint episodesPerSave=500)
        {
            for (uint i = 0; i < nSaves; ++i)
            {
                Console.WriteLine("Round " + i.ToString() + " of " + nSaves.ToString());

                lhePolicy.Update(episodesPerSave);

                Console.WriteLine("Saving...");
                lhePolicy.Save();
                Console.WriteLine("Total iterations saved to date: " + lhePolicy.nEpisodes);
            }
        }

        static void Play(LHEPolicy lhePolicy, uint userIndex=0)
        {
            LimitHoldEmState state = new LimitHoldEmState();
            LimitHoldEmAction selectedAction = LimitHoldEmAction.Call;
            string selectedActionName = "";
            double winnings = 0;

            while (true)
            {
                state.Reset();
                while (!state.isTerminal)
                {
                    if (state.playerIndex == userIndex)
                    {
                        Console.WriteLine("Your cards: " + state.holeCards[userIndex]);
                        Console.WriteLine("Community cards: " + state.communityCards);
                        Console.WriteLine("Total winnings so far: " + winnings);
                        uint pot = state.potContributions[0] + state.potContributions[1];
                        Console.WriteLine("Pot: " + pot);

                        selectedActionName = "";

                        while (selectedActionName != "c" && selectedActionName != "f" && selectedActionName != "r")
                        {
                            selectedActionName = Console.ReadLine();
                            selectedActionName = selectedActionName.ToLower();

                            if (selectedActionName == "q" || selectedActionName == "quit")
                            {
                                Environment.Exit(0);
                            }
                        }
                        switch (selectedActionName)
                        {
                            case "c":
                                selectedAction = LimitHoldEmAction.Call;
                                break;
                            case "f":
                                selectedAction = LimitHoldEmAction.Fold;
                                break;
                            case "r":
                                selectedAction = LimitHoldEmAction.Raise;
                                break;
                        }
                    }
                    else
                    {
                        uint bucket = state.Discretize(state.street, state.playerIndex);
                        string policyKey = bucket.ToString() + state.previousActions;
                        string actionName;
                        LimitHoldEmAction bestAction = LimitHoldEmAction.Call;
                        double bestValue = double.MinValue;
                        string bestActionName = "c";
                        foreach (LimitHoldEmAction action in state.GetActions())
                        {
                            switch (action)
                            {
                                case LimitHoldEmAction.Fold:
                                    actionName = "f";
                                    break;
                                case LimitHoldEmAction.Raise:
                                    actionName = "r";
                                    break;
                                default:
                                    actionName = "c";
                                    break;
                            }

                            string newKey = policyKey + actionName;
                            double actionValue = lhePolicy.policy[newKey].value;
                            if (state.playerIndex == 1) { actionValue *= -1; }

                            if (actionValue > bestValue)
                            {
                                bestValue = actionValue;
                                bestAction = action;
                                bestActionName = actionName;
                            }

                            selectedAction = bestAction;
                            selectedActionName = bestActionName;
                        }

                        Console.WriteLine("\nSmoothUCT: " + selectedActionName + "\n");
                    }

                    state = state.Step(selectedAction);
                }

                uint opponent = (userIndex == 0) ? 1u : 0;
                Console.WriteLine("\nSmoothUCT cards: " + state.holeCards[opponent]);
                double hand_winnings;
                if ((state.reward > 0 && userIndex == 0) || (state.reward < 0 && userIndex == 1))
                {
                    hand_winnings = state.potContributions[opponent];
                    Console.WriteLine("You win " + hand_winnings);
                    winnings += hand_winnings;
                }
                else if (state.reward == 0)
                {
                    Console.WriteLine("Split pot");
                }
                else
                {
                    hand_winnings = state.potContributions[userIndex];
                    Console.WriteLine("SmoothUCT wins " + hand_winnings);
                    winnings -= hand_winnings;
                }

                userIndex = (userIndex + 1) % 2;
                Console.WriteLine("==========================================================================================");
            }
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Loading...");
            LHEPolicy lhePolicy = new LHEPolicy();
            //lhePolicy.Initialize();
            lhePolicy.Load();

            //Train(lhePolicy, 1000, 500);
            Play(lhePolicy);
        }
    }
}
