using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;

namespace DotNetActorFramework.Benchmarks
{
    public class LoadBasedRouter
    {
        private readonly ConcurrentDictionary<int, int> mailboxDepths = new ConcurrentDictionary<int, int>();
        private readonly Random random = new Random();
        public async Task RouteMessageAsync(Message message)
        {
            // Sample two random routees
            var routees = message.Routees.ToList();
            var randomRoute1 = routees[random.Next(routees.Count)];
            var randomRoute2 = routees[random.Next(routees.Count)];
            
            // Route to the less-loaded one
            if (mailboxDepths.TryGetValue(randomRoute1, out int depth1) &&
                mailboxDepths.TryGetValue(randomRoute2, out int depth2))
            {
                if (depth1 < depth2)
                {
                    return randomRoute1;
                }
                else
                {
                    return randomRoute2;
                }
            }
            else
            {
                // If a route has no mailbox depth, consider it as having a depth of 0
                if (!mailboxDepths.TryGetValue(randomRoute1, out _))
                {
                    return randomRoute1;
                }
                else
                {
                    return randomRoute2;
                }
            }
        }
    }
}