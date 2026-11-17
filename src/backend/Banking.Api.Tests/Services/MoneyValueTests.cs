using Banking.Api.Contracts;

namespace Banking.Api.Tests.Services;

public sealed class MoneyValueTests
{
    [Theory]
    [InlineData("0.01", "0.01")]
    [InlineData("100", "100.00")]
    [InlineData("9999999999999999.99", "9999999999999999.99")]
    public void ParsesAndFormatsExactly(string input, string expected)
    {
        Assert.True(MoneyValue.TryParse(input, out var result));
        Assert.Equal(expected, MoneyValue.Format(result));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("+1")]
    [InlineData("1.001")]
    [InlineData("1e2")]
    [InlineData(" 1.00")]
    [InlineData("10000000000000000.00")]
    public void RejectsInvalidAmounts(string? input) => Assert.False(MoneyValue.TryParse(input, out _));
}
