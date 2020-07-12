using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace hold_em_smooth_uct
{
    public class InformationState
    {
        public double value;
        public ulong nVisits;

        public InformationState(double value = 0.0, ulong nVisits = 0)
        {
            this.value = value;
            this.nVisits = nVisits;
        }
    }

    public class LHEPolicy
    {
        public ulong nEpisodes;

        // key = bucket + previous moves
        // when changing streets, bucket is for initial street
        public Dictionary<string, InformationState> policy = new Dictionary<string, InformationState>();

        public void Initialize()
        {
            LimitHoldEmState state = new LimitHoldEmState();
            state.Reset();
            List<LimitHoldEmState> states = new List<LimitHoldEmState> { (LimitHoldEmState)state.Clone() };
            uint[] nBuckets = new uint[4] { 169, 1000, 500, 200 };

            for (int i = 0; i < 169; ++i)
            {
                policy[i.ToString()] = new InformationState();
            }

            while (states.Count != 0)
            {
                state = states[0];
                states.RemoveAt(0);

                List<LimitHoldEmAction> actions = state.GetActions();
                foreach (LimitHoldEmAction action in actions)
                {
                    LimitHoldEmState newState = state.Step(action);

                    for (int bucket = 0; bucket < nBuckets[state.street]; ++bucket)
                    {
                        policy[bucket.ToString() + newState.previousActions] = new InformationState();
                    }

                    if (!newState.isTerminal)
                    {
                        states.Add(newState);
                    }
                }
            }
        }

        public void Load()
        {
            string path = "..\\..\\..\\Resources\\policy.json";
            string json = File.ReadAllText(path);
            LHEPolicy loadedPolicy = JsonConvert.DeserializeObject<LHEPolicy>(json);
            nEpisodes = loadedPolicy.nEpisodes;
            policy = loadedPolicy.policy;
        }


        public void Save()
        {
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            string path = "..\\..\\..\\Resources\\policy.json";
            File.WriteAllText(path, json);
        }

        public void Update(uint n)
        {
            LimitHoldEmState lhes = new LimitHoldEmState();
            lhes.Reset();
            Node node = new Node(lhes);
            MonteCarleTreeSearch mcts = new MonteCarleTreeSearch(this, node, true);

            for (uint i = 0; i < n; ++i)
            {
                Console.WriteLine("Iteration " + i.ToString() + " of " + n.ToString());
                mcts.Reset();
                mcts.Search();

                ++nEpisodes;
            }
        }
    }
}
