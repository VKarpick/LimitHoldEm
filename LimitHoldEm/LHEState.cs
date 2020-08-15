using HoldemHand;
using System;
using System.Collections.Generic;

namespace hold_em_smooth_uct
{
    public enum LimitHoldEmAction { Call, Raise, Fold };

    public class LimitHoldEmState : ICloneable
    {
        public double reward;    // playerIndex 0 = max player, playerIndex 1 = min player
        public bool isTerminal;

        public List<string> cards = new List<string>();
        public static char[] suits = { 'c', 'd', 'h', 's' };
        public static char[] ranks = { '2', '3', '4', '5', '6', '7', '8', '9', 't', 'j', 'q', 'k', 'a' };

        public uint playerIndex;
        public uint[] potContributions = new uint[2];
        public string[] holeCards = new string[2];
        public string communityCards;
        public uint street;
        public uint nStreetBets;
        public string previousActions;

        public object Clone()
        {
            LimitHoldEmState newState = (LimitHoldEmState)MemberwiseClone();
            newState.cards = new List<string>(cards);
            newState.potContributions = (uint[])potContributions.Clone();
            newState.holeCards = (string[])holeCards.Clone();
            return newState;
        }

        public LimitHoldEmState Reset()
        {
            reward = 0.0;
            isTerminal = false;

            cards.Clear();
            foreach (char suit in suits)
            {
                foreach (char rank in ranks)
                {
                    cards.Add(rank.ToString() + suit.ToString());
                }
            }

            for (uint i = 0; i < 2; ++i)
            {
                holeCards[i] = Deal() + " " + Deal();
                potContributions[i] = i + 1;
            }

            playerIndex = 0;
            communityCards = "";
            street = 0;
            nStreetBets = 0;
            previousActions = "";

            return this;
        }

        public string Deal()
        {
            Random random = new Random();
            int r = random.Next(0, cards.Count);
            string card = cards[r];
            cards[r] = cards[0];
            cards.RemoveAt(0);
            return card;
        }

        public List<LimitHoldEmAction> GetActions()
        {
            // check == calling a bet of 0
            List<LimitHoldEmAction> actions = new List<LimitHoldEmAction> { LimitHoldEmAction.Call };

            if (nStreetBets < 4)    // at most 4 raises allowed
            {
                actions.Add(LimitHoldEmAction.Raise);
            }

            if (potContributions[0] != potContributions[1])    // don't allow fold when check is available
            {
                actions.Add(LimitHoldEmAction.Fold);
            }

            return actions;
        }

        public LimitHoldEmState Step(LimitHoldEmAction action)
        {
            LimitHoldEmState newState = (LimitHoldEmState)Clone();
            uint opponent = (newState.playerIndex == 0) ? 1u : 0;

            switch (action)
            {
                case LimitHoldEmAction.Fold:
                    newState.previousActions += "f";
                    newState.isTerminal = true;
                    newState.reward = newState.potContributions[newState.playerIndex];
                    if (newState.playerIndex == 0)    // max player is folding
                    {
                        newState.reward *= -1;
                    }
                    break;

                case LimitHoldEmAction.Raise:
                    newState.previousActions += "r";
                    ++newState.nStreetBets;
                    newState.potContributions[playerIndex] = newState.potContributions[opponent] + ((newState.street < 2) ? 2u : 4u);
                    newState.playerIndex = opponent;
                    break;

                case LimitHoldEmAction.Call:
                    newState.previousActions += "c";
                    newState.potContributions[newState.playerIndex] = newState.potContributions[opponent];

                    // player checks
                    if (newState.nStreetBets == 0 && 
                        ((newState.street == 0 && newState.playerIndex == 0) || newState.street > 0 && newState.playerIndex == 1))
                    {
                        newState.playerIndex = opponent;
                    }

                    // player calls
                    else
                    {
                        if (newState.street == 0)
                        {
                            // advance from preflop to flop
                            newState.communityCards = newState.Deal() + " " + newState.Deal() + " " + newState.Deal();
                        }
                        
                        else if (newState.street == 1 || newState.street == 2)
                        {
                            // advance from flop to turn or turn to river
                            newState.communityCards += " " + newState.Deal();
                        }

                        else
                        {
                            // end of hand
                            newState.isTerminal = true;

                            ulong boardMask = Hand.ParseHand(newState.communityCards);
                            ulong[] handValue = new ulong[2];
                            for (int i = 0; i < 2; ++i)
                            {
                                handValue[i] = Hand.Evaluate(boardMask | Hand.ParseHand(newState.holeCards[i]), 7);
                            }

                            // ties would set reward to 0.0 but that is already the default
                            if (handValue[0] != handValue[1])
                            {
                                // both players are guaranteed to have contributed same amount
                                newState.reward = newState.potContributions[0];

                                if (handValue[1] > handValue[0])
                                {
                                    newState.reward *= -1;
                                }
                            }
                        }

                        newState.nStreetBets = 0;
                        ++newState.street;
                        newState.playerIndex = 1u;
                    }
                    break;
            }

            return newState;
        }

