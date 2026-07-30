// =============================================================================
// Tests for GuardExtensions
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using DotNetActorFramework.Utilities;
using Xunit;

namespace DotNetActorFramework.Tests;

public class GuardExtensionsTests
{
    // NotNull ---------------------------------------------------------------
    [Fact]
    public void NotNull_ReturnsValue_WhenNotNull()
    {
        var obj = new object();
        var result = obj.NotNull(nameof(obj));
        Assert.Same(obj, result);
    }

    [Fact]
    public void NotNull_ThrowsArgumentNullException_WhenNull()
    {
        object? nullObj = null;
        Assert.Throws<ArgumentNullException>(() => nullObj!.NotNull(nameof(nullObj)));
    }

    // NotNullOrEmpty --------------------------------------------------------
    [Fact]
    public void NotNullOrEmpty_ReturnsString_WhenNotEmpty()
    {
        const string value = "test";
        var result = value.NotNullOrEmpty(nameof(value));
        Assert.Equal(value, result);
    }

    [Fact]
    public void NotNullOrEmpty_ThrowsArgumentException_WhenNull()
    {
        string? nullStr = null;
        Assert.Throws<ArgumentException>(() => nullStr!.NotNullOrEmpty(nameof(nullStr)));
    }

    [Fact]
    public void NotNullOrEmpty_ThrowsArgumentException_WhenEmpty()
    {
        const string empty = "";
        Assert.Throws<ArgumentException>(() => empty.NotNullOrEmpty(nameof(empty)));
    }

    // NotNullOrWhiteSpace ---------------------------------------------------
    [Fact]
    public void NotNullOrWhiteSpace_ReturnsString_WhenValid()
    {
        const string value = "valid";
        var result = value.NotNullOrWhiteSpace(nameof(value));
        Assert.Equal(value, result);
    }

    [Fact]
    public void NotNullOrWhiteSpace_ThrowsArgumentException_WhenNull()
    {
        string? nullStr = null;
        Assert.Throws<ArgumentException>(() => nullStr!.NotNullOrWhiteSpace(nameof(nullStr)));
    }

    [Fact]
    public void NotNullOrWhiteSpace_ThrowsArgumentException_WhenWhiteSpace()
    {
        const string ws = "   ";
        Assert.Throws<ArgumentException>(() => ws.NotNullOrWhiteSpace(nameof(ws)));
    }

    // NotEmpty (Guid) --------------------------------------------------------
    [Fact]
    public void NotEmptyGuid_ReturnsGuid_WhenNotEmpty()
    {
        var guid = Guid.NewGuid();
        var result = guid.NotEmpty(nameof(guid));
        Assert.Equal(guid, result);
    }

    [Fact]
    public void NotEmptyGuid_ThrowsArgumentException_WhenEmpty()
    {
        var empty = Guid.Empty;
        Assert.Throws<ArgumentException>(() => empty.NotEmpty(nameof(empty)));
    }

    // MustBePositive ---------------------------------------------------------
    [Fact]
    public void MustBePositive_ReturnsValue_WhenPositive()
    {
        const int value = 5;
        var result = value.MustBePositive(nameof(value));
        Assert.Equal(value, result);
    }

    [Fact]
    public void MustBePositive_ThrowsArgumentException_WhenZero()
    {
        const int zero = 0;
        Assert.Throws<ArgumentException>(() => zero.MustBePositive(nameof(zero)));
    }

    [Fact]
    public void MustBePositive_ThrowsArgumentException_WhenNegative()
    {
        const int negative = -3;
        Assert.Throws<ArgumentException>(() => negative.MustBePositive(nameof(negative)));
    }

    // MustBeNonNegative ------------------------------------------------------
    [Fact]
    public void MustBeNonNegative_ReturnsValue_WhenZero()
    {
        const int zero = 0;
        var result = zero.MustBeNonNegative(nameof(zero));
        Assert.Equal(zero, result);
    }

    [Fact]
    public void MustBeNonNegative_ReturnsValue_WhenPositive()
    {
        const int positive = 7;
        var result = positive.MustBeNonNegative(nameof(positive));
        Assert.Equal(positive, result);
    }

    [Fact]
    public void MustBeNonNegative_ThrowsArgumentException_WhenNegative()
    {
        const int negative = -1;
        Assert.Throws<ArgumentException>(() => negative.MustBeNonNegative(nameof(negative)));
    }

    // NotEmpty (IEnumerable) -------------------------------------------------
    [Fact]
    public void NotEmptyCollection_ReturnsCollection_WhenNotEmpty()
    {
        IEnumerable<int> collection = new[] { 1, 2, 3 };
        var result = collection.NotEmpty(nameof(collection));
        Assert.Same(collection, result);
    }

    [Fact]
    public void NotEmptyCollection_ThrowsArgumentException_WhenNull()
    {
        IEnumerable<int>? nullColl = null;
        Assert.Throws<ArgumentException>(() => nullColl!.NotEmpty(nameof(nullColl)));
    }

    [Fact]
    public void NotEmptyCollection_ThrowsArgumentException_WhenEmpty()
    {
        IEnumerable<int> empty = Enumerable.Empty<int>();
        Assert.Throws<ArgumentException>(() => empty.NotEmpty(nameof(empty)));
    }

    // MustBeTrue -------------------------------------------------------------
    [Fact]
    public void MustBeTrue_DoesNotThrow_WhenConditionTrue()
    {
        const bool condition = true;
        var ex = Record.Exception(() => condition.MustBeTrue("should not fail"));
        Assert.Null(ex);
    }

    [Fact]
    public void MustBeTrue_ThrowsArgumentException_WhenConditionFalse()
    {
        const bool condition = false;
        Assert.Throws<ArgumentException>(() => condition.MustBeTrue("condition must be true"));
    }

    [Fact]
    public void MustBeTrue_ThrowsArgumentNullException_WhenMessageNull()
    {
        const bool condition = false;
        Assert.Throws<ArgumentNullException>(() => condition.MustBeTrue(null!));
    }

    // MustBeFalse ------------------------------------------------------------
    [Fact]
    public void MustBeFalse_DoesNotThrow_WhenConditionFalse()
    {
        const bool condition = false;
        var ex = Record.Exception(() => condition.MustBeFalse("should not fail"));
        Assert.Null(ex);
    }

    [Fact]
    public void MustBeFalse_ThrowsArgumentException_WhenConditionTrue()
    {
        const bool condition = true;
        Assert.Throws<ArgumentException>(() => condition.MustBeFalse("condition must be false"));
    }

    [Fact]
    public void MustBeFalse_ThrowsArgumentNullException_WhenMessageNull()
    {
        const bool condition = true;
        Assert.Throws<ArgumentNullException>(() => condition.MustBeFalse(null!));
    }
}
