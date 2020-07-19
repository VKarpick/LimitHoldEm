using System;
using System.Collections.Generic;

namespace hold_em_smooth_uct
{
    class Node
    {
        public LimitHoldEmState state;
        public List<LimitHoldEmAction> unexploredActions;
        public Node parent;
        public List<Node> children = new List<Node>();
        public string policyKey;
        public Dictionary<string, uint> buckets = new Dictionary<string, uint>();    // memoize buckets to avoid unnecessary discretization

        public Node(LimitHoldEmState state, Node parent = null)
        {
            this.state = state;
            this.parent = parent;
            unexploredActions = this.state.GetActions();

            string[] bucketKeys = new string[2] { state.holeCards[0], state.holeCards[1] };
            uint discStreet = 0;
            if (parent != null)
            {
                buckets = parent.buckets;
                for (int i = 0; i < 2; ++i)
                {
                    bucketKeys[i] += parent.state.communityCards;
                }
                discStreet = parent.state.street;
            }

            if (!buckets.ContainsKey(bucketKeys[state.playerIndex]))
            {
                for (uint i = 0; i < 2; ++i)
                {
                    buckets[bucketKeys[i]] = state.Discretize(discStreet, i);
                }
            }

            policyKey = buckets[bucketKeys[(state.playerIndex == 0) ? 1u : 0]].ToString() + state.previousActions;
        }

        public Node Expand()
        {
            LimitHoldEmAction action = unexploredActions[0];
            unexploredActions.RemoveAt(0);
            LimitHoldEmState newState = state.Step(action);
            Node childNode = new Node(newState, this);
            children.Add(childNode);
            return childNode;
        }
    }

    class MonteCarleTreeSearch
    {
        public LHEPolicy policy;
        public Node root;
        public bool isSmooth;
        private double gamma_;
        private double eta_;
        private double d_;
        private double k_;
        private double c_;

        public MonteCarleTreeSearch(LHEPolicy policy, Node root, bool isSmooth = true, 
            double gamma = 0.1, double eta = 0.9, double d = 0.00005, double k = 0.5, double c = 48.0)
        {
            this.policy = policy;
            this.root = root;
            this.isSmooth = isSmooth;
            gamma_ = gamma;
            eta_ = eta;
            d_ = d;
            k_ = k;
            c_ = c;
        }

        public void Reset()
        {
            LimitHoldEmState state = root.state.Reset();
            root = new Node(state);
        }

        public Node Select()
        {
            Node currentNode = root;
            while (!currentNode.state.isTerminal)
            {
                if (currentNode.unexploredActions.Count == 0)
                {
                    Random random = new Random();
                    double z = random.NextDouble();
                    ulong nVisits = policy.policy[currentNode.policyKey].nVisits;

                    if (isSmooth && z > Math.Max(gamma_, eta_ * Math.Pow(1 + d_ * Math.Sqrt(nVisits), -1)))
                    {
                        currentNode = AverageChild(currentNode);
                    }
                    else
                    {
                        // c uses 48 instead of 24 as in the paper because measurement is in small blinds as opposed to big blinds
                        uint pot = currentNode.state.potContributions[0] + currentNode.state.potContributions[1];
                        double c = Math.Min(c_, pot + k_ * (48 - pot));
                        currentNode = BestChild(currentNode, c);
                    }
                }
                else
                {
                    return currentNode.Expand();
                }
            }
            return currentNode;
        }

        public void Search(uint nSimulations = 10000)
        {
            for (int i = 0; i < nSimulations; ++i)
            {
                Node node = Select();
                double reward = Rollout(node);
                Backpropagate(node, reward);
            }
        }

        public Node BestChild(Node node, double c = 0.0)
        {
            ulong nVisits = policy.policy[node.policyKey].nVisits;

            if (node.state.IsMaxPlayer())
            {
                Node bestChild = node.children[0];
                double bestValue = double.MinValue;
                foreach (Node child in node.children)
                {
                    double value = policy.policy[child.policyKey].value;
                    ulong childVisits = policy.policy[child.policyKey].nVisits;
                    double childValue = value + c * Math.Sqrt((2 * Math.Log10(nVisits) / childVisits));
                    if (childValue > bestValue)
                    {
                        bestValue = childValue;
                        bestChild = child;
                    }
                }

                return bestChild;
            }
            else
            {
                Node worstChild = node.children[0];
                double worstValue = double.MaxValue;
                foreach (Node child in node.children)
                {
                    double value = policy.policy[child.policyKey].value;
                    ulong childVisits = policy.policy[child.policyKey].nVisits;
                    double childValue = value - c * Math.Sqrt((2 * Math.Log10(nVisits) / childVisits));
                    if (childValue < worstValue)
                    {
                        worstValue = childValue;
                        worstChild = child;
                    }
                }

                return worstChild;
            }
        }

        public Node AverageChild(Node node)
        {
            ulong nVisits = policy.policy[node.policyKey].nVisits;
            Random random = new Random();
            int z = random.Next(0, Convert.ToInt32(nVisits));
            ulong totalChildVisits = 0;
            foreach (Node child in node.children)
            {
                ulong childVisits = policy.policy[child.policyKey].nVisits;
                totalChildVisits += childVisits;
                if (totalChildVisits > (ulong)z)
                {
                    return child;
                }
            }

            return node.children[0];
        }

        public double Rollout(Node node)
        {
            LimitHoldEmState currentState = node.state;
            while (!currentState.isTerminal)
            {
                List<LimitHoldEmAction> actions = currentState.GetActions();
                Random random = new Random();
                LimitHoldEmAction action = actions[random.Next(0, actions.Count)];
                currentState = currentState.Step(action);
            }

            return currentState.reward;
        }

        public void Backpropagate(Node node, double reward)
        {
            ++policy.policy[node.policyKey].nVisits;
            policy.policy[node.policyKey].value += (reward - policy.policy[node.policyKey].value) / policy.policy[node.policyKey].nVisits;
            if (node.parent != null)
            {
                Backpropagate(node.parent, reward);
            }
        }
    }
}
