using FluentAssertions;
using Telegram.BotKit.Commands.Models;

namespace Telegram.BotKit.Tests.Commands;

public sealed class CommandRouteResultTests
{
    // verifies that the default struct value is safe to read: no status and non-null empty suggestions
    [Fact]
    public void Default_ShouldHaveNoStatusAndEmptySuggestions()
    {
        var result = default(CommandRouteResult);

        result.Status.Should().Be(CommandRouteStatus.None);
        result.Suggestions.Should().NotBeNull().And.BeEmpty();
    }

    // verifies that factory methods without suggestions return an empty list and the right status
    [Theory]
    [InlineData(CommandRouteStatus.Success)]
    [InlineData(CommandRouteStatus.NotFound)]
    [InlineData(CommandRouteStatus.Ignored)]
    [InlineData(CommandRouteStatus.VirtualRouteNotFound)]
    public void Factories_WhenNoSuggestionsAreNeeded_ShouldReturnStatusWithEmptySuggestions(CommandRouteStatus status)
    {
        var result = status switch
        {
            CommandRouteStatus.Success => CommandRouteResult.Success(),
            CommandRouteStatus.NotFound => CommandRouteResult.NotFound(),
            CommandRouteStatus.Ignored => CommandRouteResult.Ignored(),
            _ => CommandRouteResult.VirtualRouteNotFound()
        };

        result.Status.Should().Be(status);
        result.Suggestions.Should().BeEmpty();
    }

    // verifies that suggestions are kept in the given order
    [Fact]
    public void SuggestionsFound_ShouldKeepSuggestionsInOrder()
    {
        var result = CommandRouteResult.SuggestionsFound(["ping", "pins"]);

        result.Status.Should().Be(CommandRouteStatus.Suggestions);
        result.Suggestions.Should().Equal("ping", "pins");
    }
}
