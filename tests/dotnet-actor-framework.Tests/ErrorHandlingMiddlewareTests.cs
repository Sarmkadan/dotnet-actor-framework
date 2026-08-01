using DotNetActorFramework.Middleware;
using DotNetActorFramework.Models;
using FluentAssertions;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace DotNetActorFramework.Tests
{
    public class ErrorHandlingMiddlewareTests
    {
        private static ActorRef CreateActorRef(string pathStr)
        {
            var path = new ActorPath(pathStr);
            return new ActorRef(path, Guid.NewGuid());
        }

        private static Envelope CreateEnvelope(string recipientPath = "/system/actor")
        {
            var message = new ControlMessage("test-command");
            var recipient = CreateActorRef(recipientPath);
            return new Envelope(message, recipient);
        }

        [Fact]
        public void Constructor_WithNullStrategy_ThrowsArgumentNullException()
        {
            // Act
            var act = () => new ErrorHandlingMiddleware(null!);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("strategy");
        }

        [Fact]
        public async Task InvokeAsync_WithNullEnvelope_ThrowsArgumentNullException()
        {
            // Arrange
            var strategy = new SuppressErrorStrategy();
            var middleware = new ErrorHandlingMiddleware(strategy);

            // Act
            var act = async () => await middleware.InvokeAsync(null!, _ => Task.CompletedTask);

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>()
                .WithParameterName("envelope");
        }

        [Fact]
        public async Task InvokeAsync_WithNoException_ReturnsTrueAndCallsNext()
        {
            // Arrange
            var strategy = new SuppressErrorStrategy();
            var middleware = new ErrorHandlingMiddleware(strategy);
            var envelope = CreateEnvelope();
            var nextCalled = false;
            Func<Envelope, Task> next = e =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            };

            // Act
            var result = await middleware.InvokeAsync(envelope, next);

            // Assert
            result.Should().BeTrue();
            nextCalled.Should().BeTrue();
        }

        [Fact]
        public async Task InvokeAsync_WithException_ReturnsStrategyResult()
        {
            // Arrange
            var exception = new InvalidOperationException("test");
            var strategyMock = new Mock<ErrorHandlingStrategy>();
            strategyMock.Setup(s => s.HandleErrorAsync(It.IsAny<Envelope>(), It.IsAny<Exception>()))
                .ReturnsAsync(true);
            var middleware = new ErrorHandlingMiddleware(strategyMock.Object);
            var envelope = CreateEnvelope();
            var nextCalled = false;
            Func<Envelope, Task> next = e =>
            {
                nextCalled = true;
                throw exception;
            };

            // Act
            var result = await middleware.InvokeAsync(envelope, next);

            // Assert
            result.Should().BeTrue();
            nextCalled.Should().BeTrue();
            strategyMock.Verify(s => s.HandleErrorAsync(envelope, exception), Times.Once);
        }

        [Fact]
        public async Task SuppressErrorStrategy_AlwaysReturnsTrue()
        {
            // Arrange
            var strategy = new SuppressErrorStrategy();
            var middleware = new ErrorHandlingMiddleware(strategy);
            var envelope = CreateEnvelope();
            var exception = new InvalidOperationException("test");

            // Act
            var result = await strategy.HandleErrorAsync(envelope, exception);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task RetryErrorStrategy_ReturnsTrue_WhenRetryCountLessThanMaxRetries()
        {
            // Arrange
            var strategy = new RetryErrorStrategy(maxRetries: 3, initialDelay: TimeSpan.Zero);
            var envelope = CreateEnvelope();
            var exception = new InvalidOperationException("test");

            // Act
            var resultFirst = await strategy.HandleErrorAsync(envelope, exception);
            var resultSecond = await strategy.HandleErrorAsync(envelope, exception);
            var resultThird = await strategy.HandleErrorAsync(envelope, exception);

            // Assert
            resultFirst.Should().BeTrue();
            resultSecond.Should().BeTrue();
            resultThird.Should().BeTrue();
        }

        [Fact]
        public async Task RetryErrorStrategy_ReturnsFalse_WhenRetryCountEqualsOrExceedsMaxRetries()
        {
            // Arrange
            var strategy = new RetryErrorStrategy(maxRetries: 2, initialDelay: TimeSpan.Zero);
            var envelope = CreateEnvelope();
            var exception = new InvalidOperationException("test");

            // Act
            var resultFirst = await strategy.HandleErrorAsync(envelope, exception); // retry 1
            var resultSecond = await strategy.HandleErrorAsync(envelope, exception); // retry 2
            var resultThird = await strategy.HandleErrorAsync(envelope, exception); // retry 3 (over limit)

            // Assert
            resultFirst.Should().BeTrue();
            resultSecond.Should().BeTrue();
            resultThird.Should().BeFalse();
        }

        [Fact]
        public async Task RetryErrorStrategy_DelayIncreasesWithRetryCount()
        {
            // Arrange
            var initialDelay = TimeSpan.FromMilliseconds(100);
            var backoffMultiplier = 2.0;
            var strategy = new RetryErrorStrategy(maxRetries: 3, initialDelay: initialDelay, backoffMultiplier: backoffMultiplier);
            var envelope = CreateEnvelope();
            var exception = new InvalidOperationException("test");

            // Act
            await strategy.HandleErrorAsync(envelope, exception); // retry 0 -> delay = 100 * 2^0 = 100ms
            var task2 = strategy.HandleErrorAsync(envelope, exception); // retry 1 -> delay = 100 * 2^1 = 200ms
            var task3 = strategy.HandleErrorAsync(envelope, exception); // retry 2 -> delay = 100 * 2^2 = 400ms

            // We need to measure the delay; we can't easily await all at once because they'd run sequentially.
            // Instead, we can start tasks and measure when they complete.
            // But for simplicity, we can just verify that the delay increases by checking the internal state?
            // Since the retry counts are stored, we can't easily observe delay without mocking Task.Delay.
            // We'll skip precise timing test; the logic is straightforward.
            // At least verify that after three calls, it returns true (still within retries).
            var result1 = await task2; // Actually we need to await in order.
            // Let's do sequentially but capture timestamps.
            var sw = System.Diagnostics.Stopwatch.StartNew();
            await strategy.HandleErrorAsync(envelope, exception); // retry 0
            var delay1 = sw.ElapsedMilliseconds;
            sw.Restart();
            await strategy.HandleErrorAsync(envelope, exception); // retry 1
            var delay2 = sw.ElapsedMilliseconds;
            sw.Restart();
            await strategy.HandleErrorAsync(envelope, exception); // retry 2
            var delay3 = sw.ElapsedMilliseconds;

            // Assert delays increase (allow some tolerance)
            delay2.Should().BeGreaterThan(delay1);
            delay3.Should().BeGreaterThan(delay2);
        }

        [Fact]
        public async Task FailFastErrorStrategy_ThrowsInvalidOperationException_WithExpectedMessage()
        {
            // Arrange
            var strategy = new FailFastErrorStrategy();
            var envelope = CreateEnvelope("/test/actor");
            var exception = new InvalidOperationException("test");

            // Act
            Func<Task> act = async () => await strategy.HandleErrorAsync(envelope, exception);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .Where(ex => ex.Message.Contains("Message processing failed for /test/actor"))
                .Where(ex => ex.InnerException == exception);
        }
    }
}