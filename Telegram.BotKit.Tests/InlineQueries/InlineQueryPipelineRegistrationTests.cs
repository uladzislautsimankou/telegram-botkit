using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Types;
using Telegram.BotKit.InlineQueries.Abstractions;
using Telegram.BotKit.InlineQueries.Models;

namespace Telegram.BotKit.Tests.InlineQueries;

public sealed class InlineQueryPipelineRegistrationTests
{
    private sealed class DummyInlineQueryHandler : IInlineQueryHandler
    {
        public bool CanHandle(InlineQuery query) => true;

        public Task<InlineQueryBatch> HandleAsync(InlineQueryContext context, CancellationToken cancellationToken = default) =>
            Task.FromResult(new InlineQueryBatch());
    }

    // verifies that inline query handlers are registered under their interface in the service collection
    [Fact]
    public async Task AddHandler_WhenCalled_ShouldRegisterHandlerUnderInterface()
    {
        // arrange
        var services = new ServiceCollection();
        var builder = new InlineQueryPipelineBuilder(services);

        // act
        builder.AddHandler<DummyInlineQueryHandler>();

        await using var provider = services.BuildServiceProvider();
        var handler = provider.GetService<IInlineQueryHandler>();

        // assert
        handler.Should().NotBeNull();
        handler.Should().BeOfType<DummyInlineQueryHandler>();
    }

    // verifies that repeated registration of the same handler does not duplicate entries under the interface
    [Fact]
    public async Task AddHandler_WhenHandlerRegisteredMultipleTimes_ShouldNotDuplicateUnderInterface()
    {
        // arrange
        var services = new ServiceCollection();
        var builder = new InlineQueryPipelineBuilder(services);

        // act
        builder.AddHandler<DummyInlineQueryHandler>();
        builder.AddHandler<DummyInlineQueryHandler>();

        await using var provider = services.BuildServiceProvider();
        var handlers = provider.GetServices<IInlineQueryHandler>();

        // assert
        handlers.Should().ContainSingle().Which.Should().BeOfType<DummyInlineQueryHandler>();
    }
}