// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetActorFramework.Utilities;
using FluentAssertions;

namespace DotNetActorFramework.Tests;

public class SerializationExtensionsTests
{
    private record TestDto(string Name, int Age);

    [Fact]
    public void ToJson_NullObject_ReturnsNullString()
    {
        TestDto? obj = null;
        obj.ToJson().Should().Be("null");
    }

    [Fact]
    public void ToJson_ValidObject_ReturnsJsonString()
    {
        var dto = new TestDto("Alice", 30);
        var json = dto.ToJson();
        json.Should().Contain("\"Name\"").Or.Contain("\"name\"");
        json.Should().Contain("Alice");
    }

    [Fact]
    public void ToJsonPretty_ValidObject_ContainsNewlines()
    {
        var dto = new TestDto("Bob", 25);
        var json = dto.ToJsonPretty();
        json.Should().Contain("\n");
    }

    [Fact]
    public void ToJsonBytes_NullObject_ReturnsEmptyArray()
    {
        TestDto? obj = null;
        obj.ToJsonBytes().Should().BeEmpty();
    }

    [Fact]
    public void ToJsonBytes_ValidObject_ReturnsNonEmptyBytes()
    {
        var dto = new TestDto("Charlie", 35);
        dto.ToJsonBytes().Should().NotBeEmpty();
    }

    [Fact]
    public void FromJson_NullString_ReturnsDefault()
    {
        string? json = null;
        json!.FromJson<TestDto>().Should().BeNull();
    }

    [Fact]
    public void FromJson_EmptyString_ReturnsDefault()
    {
        "".FromJson<TestDto>().Should().BeNull();
    }

    [Fact]
    public void FromJson_InvalidJson_ReturnsDefault()
    {
        "not json".FromJson<TestDto>().Should().BeNull();
    }

    [Fact]
    public void FromJson_ValidJson_ReturnsDeserialized()
    {
        var json = "{\"Name\":\"Dave\",\"Age\":40}";
        var result = json.FromJson<TestDto>();
        result.Should().NotBeNull();
        result!.Name.Should().Be("Dave");
        result.Age.Should().Be(40);
    }

    [Fact]
    public void RoundTrip_SerializeDeserialize_PreservesData()
    {
        var original = new TestDto("Eve", 28);
        var json = original.ToJson();
        var restored = json.FromJson<TestDto>();
        restored.Should().Be(original);
    }

    [Fact]
    public void RoundTrip_BytesSerialization_PreservesData()
    {
        var original = new TestDto("Frank", 33);
        var bytes = original.ToJsonBytes();
        var json = System.Text.Encoding.UTF8.GetString(bytes);
        var restored = json.FromJson<TestDto>();
        restored.Should().Be(original);
    }
}
