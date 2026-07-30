using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;

namespace DotNetActorFramework.Benchmarks
{
    public class BenchmarkDotNet
    {
        public async Task RunBenchmarkAsync()
        {
            // Run full-scan vs two-choice at 8/64/256 routees
            var fullScanResults = new List<int>();
            var twoChoiceResults = new List<int>();

            // Add results to the lists
            for (int i = 0; i < 8; i++)
            {
                fullScanResults.Add(await FullScanAsync());
                twoChoiceResults.Add(await TwoChoiceAsync());
            }

            // Print the results
            Console.WriteLine("Full-scan results: ");
            foreach (var result in fullScanResults)
            {
                Console.WriteLine(result);
            }
            Console.WriteLine("Two-choice results: ");
            foreach (var result in twoChoiceResults)
            {
                Console.WriteLine(result);
            }
        }
    }
}