using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Types;
using Telegram.BotKit.ChosenInlineResults.Abstractions;
using Telegram.BotKit.ChosenInlineResults.Models;

namespace Telegram.BotKit.Tests.ChosenInlineResults;

public sealed class ChosenInlineResultPipelineRegistrationTests
{
    private sealed class DummyChosenInlineResultHandler : IChosenInlineResultHandler
    {
        public bool CanHandle(ChosenInlineResult chosenResult) => true;

        public Task HandleAsync(ChosenInlineResultContext context, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    // verifies that chosen inline result handlers are registered under their interface in the service collection
    [Fact]
    public void AddHandler_WhenCalled_ShouldRegisterHandlerUnderInterface()
    {
        // arrange
        var services = new ServiceCollection();
        var builder = new ChosenInlineResultPipelineBuilder(services);

        // act
        builder.AddHandler<DummyChosenInlineResultHandler>();

        var provider = services.BuildServiceProvider();
        var handler = provider.GetService<IChosenInlineResultHandler>();

        // assert
        handler.Should().NotBeNull();
        handler.Should().BeOfType<DummyChosenInlineResultHandler>();
    }
}