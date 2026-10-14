using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Types;
using Telegram.BotKit.ChosenInlineResults.Abstractions;
using Telegram.BotKit.ChosenInlineResults.Models;
using Telegram.BotKit.ChosenInlineResults.Routing;

namespace Telegram.BotKit.Tests.ChosenInlineResults;

public sealed class ChosenInlineResultPipelineRegistrationTests
{
    private sealed class DummyChosenInlineResultHandler : IChosenInlineResultHandler
    {
        public static bool CanHandle(ChosenInlineResult chosenResult) => true;

        public Task HandleAsync(ChosenInlineResultContext context, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    // verifies that chosen inline result handlers are registered in the registry and service collection
    [Fact]
    public async Task AddHandler_WhenCalled_ShouldRegisterHandlerInRegistryAndDi()
    {
        // arrange
        var services = new ServiceCollection();
        var builder = new ChosenInlineResultPipelineBuilder(services);

        // act
        builder.AddHandler<DummyChosenInlineResultHandler>();

        await using var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<ChosenInlineResultHandlerRegistry>();
        registry.Build();

        // assert
        registry.GetHandlers().ToArray().Should().ContainSingle()
            .Which.HandlerType.Should().Be(typeof(DummyChosenInlineResultHandler));

        provider.GetService<DummyChosenInlineResultHandler>().Should().NotBeNull();
    }

    // verifies that duplicate chosen inline result handler registrations are ignored
    [Fact]
    public async Task AddHandler_WhenSameHandlerAddedTwice_ShouldNotDuplicateInRegistryOrDi()
    {
        // arrange
        var services = new ServiceCollection();
        var builder = new ChosenInlineResultPipelineBuilder(services);

        // act
        builder.AddHandler<DummyChosenInlineResultHandler>();
        builder.AddHandler<DummyChosenInlineResultHandler>();

        await using var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<ChosenInlineResultHandlerRegistry>();
        registry.Build();

        // assert
        registry.GetHandlers().ToArray().Should().ContainSingle()
            .Which.HandlerType.Should().Be(typeof(DummyChosenInlineResultHandler));

        provider.GetServices<DummyChosenInlineResultHandler>().Should().ContainSingle();
    }
}