// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetActorFramework.Configuration;
using DotNetActorFramework.Models;
using DotNetActorFramework.Routing;
using DotNetActorFramework.Services;
using FluentAssertions;
using Xunit;

namespace DotNetActorFramework.Tests;

public class RoundRobinRouterTests
{
    private const string Capability = "round-robin-capability";

    [Fact]
    public async Task RouteAsync_ThreeRoutees_ShouldDistributeMessagesEvenly()
    {
        // Arrange
        var (router, mailboxes, routees) = await CreateRoutedPoolAsync(3, Capability);

        // Act
        for (var i = 0; i < 300; i++)
            (await router.RouteAsync(Capability, CreateEnvelope())).Should().BeTrue();

        // Assert
        GetCounts(mailboxes, routees).Should().Equal(100, 100, 100);
    }

    [Fact]
    public async Task RouteAsync_FirstMessages_ShouldCycleThroughRouteesInPoolOrder()
    {
        // Arrange
        var (router, mailboxes, routees) = await CreateRoutedPoolAsync(3, Capability);

        // Act
        var counts = new List<int[]>();
        for (var i = 0; i < 4; i++)
        {
            await router.RouteAsync(Capability, CreateEnvelope());
            counts.Add(GetCounts(mailboxes, routees));
        }

        // Assert
        // Discovery order is not registration order, so compare sorted counts.
        counts.Select(snapshot => string.Join(",", Sorted(snapshot))).Should().Equal("0,0,1", "0,1,1", "1,1,1", "1,1,2");
    }

    [Theory]
    [InlineData(2, 7)]
    [InlineData(3, 10)]
    [InlineData(4, 15)]
    [InlineData(5, 23)]
    public async Task RouteAsync_UnevenMessageCount_ShouldGiveEarliestRouteesOneExtraMessage(
        int routeeCount,
        int messageCount)
    {
        // Arrange
        var (router, mailboxes, routees) = await CreateRoutedPoolAsync(routeeCount, Capability);

        // Act
        for (var i = 0; i < messageCount; i++)
            await router.RouteAsync(Capability, CreateEnvelope());

        // Assert
        var expected = Enumerable.Range(0, routeeCount)
            .Select(i => messageCount / routeeCount + (i < messageCount % routeeCount ? 1 : 0))
            .ToArray();
        Sorted(GetCounts(mailboxes, routees)).Should().Equal(Sorted(expected));
    }

    [Fact]
    public async Task RouteAsync_ConcurrentCallers_ShouldDistributeMessagesEvenly()
    {
        // Arrange
        var (router, mailboxes, routees) = await CreateRoutedPoolAsync(4, Capability);

        // Act
        var tasks = Enumerable.Range(0, 400)
            .Select(_ => router.RouteAsync(Capability, CreateEnvelope()));
        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().OnlyContain(accepted => accepted);
        GetCounts(mailboxes, routees).Should().Equal(100, 100, 100, 100);
    }

    [Fact]
    public async Task RouteAsync_DifferentCapabilities_ShouldKeepIndependentCounters()
    {
        // Arrange
        const string alpha = "alpha";
        const string beta = "beta";
        var (router, mailboxes, routees) = await CreateRoutedPoolAsync(2, alpha, beta);

        // Act
        await router.RouteAsync(alpha, CreateEnvelope());
        await router.RouteAsync(beta, CreateEnvelope());
        var afterFirstRound = GetCounts(mailboxes, routees);

        await router.RouteAsync(alpha, CreateEnvelope());
        await router.RouteAsync(beta, CreateEnvelope());

        // Assert
        Sorted(afterFirstRound).Should().Equal(0, 2);
        GetCounts(mailboxes, routees).Should().Equal(2, 2);
    }

    [Fact]
    public async Task RouteAsync_NoRegisteredRoutees_ShouldReturnFalse()
    {
        // Arrange
        var (router, _, _) = await CreateRoutedPoolAsync(0, Capability);

        // Act
        var accepted = await router.RouteAsync(Capability, CreateEnvelope());

        // Assert
        accepted.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RouteAsync_BlankCapability_ShouldThrowArgumentException(string capability)
    {
        // Arrange
        var (router, _, _) = await CreateRoutedPoolAsync(1, Capability);

        // Act
        Func<Task> act = () => router.RouteAsync(capability, CreateEnvelope());

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task RouteAsync_NullEnvelope_ShouldThrowArgumentNullException()
    {
        // Arrange
        var (router, _, _) = await CreateRoutedPoolAsync(1, Capability);

        // Act
        Func<Task> act = () => router.RouteAsync(Capability, null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task RouteAsync_CancelledToken_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var (router, mailboxes, routees) = await CreateRoutedPoolAsync(2, Capability);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        Func<Task> act = () => router.RouteAsync(Capability, CreateEnvelope(), cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        GetCounts(mailboxes, routees).Should().Equal(0, 0);
    }

    [Fact]
    public void Constructor_NullDiscovery_ShouldThrowArgumentNullException()
    {
        // Arrange
        var dispatcher = CreateDispatcher();

        // Act
        Action act = () => new RoundRobinRouter(null!, dispatcher);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullDispatcher_ShouldThrowArgumentNullException()
    {
        // Act
        Action act = () => new RoundRobinRouter(new ActorDiscoveryService(), null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Builds a router over a pool of <paramref name="routeeCount"/> live actors, each
    /// registered under every capability in <paramref name="capabilities"/> with an
    /// empty mailbox.
    /// </summary>
    private static async Task<(RoundRobinRouter Router, MailboxService Mailboxes, List<ActorRef> Routees)>
        CreateRoutedPoolAsync(int routeeCount, params string[] capabilities)
    {
        var mailboxes = new MailboxService(new ActorSystemOptions());
        var registry = new ActorRegistry();
        var discovery = new ActorDiscoveryService();
        var dispatcher = new MessageDispatcher(mailboxes, registry, new ActorSystem("round-robin-router-tests"));
        var routees = new List<ActorRef>(routeeCount);

        for (var i = 0; i < routeeCount; i++)
        {
            var routee = new ActorRef(new ActorPath($"/round-robin/routee-{i}"), Guid.NewGuid());
            registry.Register(routee);
            discovery.Register(routee, capabilities);
            mailboxes.CreateMailbox(routee.Id, capacity: 10000);
            routees.Add(routee);
        }

        return (new RoundRobinRouter(discovery, dispatcher), mailboxes, routees);
    }

    private static MessageDispatcher CreateDispatcher()
    {
        return new MessageDispatcher(
            new MailboxService(new ActorSystemOptions()),
            new ActorRegistry(),
            new ActorSystem("round-robin-router-tests"));
    }

    /// <summary>
    /// Creates an envelope whose recipient is a placeholder; the router replaces it with the selected routee.
    /// </summary>
    private static Envelope CreateEnvelope()
    {
        return new Envelope(new ControlMessage("work"), new ActorRef(new ActorPath("/round-robin/placeholder"), Guid.NewGuid()));
    }

    private static int[] Sorted(IEnumerable<int> values) => values.OrderBy(value => value).ToArray();

    private static int[] GetCounts(MailboxService mailboxes, IEnumerable<ActorRef> routees)
    {
        return routees.Select(routee => mailboxes.GetMailboxSize(routee.Id)).ToArray();
    }
}
