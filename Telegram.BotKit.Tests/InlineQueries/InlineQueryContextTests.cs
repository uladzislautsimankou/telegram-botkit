using FluentAssertions;
using NSubstitute;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.BotKit.InlineQueries.Models;

namespace Telegram.BotKit.Tests.InlineQueries;

public sealed class InlineQueryContextTests
{
    private static InlineQueryContext CreateContext() =>
        new(new InlineQuery { Id = "1", Query = "q", From = new User { Id = 1, FirstName = "User" } });

    // verifies that the personal flag cannot be switched off once any handler requested it
    [Fact]
    public void MergeConfiguration_WhenPersonalWasRequested_ShouldStayPersonal()
    {
        var context = CreateContext();

        context.MergeConfiguration(true, null, null, null);
        context.MergeConfiguration(false, null, null, null);

        context.IsPersonal.Should().BeTrue();
    }

    // verifies that the smallest specified cache time wins
    [Theory]
    [InlineData(600, 300, 300)]
    [InlineData(300, 600, 300)]
    [InlineData(null, 600, 600)]
    [InlineData(600, null, 600)]
    [InlineData(null, null, null)]
    public void MergeConfiguration_WhenCacheTimesAreMerged_ShouldKeepSmallestSpecifiedValue(int? first, int? second, int? expected)
    {
        var context = CreateContext();

        context.MergeConfiguration(false, first, null, null);
        context.MergeConfiguration(false, second, null, null);

        context.CacheTime.Should().Be(expected);
    }

    // verifies that the latest non-empty next offset wins and empty values never overwrite it
    [Theory]
    [InlineData("page2", null, "page2")]
    [InlineData("page2", "", "page2")]
    [InlineData(null, "page3", "page3")]
    [InlineData("page2", "page3", "page3")]
    public void MergeConfiguration_WhenNextOffsetsAreMerged_ShouldKeepLatestNonEmptyValue(string? first, string? second, string expected)
    {
        var context = CreateContext();

        context.MergeConfiguration(false, null, first, null);
        context.MergeConfiguration(false, null, second, null);

        context.NextOffset.Should().Be(expected);
    }

    // verifies that only replacing an existing button is reported as a conflict
    [Fact]
    public void MergeConfiguration_WhenButtonsAreMerged_ShouldReportConflictOnlyOnReplacement()
    {
        var context = CreateContext();
        var first = new InlineQueryResultsButton("First");
        var second = new InlineQueryResultsButton("Second");

        context.MergeConfiguration(false, null, null, first).Should().BeFalse();
        context.MergeConfiguration(false, null, null, null).Should().BeFalse();
        context.Button.Should().BeSameAs(first);

        context.MergeConfiguration(false, null, null, second).Should().BeTrue();
        context.Button.Should().BeSameAs(second);
    }

    // verifies that results and exceptions are exposed as snapshots that later additions do not change
    [Fact]
    public void Collections_WhenReadBeforeFurtherAdditions_ShouldReturnStableSnapshots()
    {
        var context = CreateContext();

        var resultsSnapshot = context.Results;
        var exceptionsSnapshot = context.Exceptions;
        context.AddResults([Substitute.For<InlineQueryResult>()]);
        context.AddException(new InvalidOperationException());

        resultsSnapshot.Should().BeEmpty();
        exceptionsSnapshot.Should().BeEmpty();
        context.Results.Should().HaveCount(1);
        context.Exceptions.Should().HaveCount(1);
    }
}
