using System;
using System.Collections.Generic;

namespace hold_em_smooth_uct
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Loading...");
            LHEPolicy lhePolicy = new LHEPolicy();
            //lhePolicy.Initialize();
            lhePolicy.Load();

            uint nSaves = 24;
            uint episodesPerSave = 500;
            for (uint i = 0; i < nSaves; ++i)
            {
                Console.WriteLine("Round " + i.ToString() + " of " + nSaves.ToString());

                lhePolicy.Update(episodesPerSave);

                Console.WriteLine("Saving...");
                lhePolicy.Save();
                Console.WriteLine("Total iterations saved to date: " + lhePolicy.nEpisodes);
            }

            //uint pos = 0;
            //uint neg = 0;
            //foreach (KeyValuePair<string, InformationState> entry in lhePolicy.policy)
            //{
            //    bool isFold = entry.Key.Substring(entry.Key.Length - 1) == "f";
            //    if (!isFold)
            //    {
            //        if (entry.Value.value < 0)
            //        {
            //            ++neg;
            //        }
            //        else
            //        {
            //            ++pos;
            //        }
            //    }
            //}
            //Console.WriteLine(pos);
            //Console.WriteLine(neg);

            //lhes.Reset();
            //Node node = new Node(lhes);
            //MonteCarleTreeSearch mcts = new MonteCarleTreeSearch(lhePolicy, node, true);

            //for (int i = 0; i < 10; ++i)
            //{
            //    foreach (uint nEpisodes in new uint[] { 500, 1000, 2000, 4000, 8000, 16000, 32000 })
            //    {
            //        var watch = System.Diagnostics.Stopwatch.StartNew();
            //        mcts.Reset();
            //        mcts.Search(nEpisodes);
            //        watch.Stop();
            //        Console.WriteLine(nEpisodes.ToString() + ": " + watch.ElapsedMilliseconds);
            //    }
            //}
        }
    }
}
