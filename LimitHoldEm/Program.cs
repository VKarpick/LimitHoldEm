using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Text;

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

            for (int i = 0; i < 5; ++i)
            {
                state.Reset();
                while (!state.isTerminal)
                {
                    Console.WriteLine(state.holeCards[0]);
                    Console.WriteLine(state.holeCards[1]);
                    Console.WriteLine(state.communityCards);

                    uint bucket = state.Discretize(state.street, state.playerIndex);
                    Console.WriteLine("bucket " + state.street.ToString() + " " + bucket.ToString());
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
                        Console.WriteLine(newKey);
                        double actionValue = lhePolicy.policy[newKey].value;
                        if (state.playerIndex == 1) { actionValue *= -1; }
                        Console.WriteLine(actionName + " " + actionValue.ToString());
                        if (actionValue > bestValue)
                        {
                            bestValue = actionValue;
                            bestAction = action;
                            bestActionName = actionName;
                        }
                    }

                    Console.WriteLine(bestActionName);
                    state = state.Step(bestAction);
                }

                Console.WriteLine("==========================================================================================");
            }
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Loading...");
            LHEPolicy lhePolicy = new LHEPolicy();
            lhePolicy.Initialize();
            //lhePolicy.Load();

            Train(lhePolicy, 1000, 500);
            //Play(lhePolicy);
        }
    }
}
