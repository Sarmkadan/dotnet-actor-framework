// Copyright (c) 2024
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using DotNetActorFramework.Api;
using DotNetActorFramework.Models;
using Xunit;

namespace DotNetActorFramework.Tests;

public class SystemMetricsApiValidationTests
{
    private static HealthSummary CreateValidHealthSummary() => new()
    {
        SystemName = "TestSystem",
        SystemId = Guid.NewGuid(),
        TotalActors = 10,
        HealthyActors = 8,
        UnhealthyActors = 1,
        ErrorActors = 1,
        TotalMessages = 1000,
        TotalErrors = 10,
        ErrorRate = 0.01,
        HealthPercentage = 95.0,
        AverageLatencyMs = 12.5,
        Timestamp = DateTime.UtcNow
    };

    private static MessageTypeMetricsInfo CreateValidMessageTypeMetricsInfo() => new()
    {
        MessageType = "TestMessage",
        ProcessedCount = 500,
        ErrorCount = 5,
        AverageLatencyMs = 8.3,
        ErrorRate = 0.01
    };

    private static ActorMetricsInfo CreateValidActorMetricsInfo() => new()
    {
        ActorPath = "/user/testActor",
        ProcessedCount = 200,
        ErrorCount = 2,
        AverageLatencyMs = 5.0,
        ErrorRate = 0.01
    };

    [Fact]
    public void Validate_HealthSummary_HappyPath_ReturnsEmpty()
    {
        var summary = CreateValidHealthSummary();

        var errors = summary.Validate();

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_HealthSummary_Null_ThrowsArgumentNullException()
    {
        HealthSummary? summary = null;

        Assert.Throws<ArgumentNullException>(() => summary!.Validate());
    }

    [Fact]
    public void Validate_HealthSummary_InvalidValues_ReturnsErrors()
    {
        var summary = new HealthSummary
        {
            SystemName = "",
            SystemId = Guid.Empty,
            TotalActors = -1,
            HealthyActors = -1,
            UnhealthyActors = -1,
            ErrorActors = -1,
            TotalMessages = -1,
            TotalErrors = -1,
            ErrorRate = 1.5,               // > 1
            HealthPercentage = 150,        // > 100
            AverageLatencyMs = -10,
            Timestamp = default
        };

        var errors = summary.Validate();

        // Expect at least one error for each invalid field
        Assert.Contains(errors, e => e.Contains("SystemName"));
        Assert.Contains(errors, e => e.Contains("SystemId"));
        Assert.Contains(errors, e => e.Contains("TotalActors"));
        Assert.Contains(errors, e => e.Contains("HealthyActors"));
        Assert.Contains(errors, e => e.Contains("UnhealthyActors"));
        Assert.Contains(errors, e => e.Contains("ErrorActors"));
        Assert.Contains(errors, e => e.Contains("TotalMessages"));
        Assert.Contains(errors, e => e.Contains("TotalErrors"));
        Assert.Contains(errors, e => e.Contains("ErrorRate"));
        Assert.Contains(errors, e => e.Contains("HealthPercentage"));
        Assert.Contains(errors, e => e.Contains("AverageLatencyMs"));
        Assert.Contains(errors, e => e.Contains("Timestamp"));
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void IsValid_HealthSummary_ReturnsCorrectFlag()
    {
        var valid = CreateValidHealthSummary();
        var invalid = new HealthSummary { SystemName = "", SystemId = Guid.Empty, Timestamp = default };

        Assert.True(valid.IsValid());
        Assert.False(invalid.IsValid());
    }

    [Fact]
    public void EnsureValid_HealthSummary_Invalid_ThrowsArgumentException()
    {
        var invalid = new HealthSummary { SystemName = "", SystemId = Guid.Empty, Timestamp = default };

        var ex = Assert.Throws<ArgumentException>(() => invalid.EnsureValid());

        // The message should contain the word "invalid" and list at least one error
        Assert.Contains("HealthSummary instance is invalid", ex.Message);
        Assert.Contains("SystemName", ex.Message);
    }

    [Fact]
    public void Validate_MessageTypeMetricsInfo_HappyPath_ReturnsEmpty()
    {
        var info = CreateValidMessageTypeMetricsInfo();

        var errors = info.Validate();

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ActorMetricsInfo_HappyPath_ReturnsEmpty()
    {
        var info = CreateValidActorMetricsInfo();

        var errors = info.Validate();

        Assert.Empty(errors);
    }

    [Fact]
    public void EnsureValid_MessageTypeMetricsInfo_Invalid_ThrowsArgumentException()
    {
        var invalid = new MessageTypeMetricsInfo
        {
            MessageType = "",
            ProcessedCount = -5,
            ErrorCount = -1,
            AverageLatencyMs = -2,
            ErrorRate = -0.1
        };

        var ex = Assert.Throws<ArgumentException>(() => invalid.EnsureValid());

        Assert.Contains("MessageTypeMetricsInfo instance is invalid", ex.Message);
        Assert.Contains("MessageType", ex.Message);
    }

    [Fact]
    public void EnsureValid_ActorMetricsInfo_Invalid_ThrowsArgumentException()
    {
        var invalid = new ActorMetricsInfo
        {
            ActorPath = "",
            ProcessedCount = -10,
            ErrorCount = -2,
            AverageLatencyMs = -3,
            ErrorRate = 2.0
        };

        var ex = Assert.Throws<ArgumentException>(() => invalid.EnsureValid());

        Assert.Contains("ActorMetricsInfo instance is invalid", ex.Message);
        Assert.Contains("ActorPath", ex.Message);
    }
}
