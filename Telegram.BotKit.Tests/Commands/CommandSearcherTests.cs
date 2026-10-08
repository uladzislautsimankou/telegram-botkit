using FluentAssertions;
using Telegram.BotKit.Commands;

namespace Telegram.BotKit.Tests.Commands;

public sealed class CommandSearcherTests
{
    private readonly CommandSearcher _searcher = new([
        "/ping",
        "/bday_check",
        "/weather_minsk"
    ]);

    // verifies that typo variations of a short command like /ping are correctly matched
    [Theory]
    [InlineData("png", "/ping")]           // missing letter
    [InlineData("pnig", "/ping")]          // swapped letters
    [InlineData("pingg", "/ping")]         // extra letter
    [InlineData("pibg", "/ping")]          // typo
    [InlineData("/ping", "/ping")]         // exact match
    public void FindSimilar_WhenPingVariationsAreQueried_ShouldReturnPing(string input, string expected)
    {
        // arrange & act
        var result = _searcher.FindSimilar(input);

        // assert
        result.Should().Contain(expected);
    }

    // verifies that shuffled words and typo variations of /bday_check are correctly resolved
    [Theory]
    [InlineData("check_bday", "/bday_check")]   // reversed words
    [InlineData("chek_bday", "/bday_check")]    // reversed words with typo
    [InlineData("checkbday", "/bday_check")]    // missing underscore
    [InlineData("bdaycheck", "/bday_check")]    // concatenated words
    [InlineData("bday_chek", "/bday_check")]    // typo
    public void FindSimilar_WhenBdayCheckVariationsAreQueried_ShouldReturnBdayCheck(string input, string expected)
    {
        // arrange & act
        var result = _searcher.FindSimilar(input);

        // assert
        result.Should().Contain(expected);
    }

    // verifies that typing an incomplete prefix of a long command matches successfully
    [Theory]
    [InlineData("wether", "/weather_minsk")]    // incomplete prefix
    public void FindSimilar_WhenLongCommandPrefixIsQueried_ShouldReturnCommand(string input, string expected)
    {
        // arrange & act
        var result = _searcher.FindSimilar(input);

        // assert
        result.Should().Contain(expected);
    }

    // verifies that completely unrecognized strings return no suggested matches
    [Fact]
    public void FindSimilar_WhenNonsenseInputIsProvided_ShouldReturnEmpty()
    {
        // arrange
        var nonsenseInput = "some_random_command";

        // act
        var result = _searcher.FindSimilar(nonsenseInput);

        // assert
        result.Should().BeEmpty();
    }

    // verifies that inputs exceeding the maximum allowed command length are rejected early and return no suggestions
    [Fact]
    public void FindSimilar_WhenInputExceedsMaxCommandLength_ShouldReturnEmptyWithoutCrashing()
    {
        // arrange
        var longSpamInput = "/" + new string('a', 5000);

        // act
        var result = _searcher.FindSimilar(longSpamInput);

        // assert
        result.Should().BeEmpty();
    }
}