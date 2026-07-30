// Copyright (c) 2024
// SPDX-License-Identifier: MIT

using System;
using System.Text.Json;
using DotNetActorFramework.Models;
using DotNetActorFramework.Utilities;
using Xunit;

namespace DotNetActorFramework.Tests;

public class ActorPathExtensionsJsonExtensionsTests
{
    private static ActorPath CreateSamplePath()
        => new ActorPath("/user/sampleActor");

    [Fact]
    public void ToJson_HappyPath_ReturnsValidJson()
    {
        // Arrange
        var path = CreateSamplePath();

        // Act
        var json = path.ToJson();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(json));

        // The JSON should contain the camel‑cased property name "path"
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.TryGetProperty("path", out var pathProp));
        Assert.Equal(path.Path, pathProp.GetString());
    }

    [Fact]
    public void ToJson_WithIndentation_ProducesIndentedJson()
    {
        var path = CreateSamplePath();

        var json = path.ToJson(indented: true);

        // Indented JSON contains line‑breaks
        Assert.Contains(Environment.NewLine, json);
    }

    [Fact]
    public void ToJson_NullPath_ThrowsArgumentNullException()
    {
        ActorPath? nullPath = null;

        Assert.Throws<ArgumentNullException>(() => nullPath!.ToJson());
    }

    [Fact]
    public void FromJson_HappyPath_ReturnsActorPath()
    {
        var original = CreateSamplePath();
        var json = original.ToJson();

        var deserialized = ActorPathExtensionsJsonExtensions.FromJson(json);

        Assert.NotNull(deserialized);
        Assert.Equal(original.Path, deserialized!.Path);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void FromJson_NullOrEmpty_ReturnsNull(string? json)
    {
        var result = ActorPathExtensionsJsonExtensions.FromJson(json!);
        Assert.Null(result);
    }

    [Fact]
    public void TryFromJson_ValidJson_ReturnsTrueAndValue()
    {
        var original = CreateSamplePath();
        var json = original.ToJson();

        var success = ActorPathExtensionsJsonExtensions.TryFromJson(json, out var value);

        Assert.True(success);
        Assert.NotNull(value);
        Assert.Equal(original.Path, value!.Path);
    }

    [Fact]
    public void TryFromJson_InvalidJson_ReturnsFalseAndNull()
    {
        const string malformed = "{ this is not valid json }";

        var success = ActorPathExtensionsJsonExtensions.TryFromJson(malformed, out var value);

        Assert.False(success);
        Assert.Null(value);
    }

    [Fact]
    public void TryFromJson_NullOrEmpty_ReturnsTrueAndNull()
    {
        var success = ActorPathExtensionsJsonExtensions.TryFromJson(string.Empty, out var value);
        Assert.True(success);
        Assert.Null(value);
    }
}
