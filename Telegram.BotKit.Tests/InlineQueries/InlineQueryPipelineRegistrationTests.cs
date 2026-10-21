using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Types;
using Telegram.BotKit.InlineQueries;
using Telegram.BotKit.InlineQueries.Abstractions;
using Telegram.BotKit.InlineQueries.Middlewares;
using Telegram.BotKit.InlineQueries.Models;
using Telegram.BotKit.InlineQueries.Routing;
using Telegram.BotKit.Shared.Pipeline;

namespace Telegram.BotKit.Tests.InlineQueries;

public sealed class InlineQueryPipelineRegistrationTests
{
    private sealed class DummyInlineQueryHandler : IInlineQueryHandler
    {
        public static bool CanHandle(InlineQuery query) => true;

        public Task<InlineQueryBatch> HandleAsync(InlineQueryContext context, CancellationToken cancellationToken = default) =>
            Task.FromResult(new InlineQueryBatch());
    }

    private sealed class CustomAnswerMiddleware : IInlineQueryAnswerMiddleware
    {
        public ValueTask InvokeAsync(InlineQueryContext context, NextDelegate next, CancellationToken cancellationToken = default) =>
            next();
    }

    private sealed class AuditMiddleware : IInlineQueryAnswerMiddleware
    {
        public ValueTask InvokeAsync(InlineQueryContext context, NextDelegate next, CancellationToken cancellationToken = default) =>
            next();
    }

    // verifies that a handler is registered as a descriptor in the registry and as a concrete type in DI
    [Fact]
    public async Task AddHandler_WhenCalled_ShouldRegisterDescriptorInRegistryAndHandlerInDi()
    {
        // arrange
        var services = new ServiceCollection();
        var registry = new InlineQueryHandlerRegistry();
        var builder = new InlineQueryPipelineBuilder(services, registry);

        // act
        builder.AddHandler<DummyInlineQueryHandler>();

        await using var provider = services.BuildServiceProvider();
        registry.Build();

        // assert
        var descriptor = registry.GetHandlers().ToArray().Should().ContainSingle().Which;
        descriptor.HandlerType.Should().Be(typeof(DummyInlineQueryHandler));
        descriptor.CanHandle(new InlineQuery { Id = "1", Query = "q", From = new User { Id = 1, FirstName = "U" } }).Should().BeTrue();
        descriptor.Factory(provider).Should().BeOfType<DummyInlineQueryHandler>();
    }

    // verifies that repeated registration of the same handler does not create duplicate entries
    [Fact]
    public async Task AddHandler_WhenHandlerRegisteredMultipleTimes_ShouldNotDuplicateInRegistryOrDi()
    {
        // arrange
        var services = new ServiceCollection();
        var registry = new InlineQueryHandlerRegistry();
        var builder = new InlineQueryPipelineBuilder(services, registry);

        // act
        builder.AddHandler<DummyInlineQueryHandler>();
        builder.AddHandler<DummyInlineQueryHandler>();

        await using var provider = services.BuildServiceProvider();
        registry.Build();

        // assert
        registry.GetHandlers().ToArray().Should().ContainSingle();
        provider.GetServices<DummyInlineQueryHandler>().Should().ContainSingle();
    }

    // verifies that calling userouting multiple times preserves a single routing step
    [Fact]
    public void UseRouting_WhenCalledMultipleTimes_ShouldOnlyAddOneRoutingStep()
    {
        // arrange
        var services = new ServiceCollection();
        var builder = new InlineQueryPipelineBuilder(services, new InlineQueryHandlerRegistry());

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
        var builder = new InlineQueryPipelineBuilder(services, new InlineQueryHandlerRegistry());

        // act
        builder.UseAnswer<CustomAnswerMiddleware>();
        builder.UseAnswer<CustomAnswerMiddleware>();
        builder.EnsureRoutingAndExecution();

        // assert
        builder.Steps.Where(s => s.Type == PipelineStepType.Execution).Should().ContainSingle()
            .Which.MiddlewareType.Should().Be(typeof(CustomAnswerMiddleware));
    }

    // verifies that the answer middleware runs last wherever useanswer is called
    [Fact]
    public void UseAnswer_WhenCalledBeforeOtherMiddleware_ShouldKeepExecutionStepLast()
    {
        // arrange
        var services = new ServiceCollection();
        var builder = new InlineQueryPipelineBuilder(services, new InlineQueryHandlerRegistry());

        // act
        builder.UseAnswer<CustomAnswerMiddleware>();
        builder.UseMiddleware<AuditMiddleware>();
        builder.EnsureRoutingAndExecution();

        // assert
        builder.Steps.Select(s => s.Type).Should().Equal(
            PipelineStepType.Routing,
            PipelineStepType.UserMiddleware,
            PipelineStepType.Execution);
        builder.Steps[^1].MiddlewareType.Should().Be(typeof(CustomAnswerMiddleware));
    }

    // verifies that default pipeline registration registers default answer middleware
    [Fact]
    public void AddInlineQueries_WhenNoStepsSpecified_ShouldAutoConfigureDefaultAnswerMiddleware()
    {
        // arrange
        var services = new ServiceCollection();

        // act
        services.AddInlineQueries(assemblies: typeof(InlineQueryDispatcher).Assembly);

        // assert
        services.Should().Contain(d => d.ServiceType == typeof(IInlineQueryAnswerMiddleware)
            && d.ImplementationType == typeof(DefaultInlineQueryAnswerMiddleware));
    }

    // verifies that handlers run in parallel unless configured otherwise
    [Fact]
    public void AddInlineQueries_WhenExecutionModeIsNotConfigured_ShouldDefaultToParallel()
    {
        // arrange
        var services = new ServiceCollection();

        // act
        services.AddInlineQueries(assemblies: typeof(InlineQueryDispatcher).Assembly);

        // assert
        GetSettings(services).ExecutionMode.Should().Be(InlineQueryExecutionMode.Parallel);
    }

    // verifies that the configured execution mode reaches the routing settings
    [Fact]
    public void AddInlineQueries_WhenExecutionModeIsConfigured_ShouldExposeItInSettings()
    {
        // arrange
        var services = new ServiceCollection();

        // act
        services.AddInlineQueries(
            builder => builder.WithExecutionMode(InlineQueryExecutionMode.Sequential),
            typeof(InlineQueryDispatcher).Assembly);

        // assert
        GetSettings(services).ExecutionMode.Should().Be(InlineQueryExecutionMode.Sequential);
    }

    private static InlineQueryPipelineSettings GetSettings(IServiceCollection services) =>
        services.Single(d => d.ServiceType == typeof(InlineQueryPipelineSettings))
            .ImplementationInstance.Should().BeOfType<InlineQueryPipelineSettings>().Subject;
}
