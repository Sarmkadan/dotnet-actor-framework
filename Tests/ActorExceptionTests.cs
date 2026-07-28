using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq.Humanizer.Core;
using Moq.Humanizer.Core.Orgs;
using dotnet_actor_framework;
using dotnet_actor_framework.Models;
using dotnet_actor_framework.Models.Messages;
using dotnet_actor_framework.Tests;
using dotnet_actor_framework.Tests.Helpers;

namespace dotnet_actor_framework.Tests
{
    [TestClass]
    public class ActorExceptionTests
    {
        [TestMethod]
        public async Task Test_InvalidActorPathException_Throws_With_OffendingPath()
        {
            // Arrange
            var invalidPath = " "; // whitespace-only
            var ex = Assert.Throws<InvalidActorPathException>(() => ActorPath.Create(invalidPath));
            // Assert
            Assert.AreEqual("Actor path cannot be empty or contain only whitespace", ex.Message);
            Assert.AreEqual(invalidPath, ex.InvalidPath);
        }

        [TestMethod]
        public async Task Test_InvalidActorPathException_Throws_With_DisallowedCharacters()
        {
            // Arrange
            var invalidPath = "<>"; // disallowed characters
            var ex = Assert.Throws<InvalidActorPathException>(() => ActorPath.Create(invalidPath));
            // Assert
            Assert.AreEqual("Actor path cannot contain the following characters: <, >", ex.Message);
            Assert.AreEqual(invalidPath, ex.InvalidPath);
        }

        [TestMethod]
        public async Task Test_InvalidMessageException_Throws_With_NullPayload()
        {
            // Arrange
            var message = new Message<int>();
            var ex = Assert.Throws<InvalidMessageException>(() => message.Dispatch());
            // Assert
            Assert.AreEqual("Payload cannot be null", ex.Message);
            Assert.AreEqual(message.Id, ex.MessageId);
        }

        [TestMethod]
        public async Task Test_InvalidMessageException_Throws_With_TypeMismatchedPayload()
        {
            // Arrange
            var message = new Message<string>();
            var ex = Assert.Throws<InvalidMessageException>(() => message.Dispatch());
            // Assert
            Assert.AreEqual("Payload type mismatch", ex.Message);
            Assert.AreEqual(message.Id, ex.MessageId);
        }
    }
}
