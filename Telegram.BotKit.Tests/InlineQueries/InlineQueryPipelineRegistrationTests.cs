using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Types;
using Telegram.BotKit.InlineQueries.Abstractions;
using Telegram.BotKit.InlineQueries.Middlewares;
using Telegram.BotKit.InlineQueries.Models;
using Telegram.BotKit.InlineQueries.Routing;
using Telegram.BotKit.Shared;
using Telegram.BotKit.Shared.Pipeline;

namespace Telegram.BotKit.Tests.InlineQueries;

public sealed class InlineQueryPipelineRegistrationTests
{
    private sealed class DummyInlineQueryHandler : IInlineQueryHandler
    {
        public bool CanHandle(InlineQuery query) => true;

        public Task<InlineQueryBatch> HandleAsync(InlineQueryContext context, CancellationToken cancellationToken = default) =>
            Task.FromResult(new InlineQueryBatch());
    }

    private sealed class CustomAnswerMiddleware : IInlineQueryAnswerMiddleware
    {
        public ValueTask InvokeAsync(InlineQueryContext context, NextDelegate next, CancellationToken cancellationToken = default) =>
            next();
    }

    // verifies that inline query handler registers under its interface in service collection
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

    // verifies that repeated registration of identical handler does not create duplicate entries
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

    // verifies that calling userouting multiple times preserves a single routing step
    [Fact]
    public void UseRouting_WhenCalledMultipleTimes_ShouldOnlyAddOneRoutingStep()
    {
        // arrange
        var services = new ServiceCollection();
        var builder = new InlineQueryPipelineBuilder(services);

        // act
        builder.UseRouting();
        builder.UseRouting();
        builder.UseRouting();

        // assert
        builder.Steps.Where(s => s.Type == PipelineStepType.Routing).Should().ContainSingle()
            .Which.MiddlewareType.Should().Be(typeof(InlineQueryRoutingMiddleware));
    }

    // verifies that calling useanswer multiple times preserves a single execution step
    [Fact]
    public void UseAnswer_WhenCalledMultipleTimes_ShouldOnlyAddOneExecutionStep()
    {
        // arrange
        var services = new ServiceCollection();
        var builder = new InlineQueryPipelineBuilder(services);

        // act
        builder.UseAnswer<CustomAnswerMiddleware>();
        builder.UseAnswer<CustomAnswerMiddleware>();

        // assert
        builder.Steps.Where(s => s.Type == PipelineStepType.Execution).Should().ContainSingle()
            .Which.MiddlewareType.Should().Be(typeof(CustomAnswerMiddleware));
    }

    // verifies that default pipeline registration registers default answer middleware
    [Fact]
    public void AddInlineQueries_WhenNoStepsSpecified_ShouldAutoConfigureDefaultAnswerMiddleware()
    {
        // arrange
        var services = new ServiceCollection();

        // act
        services.AddInlineQueries();

        // assert
        services.Should().Contain(d => d.ServiceType == typeof(IInlineQueryAnswerMiddleware)
                                       && d.ImplementationType == typeof(DefaultInlineQueryAnswerMiddleware));
    }
}