        public bool IsMaxPlayer()
        {
            return playerIndex == 0;
        }

        public uint Discretize(uint discStreet, uint index)
        {
            if (discStreet == 0)
            {
                string[] handCards = holeCards[index].Split(" ");
                int card1Index = Array.FindIndex(ranks, row => row == handCards[0][0]);
                int card2Index = Array.FindIndex(ranks, row => row == handCards[1][0]);
                string hand = (card1Index > card2Index) ? 
                    handCards[0][0].ToString() + handCards[1][0].ToString() : handCards[1][0].ToString() + handCards[0][0].ToString();
                hand += ((handCards[0][1] == handCards[1][1]) ? "s" : "o");

                switch (hand)
                {
                    case "aao": return 168;
                    case "kko": return 167;
                    case "qqo": return 166;
                    case "jjo": return 165;
                    case "tto": return 164;
                    case "99o": return 163;
                    case "88o": return 162;
                    case "aks": return 161;
                    case "77o": return 160;
                    case "aqs": return 159;
                    case "ajs": return 158;
                    case "ako": return 157;
                    case "ats": return 156;
                    case "aqo": return 155;
                    case "ajo": return 154;
                    case "kqs": return 153;
                    case "66o": return 152;
                    case "a9s": return 151;
                    case "ato": return 150;
                    case "kjs": return 149;
                    case "a8s": return 148;
                    case "kts": return 147;
                    case "kqo": return 146;
                    case "a7s": return 145;
                    case "a9o": return 144;
                    case "kjo": return 143;
                    case "55o": return 142;
                    case "qjs": return 141;
                    case "k9s": return 140;
                    case "a5s": return 139;
                    case "a6s": return 138;
                    case "a8o": return 137;
                    case "kto": return 136;
                    case "qts": return 135;
                    case "a4s": return 134;
                    case "a7o": return 133;
                    case "k8s": return 132;
                    case "a3s": return 131;
                    case "qjo": return 130;
                    case "k9o": return 129;
                    case "a5o": return 128;
                    case "a6o": return 127;
                    case "q9s": return 126;
                    case "k7s": return 125;
                    case "jts": return 124;
                    case "a2s": return 123;
                    case "qto": return 122;
                    case "44o": return 121;
                    case "a4o": return 120;
                    case "k6s": return 119;
                    case "k8o": return 118;
                    case "q8s": return 117;
                    case "a3o": return 116;
                    case "k5s": return 115;
                    case "j9s": return 114;
                    case "q9o": return 113;
                    case "jto": return 112;
                    case "k7o": return 111;
                    case "a2o": return 110;
                    case "k4s": return 109;
                    case "q7s": return 108;
                    case "k6o": return 107;
                    case "k3s": return 106;
                    case "t9s": return 105;
                    case "j8s": return 104;
                    case "33o": return 103;
                    case "q6s": return 102;
                    case "q8o": return 101;
                    case "k5o": return 100;
                    case "j9o": return 99;
                    case "k2s": return 98;
                    case "q5s": return 97;
                    case "t8s": return 96;
                    case "k4o": return 95;
                    case "j7s": return 94;
                    case "q4s": return 93;
                    case "q7o": return 92;
                    case "t9o": return 91;
                    case "j8o": return 90;
                    case "k3o": return 89;
                    case "q6o": return 88;
                    case "q3s": return 87;
                    case "98s": return 86;
                    case "t7s": return 85;
                    case "j6s": return 84;
                    case "k2o": return 83;
                    case "22o": return 82;
                    case "q2s": return 81;
                    case "q5o": return 80;
                    case "j5s": return 79;
                    case "t8o": return 78;
                    case "j7o": return 77;
                    case "q4o": return 76;
                    case "97s": return 75;
                    case "j4s": return 74;
                    case "t6s": return 73;
                    case "j3s": return 72;
                    case "q3o": return 71;
                    case "98o": return 70;
                    case "87s": return 69;
                    case "t7o": return 68;
                    case "j6o": return 67;
                    case "96s": return 66;
                    case "j2s": return 65;
                    case "q2o": return 64;
                    case "t5s": return 63;
                    case "j5o": return 62;
                    case "t4s": return 61;
                    case "97o": return 60;
                    case "86s": return 59;
                    case "j4o": return 58;
                    case "t6o": return 57;
                    case "95s": return 56;
                    case "t3s": return 55;
                    case "76s": return 54;
                    case "j3o": return 53;
                    case "87o": return 52;
                    case "t2s": return 51;
                    case "85s": return 50;
                    case "96o": return 49;
                    case "j2o": return 48;
                    case "t5o": return 47;
                    case "94s": return 46;
                    case "75s": return 45;
                    case "t4o": return 44;
                    case "93s": return 43;
                    case "86o": return 42;
                    case "65s": return 41;
                    case "84s": return 40;
                    case "95o": return 39;
                    case "t3o": return 38;
                    case "92s": return 37;
                    case "76o": return 36;
                    case "74s": return 35;
                    case "t2o": return 34;
                    case "54s": return 33;
                    case "85o": return 32;
                    case "64s": return 31;
                    case "83s": return 30;
                    case "94o": return 29;
                    case "75o": return 28;
                    case "82s": return 27;
                    case "73s": return 26;
                    case "93o": return 25;
                    case "65o": return 24;
                    case "53s": return 23;
                    case "63s": return 22;
                    case "84o": return 21;
                    case "92o": return 20;
                    case "43s": return 19;
                    case "74o": return 18;
                    case "72s": return 17;
                    case "54o": return 16;
                    case "64o": return 15;
                    case "52s": return 14;
                    case "62s": return 13;
                    case "83o": return 12;
                    case "42s": return 11;
                    case "82o": return 10;
                    case "73o": return 9;
                    case "53o": return 8;
                    case "63o": return 7;
                    case "32s": return 6;
                    case "43o": return 5;
                    case "72o": return 4;
                    case "52o": return 3;
                    case "62o": return 2;
                    case "42o": return 1;
                    case "32o": return 0;
                }
                return 0;
            }
            else
            {
                uint[] nBuckets = { 169, 1000, 500, 200, 1 };

                ulong playerMask = Hand.ParseHand(holeCards[index]);
                ulong board = Hand.ParseHand(communityCards);
                long playerWins = 0;
                long count = 0;

                foreach (ulong opponentMask in Hand.Hands(0UL, board | playerMask, 2))
                {
                    foreach (ulong boardMask in Hand.Hands(board, opponentMask | playerMask, 5))
                    {
                        uint playerHandValue = Hand.Evaluate(boardMask | playerMask, 7);
                        uint opponentHandValue = Hand.Evaluate(boardMask | opponentMask, 7);

                        if (playerHandValue > opponentHandValue)
                        {
                            playerWins++;
                        }

                        count++;
                    }
                }

                if (count == 0) { return 0; }
                
                // don't bother squaring
                return (uint)Math.Round(((double)playerWins / (double)count * (nBuckets[discStreet] - 1)), 
                    0, MidpointRounding.AwayFromZero);
            }
        }
    }
}
