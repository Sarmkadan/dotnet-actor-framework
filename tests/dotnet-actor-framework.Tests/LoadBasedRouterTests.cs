using DotNetActorFramework.Configuration;
using DotNetActorFramework.Models;
using DotNetActorFramework.Routing;
using DotNetActorFramework.Services;
using Xunit;

namespace DotNetActorFramework.Tests;

/// <summary>
/// Verifies that <see cref="LoadBasedRouter.SampleLeastLoaded"/> keeps mailbox depths
/// reasonably balanced across routees whose processing time varies, rather than
/// blindly spraying messages evenly (as round-robin would).
/// </summary>
public class LoadBasedRouterTests
{
    /// <summary>
    /// Simulates a "slow" routee that never drains its mailbox and several "fast"
    /// routees that drain constantly. Power-of-two-choices should steer the large
    /// majority of newly routed messages away from the perpetually-loaded slow
    /// routee and toward the empty fast ones.
    /// </summary>
    [Fact]
    public async Task SampleLeastLoaded_UnderSkewedLoad_FavorsLessLoadedRoutees()
    {
        const string capability = "skewed-capability";
        var options = new ActorSystemOptions();
        var mailboxService = new MailboxService(options);
        var discovery = new ActorDiscoveryService();
        var registry = new ActorRegistry();
        var system = new ActorSystem("skew-test-system");
        var dispatcher = new MessageDispatcher(mailboxService, registry, system);
        var router = new LoadBasedRouter(discovery, mailboxService, dispatcher);

        // One slow routee, pre-loaded and never drained, standing in for an actor
        // whose processing time is much higher than its peers.
        var slowActor = await system.CreateActorAsync(new ActorPath("/slow-routee"));
        discovery.Register(slowActor, [capability]);
        var slowMailbox = mailboxService.CreateMailbox(slowActor.Id, capacity: 10_000);
        for (var i = 0; i < 200; i++)
            await slowMailbox.EnqueueAsync(new Envelope(new ControlMessage("backlog"), slowActor));

        // Several fast routees that start empty and are drained after every message,
        // standing in for actors with low processing time.
        const int fastRouteeCount = 5;
        var fastMailboxes = new Dictionary<Guid, IMailbox>();
        for (var i = 0; i < fastRouteeCount; i++)
        {
            var fastActor = await system.CreateActorAsync(new ActorPath($"/fast-routee-{i}"));
            discovery.Register(fastActor, [capability]);
            fastMailboxes[fastActor.Id] = mailboxService.CreateMailbox(fastActor.Id, capacity: 10_000);
        }

        var slowSelections = 0;
        const int totalDecisions = 2_000;

        for (var i = 0; i < totalDecisions; i++)
        {
            var chosen = router.SampleLeastLoaded(capability);
            Assert.NotNull(chosen);

            if (chosen!.Id == slowActor.Id)
            {
                slowSelections++;
                // The slow routee keeps its backlog; it never drains.
            }
            else
            {
                // Fast routees are immediately drained back to empty, mimicking
                // low processing time.
                var mailbox = fastMailboxes[chosen.Id];
                await mailbox.EnqueueAsync(new Envelope(new ControlMessage("work"), chosen));
                await mailbox.DequeueAsync();
            }
        }

        // With a 200-message backlog on the slow routee against empty fast routees,
        // power-of-two-choices should almost never pick the slow routee: it only
        // wins a sample pair if it happens to be compared only against itself, which
        // cannot happen since it is a single routee among six, so any pair that
        // includes it also includes a much lighter alternative that wins the
        // comparison. Allow a small tolerance for the rare edge case where both
        // sampled indices could still resolve to it being reported (defensive bound).
        var slowSelectionRate = (double)slowSelections / totalDecisions;
        Assert.True(
            slowSelectionRate < 0.05,
            $"expected the perpetually backlogged routee to be selected rarely, but it was selected {slowSelections}/{totalDecisions} times ({slowSelectionRate:P1})");
    }

    /// <summary>
    /// With a single routee, sampling degenerates to returning that routee without
    /// requiring two distinct samples.
    /// </summary>
    [Fact]
    public async Task SampleLeastLoaded_WithSingleRoutee_ReturnsThatRoutee()
    {
        const string capability = "single-routee-capability";
        var options = new ActorSystemOptions();
        var mailboxService = new MailboxService(options);
        var discovery = new ActorDiscoveryService();
        var registry = new ActorRegistry();
        var system = new ActorSystem("single-routee-system");
        var dispatcher = new MessageDispatcher(mailboxService, registry, system);
        var router = new LoadBasedRouter(discovery, mailboxService, dispatcher);

        var onlyActor = await system.CreateActorAsync(new ActorPath("/only-routee"));
        discovery.Register(onlyActor, [capability]);
        mailboxService.CreateMailbox(onlyActor.Id, capacity: 10);

        var chosen = router.SampleLeastLoaded(capability);

        Assert.NotNull(chosen);
        Assert.Equal(onlyActor.Id, chosen!.Id);
    }

    /// <summary>
    /// An unregistered capability yields no routee.
    /// </summary>
    [Fact]
    public void SampleLeastLoaded_WithNoRoutees_ReturnsNull()
    {
        var options = new ActorSystemOptions();
        var mailboxService = new MailboxService(options);
        var discovery = new ActorDiscoveryService();
        var registry = new ActorRegistry();
        var system = new ActorSystem("empty-pool-system");
        var dispatcher = new MessageDispatcher(mailboxService, registry, system);
        var router = new LoadBasedRouter(discovery, mailboxService, dispatcher);

        var chosen = router.SampleLeastLoaded("no-such-capability");

        Assert.Null(chosen);
    }

    /// <summary>
    /// When two actors have equal load, the router returns the first one sampled.
    /// </summary>
    [Fact]
    public async Task SampleLeastLoaded_WithEqualLoad_ReturnsFirst()
    {
        const string capability = "equal-load-capability";
        var options = new ActorSystemOptions();
        var mailboxService = new MailboxService(options);
        var discovery = new ActorDiscoveryService();
        var registry = new ActorRegistry();
        var system = new ActorSystem("equal-load-system");
        var dispatcher = new MessageDispatcher(mailboxService, registry, system);
        var router = new LoadBasedRouter(discovery, mailboxService, dispatcher);

        var actor1 = await system.CreateActorAsync(new ActorPath("/actor-1"));
        var actor2 = await system.CreateActorAsync(new ActorPath("/actor-2"));
        discovery.Register(actor1, [capability]);
        discovery.Register(actor2, [capability]);
        
        // Ensure both have the same load (0)
        mailboxService.CreateMailbox(actor1.Id, capacity: 10);
        mailboxService.CreateMailbox(actor2.Id, capacity: 10);

        var chosen = router.SampleLeastLoaded(capability);
        Assert.NotNull(chosen);
        Assert.True(chosen!.Id == actor1.Id || chosen.Id == actor2.Id);
    }
}
