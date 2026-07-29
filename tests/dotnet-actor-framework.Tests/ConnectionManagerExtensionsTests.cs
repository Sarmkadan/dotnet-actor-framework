// =============================================================================
// Author: Automated Generation
// =============================================================================

using System;
using System.Collections.Generic;
using System.Reflection;
using DotNetActorFramework.Repository;
using FluentAssertions;
using Xunit;

namespace DotNetActorFramework.Tests;

public class ConnectionManagerExtensionsTests
{
    // Helper to create a ConnectionManager instance with the desired internal state.
    private static ConnectionManager CreateConnectionManager(bool isConnected, Dictionary<string, PooledConnection>? pool = null)
    {
        // Use the non‑public constructor (if any) via reflection.
        var cm = (ConnectionManager)Activator.CreateInstance(typeof(ConnectionManager), true)!;

        // Initialise the private lock object.
        var lockField = typeof(ConnectionManager).GetField("_lockObject",
            BindingFlags.NonPublic | BindingFlags.Instance);
        lockField?.SetValue(cm, new object());

        // Initialise the private connection pool.
        var poolField = typeof(ConnectionManager).GetField("_connectionPool",
            BindingFlags.NonPublic | BindingFlags.Instance);
        poolField?.SetValue(cm, pool ?? new Dictionary<string, PooledConnection>());

        // Set the IsConnected flag – try the public setter first, otherwise set the backing field.
        var isConnectedProp = typeof(ConnectionManager).GetProperty("IsConnected",
            BindingFlags.Public | BindingFlags.Instance);
        if (isConnectedProp != null && isConnectedProp.CanWrite)
        {
            isConnectedProp.SetValue(cm, isConnected);
        }
        else
        {
            var backingField = typeof(ConnectionManager).GetField("<IsConnected>k__BackingField",
                BindingFlags.NonPublic | BindingFlags.Instance);
            backingField?.SetValue(cm, isConnected);
        }

        return cm;
    }

    #region ArgumentNullException tests

    [Fact]
    public void GetConnectionKeys_Null_ThrowsArgumentNullException()
    {
        Action act = () => ConnectionManagerExtensions.GetConnectionKeys(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetTotalConnections_Null_ThrowsArgumentNullException()
    {
        Action act = () => ConnectionManagerExtensions.GetTotalConnections(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetConnectionStatistics_Null_ThrowsArgumentNullException()
    {
        Action act = () => ConnectionManagerExtensions.GetConnectionStatistics(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetOldestIdleConnection_Null_ThrowsArgumentNullException()
    {
        Action act = () => ConnectionManagerExtensions.GetOldestIdleConnection(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetTotalIdleTime_Null_ThrowsArgumentNullException()
    {
        Action act = () => ConnectionManagerExtensions.GetTotalIdleTime(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetAverageIdleTime_Null_ThrowsArgumentNullException()
    {
        Action act = () => ConnectionManagerExtensions.GetAverageIdleTime(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Disconnected (IsConnected == false) tests

    [Fact]
    public void GetConnectionKeys_WhenDisconnected_ReturnsEmpty()
    {
        var cm = CreateConnectionManager(isConnected: false);
        var keys = cm.GetConnectionKeys();
        keys.Should().BeEmpty();
    }

    [Fact]
    public void GetTotalConnections_WhenDisconnected_ReturnsZero()
    {
        var cm = CreateConnectionManager(isConnected: false);
        var total = cm.GetTotalConnections();
        total.Should().Be(0);
    }

    [Fact]
    public void GetConnectionStatistics_WhenDisconnected_ReturnsEmpty()
    {
        var cm = CreateConnectionManager(isConnected: false);
        var stats = cm.GetConnectionStatistics();
        stats.Should().BeEmpty();
    }

    [Fact]
    public void GetOldestIdleConnection_WhenDisconnected_ReturnsNull()
    {
        var cm = CreateConnectionManager(isConnected: false);
        var oldest = cm.GetOldestIdleConnection();
        oldest.Should().BeNull();
    }

    [Fact]
    public void GetTotalIdleTime_WhenDisconnected_ReturnsZero()
    {
        var cm = CreateConnectionManager(isConnected: false);
        var total = cm.GetTotalIdleTime();
        total.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void GetAverageIdleTime_WhenDisconnected_ReturnsZero()
    {
        var cm = CreateConnectionManager(isConnected: false);
        var avg = cm.GetAverageIdleTime();
        avg.Should().Be(TimeSpan.Zero);
    }

    #endregion

    #region Connected with empty pool (happy path – empty results)

    [Fact]
    public void GetConnectionKeys_WhenConnectedAndEmptyPool_ReturnsEmpty()
    {
        var cm = CreateConnectionManager(isConnected: true);
        var keys = cm.GetConnectionKeys();
        keys.Should().BeEmpty();
    }

    [Fact]
    public void GetTotalConnections_WhenConnectedAndEmptyPool_ReturnsZero()
    {
        var cm = CreateConnectionManager(isConnected: true);
        var total = cm.GetTotalConnections();
        total.Should().Be(0);
    }

    [Fact]
    public void GetConnectionStatistics_WhenConnectedAndEmptyPool_ReturnsEmpty()
    {
        var cm = CreateConnectionManager(isConnected: true);
        var stats = cm.GetConnectionStatistics();
        stats.Should().BeEmpty();
    }

    [Fact]
    public void GetOldestIdleConnection_WhenConnectedAndEmptyPool_ReturnsNull()
    {
        var cm = CreateConnectionManager(isConnected: true);
        var oldest = cm.GetOldestIdleConnection();
        oldest.Should().BeNull();
    }

    [Fact]
    public void GetTotalIdleTime_WhenConnectedAndEmptyPool_ReturnsZero()
    {
        var cm = CreateConnectionManager(isConnected: true);
        var total = cm.GetTotalIdleTime();
        total.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void GetAverageIdleTime_WhenConnectedAndEmptyPool_ReturnsZero()
    {
        var cm = CreateConnectionManager(isConnected: true);
        var avg = cm.GetAverageIdleTime();
        avg.Should().Be(TimeSpan.Zero);
    }

    #endregion
}
