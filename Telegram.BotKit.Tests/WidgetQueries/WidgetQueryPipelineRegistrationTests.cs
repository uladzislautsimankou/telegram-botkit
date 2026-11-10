using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Telegram.BotKit.Shared.Pipeline;
using Telegram.BotKit.WidgetQueries.Abstractions;
using Telegram.BotKit.WidgetQueries.Models;
using Telegram.BotKit.WidgetQueries.Routing;

namespace Telegram.BotKit.Tests.WidgetQueries;

public sealed class WidgetQueryPipelineRegistrationTests
{
    private sealed class VoteWidgetQueryHandler : IWidgetQueryHandler<EmptyWidgetQueryParams>
    {
        public static string Key => "btn:vote";

        public Task HandleAsync(EmptyWidgetQueryParams parameters, WidgetQueryContext context, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class RefreshWidgetQueryHandler : IWidgetQueryHandler<EmptyWidgetQueryParams>
    {
        public static string Key => "btn:refresh";

        public Task HandleAsync(EmptyWidgetQueryParams parameters, WidgetQueryContext context, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class IdempotentWidgetQueryHandler : IWidgetQueryHandler<EmptyWidgetQueryParams>
    {
        public static string Key => "idem";

        public Task HandleAsync(EmptyWidgetQueryParams parameters, WidgetQueryContext context, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    // verifies that widget query handlers sharing identical parameter models register distinct registrations and factories without collisions
    [Fact]
    public async Task AddHandler_WhenMultipleHandlersShareSameParameters_ShouldRegisterDistinctRegistrations()
    {
        // arrange
        var services = new ServiceCollection();

        var registry = new WidgetQueryHandlerRegistry();
        var builder = new WidgetQueryPipelineBuilder(services, registry);

        // act
        builder.AddHandler<VoteWidgetQueryHandler>();
        builder.AddHandler<RefreshWidgetQueryHandler>();

        await using var provider = services.BuildServiceProvider();
        registry.Build();

        // assert
        registry.TryGetRegistration("btn:vote", out var voteRegistration).Should().BeTrue();
        registry.TryGetRegistration("btn:refresh", out var refreshRegistration).Should().BeTrue();

        voteRegistration!.HandlerType.Should().Be(typeof(VoteWidgetQueryHandler));
        refreshRegistration!.HandlerType.Should().Be(typeof(RefreshWidgetQueryHandler));

        voteRegistration.Factory(provider).Should().BeOfType<VoteWidgetQueryHandler>();
        refreshRegistration.Factory(provider).Should().BeOfType<RefreshWidgetQueryHandler>();
    }

    // verifies that multiple calls to userouting do not introduce duplicate steps
    [Fact]
    public void UseRouting_WhenCalledMultipleTimes_ShouldRegisterOnlySingleStep()
    {
        // arrange
        var services = new ServiceCollection();
        var builder = new WidgetQueryPipelineBuilder(services, new WidgetQueryHandlerRegistry());

        // act
        builder.UseRouting();
        builder.UseRouting();

        // assert
        builder.Steps.Should().ContainSingle(step => step.Type == PipelineStepType.Routing);
    }

    // verifies that adding the same handler twice is harmless
    [Fact]
    public void AddHandler_WhenSameHandlerAddedTwice_ShouldNotThrowAndRegisterOnce()
    {
        var services = new ServiceCollection();
        var builder = new WidgetQueryPipelineBuilder(services, new WidgetQueryHandlerRegistry());

        var act = () =>
        {
            builder.AddHandler<IdempotentWidgetQueryHandler>();
            builder.AddHandler<IdempotentWidgetQueryHandler>();
        };

        act.Should().NotThrow();
        services.Count(d => d.ServiceType == typeof(IdempotentWidgetQueryHandler)).Should().Be(1);
    }

    // verifies that routing and execution are added around the custom middleware when they are not configured explicitly
    [Fact]
    public void EnsureRoutingAndExecution_WhenNoStepsConfigured_ShouldAddRoutingFirstAndExecutionLast()
    {
        var builder = new WidgetQueryPipelineBuilder(new ServiceCollection(), new WidgetQueryHandlerRegistry());

        builder.EnsureRoutingAndExecution();

        builder.Steps.Select(s => s.Type).Should()
            .Equal(PipelineStepType.Routing, PipelineStepType.Execution);
        builder.Steps[0].MiddlewareType.Should().Be(typeof(WidgetQueryRoutingMiddleware));
        builder.Steps[1].MiddlewareType.Should().Be(typeof(WidgetQueryExecutionMiddleware));
    }
}
