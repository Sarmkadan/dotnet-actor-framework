using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq.Hibernate.NHibernate;
using Moq.Hibernate.NHibernate.Linq;

namespace dotnet_actor_framework.tests
{
    [TestClass]
    public class ActorCacheServiceTests
    {
        [TestMethod]
        public async Task TestEvictionOnCapacity()
        {
            // Arrange
            var cacheService = new ActorCacheService(10);
            for (int i = 0; i < 11; i++)
            {
                await cacheService.AddAsync("key" + i, "value" + i);
            }
            // Act
            var result = await cacheService.GetAsync("key0");
            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task TestEvictionOnAccess()
        {
            // Arrange
            var cacheService = new ActorCacheService(10);
            await cacheService.AddAsync("key1", "value1");
            await cacheService.AddAsync("key2", "value2");
            await cacheService.GetAsync("key1");
            // Act
            await cacheService.AddAsync("key3", "value3");
            // Assert
            var result = await cacheService.GetAsync("key2");
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task TestTtlExpiration()
        {
            // Arrange
            var cacheService = new ActorCacheService(10);
            await cacheService.AddAsync("key1", "value1", TimeSpan.FromHours(1));
            await Task.Delay(TimeSpan.FromHours(2));
            // Act
            var result = await cacheService.GetAsync("key1");
            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task TestConcurrentAccess()
        {
            // Arrange
            var cacheService = new ActorCacheService(10);
            await cacheService.AddAsync("key1", "value1");
            // Act
            Parallel.For(0, 10, async i =>
            {
                await cacheService.AddAsync("key" + i, "value" + i);
            });
            // Assert
            for (int i = 0; i < 10; i++)
            {
                var result = await cacheService.GetAsync("key" + i);
                Assert.IsNotNull(result);
            }
        }
    }
}