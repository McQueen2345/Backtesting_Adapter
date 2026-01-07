using FluentAssertions;
using QTS.Backtest.Adapter.Mapping;
using Xunit;

namespace QTS.Backtest.Adapter.Tests.Mapping;

public sealed class SynonymRegistryTests
{
    private readonly SynonymRegistry _sut = new();

    [Fact]
    public void GetCanonicalName_ts_Returns_timestamp()
    {
        _sut.GetCanonicalName("ts").Should().Be("timestamp");
    }

    [Fact]
    public void GetCanonicalName_qty_Returns_size()
    {
        _sut.GetCanonicalName("qty").Should().Be("size");
    }

    [Fact]
    public void GetCanonicalName_unknown_col_Returns_null()
    {
        _sut.GetCanonicalName("unknown_col").Should().BeNull();
    }

    [Theory]
    [InlineData("TS")]
    [InlineData("  ts ")]
    [InlineData("DateTime")]
    [InlineData("date_time")]
    public void GetCanonicalName_CaseInsensitiveAndTrim_Works_ForTimestamp(string value)
    {
        _sut.GetCanonicalName(value).Should().Be("timestamp");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void GetCanonicalName_EmptyOrWhitespace_Returns_null(string value)
    {
        _sut.GetCanonicalName(value).Should().BeNull();
    }
}
