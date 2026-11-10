using FluentAssertions;
using Telegram.Bot.Types;
using Telegram.BotKit.WidgetQueries.Models;

namespace Telegram.BotKit.Tests.WidgetQueries;

public sealed class WidgetQueryContextTests
{
    // verifies that a query of an original message is exposed as it is and is not a copy
    [Fact]
    public void Ctor_WhenOriginalQueryIsProvided_ShouldExposeQueryFields()
    {
        // arrange
        var query = WidgetQueryTestData.Original("poll:vote?o=2", widgetId: "widget_1");

        // act
        var context = new WidgetQueryContext(query);

        // assert
        context.Query.Should().BeSameAs(query);
        context.SharedQuery.Should().BeNull();
        context.IsCopy.Should().BeFalse();
        context.Id.Should().Be(query.Id);
        context.From.Should().BeSameAs(query.From);
        context.Data.Should().Be("poll:vote?o=2");
        context.WidgetId.Should().Be("widget_1");
    }

    // verifies that the data of the query is split into a route key and parameters, as callback data is
    [Fact]
    public void Ctor_WhenDataHasParameters_ShouldParseKeyAndRawParams()
    {
        var context = WidgetQueryTestData.Context("poll:vote?o=2&u=abc");

        context.Key.Should().Be("poll:vote");
        context.RawParams.Should().HaveCount(2);
        context.RawParams["o"].Should().Be("2");
        context.RawParams["u"].Should().Be("abc");
    }

    // verifies that empty or whitespace data gives an empty key, which no handler is registered for
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Ctor_WhenDataIsEmpty_ShouldHaveEmptyKeyAndNoParams(string data)
    {
        var context = WidgetQueryTestData.Context(data);

        context.Key.Should().BeEmpty();
        context.RawParams.Should().BeEmpty();
    }

    // verifies that a message in a chat exposes the chat and the message id
    [Fact]
    public void Ctor_WhenOriginalQueryIsInChat_ShouldExposeChatAndMessageId()
    {
        var query = WidgetQueryTestData.Original("test");

        var context = new WidgetQueryContext(query);

        context.Chat.Should().BeSameAs(query.Chat);
        context.MessageId.Should().Be(123);
        context.InlineMessageId.Should().BeNull();
    }

    // verifies that a message sent via the bot exposes the inline message id only
    [Fact]
    public void Ctor_WhenOriginalQueryIsViaBot_ShouldExposeInlineMessageIdOnly()
    {
        var context = new WidgetQueryContext(WidgetQueryTestData.ViaBot("test"));

        context.IsCopy.Should().BeFalse();
        context.InlineMessageId.Should().Be("inline_1");
        context.Chat.Should().BeNull();
        context.MessageId.Should().BeNull();
    }

    // verifies that a static widget has no widget id
    [Fact]
    public void Ctor_WhenWidgetIsStatic_ShouldHaveNoWidgetId()
    {
        var context = WidgetQueryTestData.Context("test");

        context.WidgetId.Should().BeNull();
    }

    // verifies that a query of a forwarded copy is a copy and tells nothing about the chat
    [Fact]
    public void Ctor_WhenSharedQueryIsProvided_ShouldBeCopyWithoutChatInformation()
    {
        // arrange
        var query = WidgetQueryTestData.Copy("poll:vote?o=1", widgetId: "widget_7");

        // act
        var context = new WidgetQueryContext(query);

        // assert
        context.SharedQuery.Should().BeSameAs(query);
        context.Query.Should().BeNull();
        context.IsCopy.Should().BeTrue();
        context.Id.Should().Be(query.Id);
        context.From.Should().BeSameAs(query.From);
        context.WidgetId.Should().Be("widget_7");
        context.Chat.Should().BeNull();
        context.MessageId.Should().BeNull();
        context.InlineMessageId.Should().BeNull();
    }

    // verifies that the data of a copy is parsed like the data of an original
    [Fact]
    public void Ctor_WhenSharedQueryHasParameters_ShouldParseKeyAndRawParams()
    {
        var context = new WidgetQueryContext(WidgetQueryTestData.Copy("poll:vote?o=1"));

        context.Key.Should().Be("poll:vote");
        context.RawParams["o"].Should().Be("1");
    }

    // verifies that a null query of either kind is rejected
    [Fact]
    public void Ctor_WhenOriginalQueryIsNull_ShouldThrowArgumentNullException()
    {
        var act = () => new WidgetQueryContext((WidgetQuery)null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("query");
    }

    [Fact]
    public void Ctor_WhenSharedQueryIsNull_ShouldThrowArgumentNullException()
    {
        var act = () => new WidgetQueryContext((SharedWidgetQuery)null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("query");
    }

    // verifies that the type of the query is exposed as it is, for a query of an original message
    [Fact]
    public void Ctor_WhenOriginalQueryHasType_ShouldExposeQueryType()
    {
        var type = new WidgetQueryTypeSignData();

        var context = new WidgetQueryContext(WidgetQueryTestData.Original("test", type: type));

        context.QueryType.Should().BeSameAs(type);
    }

    // verifies that the type of the query is exposed as it is, for a query of a forwarded copy
    [Fact]
    public void Ctor_WhenSharedQueryHasType_ShouldExposeQueryType()
    {
        var type = new WidgetQueryTypeSentTransaction();

        var context = new WidgetQueryContext(WidgetQueryTestData.Copy("test", type: type));

        context.QueryType.Should().BeSameAs(type);
    }

    // verifies that the type does not change what the data is parsed into: every type is routed by the key
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Ctor_WhenQueryTypeVaries_ShouldParseDataTheSameWay(int typeIndex)
    {
        WidgetQueryType type = typeIndex switch
        {
            0 => new WidgetQueryTypeData(),
            1 => new WidgetQueryTypeSignData(),
            _ => new WidgetQueryTypeSentTransaction()
        };

        var context = new WidgetQueryContext(WidgetQueryTestData.Original("poll:vote?o=2", type: type));

        context.Key.Should().Be("poll:vote");
        context.RawParams["o"].Should().Be("2");
    }

    // verifies that a new context is not matched until the routing middleware runs
    [Fact]
    public void Ctor_WhenCreated_ShouldHaveNoMatchedHandler()
    {
        var context = WidgetQueryTestData.Context("test");

        context.MatchedWidgetQueryMetadata.Should().BeNull();
        context.MatchedRegistration.Should().BeNull();
    }
}
