﻿using API.Helpers.Converters;
using Hangfire;
using Xunit;

namespace API.Tests.Converters;

public class CronConverterTests
{
    [Theory]
    [InlineData("daily", "0 0 * * *")]
    [InlineData("disabled", "0 0 31 2 *")]
    [InlineData("weekly", "0 0 * * 1")]
    [InlineData("", "0 0 31 2 *")]
    [InlineData("sdfgdf", "sdfgdf")]
    [InlineData("* * * * *", "* * * * *")]
    [InlineData(null, "daily")]
    public void ConvertTest(string? input, string expected)
    {
        Assert.Equal(expected, CronConverter.ConvertToCronNotation(input));
    }
}
