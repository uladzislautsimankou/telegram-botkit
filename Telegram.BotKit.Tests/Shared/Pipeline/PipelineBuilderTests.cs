using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Telegram.BotKit.Commands.Abstractions;
using Telegram.BotKit.Commands.Models;
using Telegram.BotKit.Commands.Routing;
using Telegram.BotKit.Shared.Pipeline;

namespace Telegram.BotKit.Tests.Shared.Pipeline;

public sealed class PipelineBuilderTests
{
    private sealed class CustomMiddleware : ICommandMiddleware
    {
        public ValueTask InvokeAsync(CommandContext context, NextDelegate next, CancellationToken cancellationToken = default) =>
            next();
    }

    private static CommandPipelineBuilder CreateBuilder() => new(new ServiceCollection(), new CommandHandlerRegistry());

    private static Type[] StepTypes(CommandPipelineBuilder builder) =>
        builder.Steps.Select(s => s.MiddlewareType).ToArray();

    // verifies that an empty pipeline gets routing followed by execution
    [Fact]
    public void EnsureRoutingAndExecution_WhenNoStepsConfigured_ShouldAddRoutingThenExecution()
    {
        var builder = CreateBuilder();

        builder.EnsureRoutingAndExecution();

        StepTypes(builder).Should().Equal(typeof(CommandRoutingMiddleware), typeof(CommandExecutionMiddleware));
    }

    // verifies that routing is inserted before user middleware when UseRouting was not called
    [Fact]
    public void EnsureRoutingAndExecution_WhenOnlyUserMiddlewareConfigured_ShouldInsertRoutingFirst()
    {
        var builder = CreateBuilder().UseMiddleware<CustomMiddleware>();

        builder.EnsureRoutingAndExecution();

        StepTypes(builder).Should().Equal(
            typeof(CommandRoutingMiddleware), typeof(CustomMiddleware), typeof(CommandExecutionMiddleware));
    }

    // verifies that an explicitly placed routing step keeps its position
    [Fact]
    public void EnsureRoutingAndExecution_WhenRoutingIsPlacedExplicitly_ShouldKeepItsPosition()
    {
        var builder = CreateBuilder().UseMiddleware<CustomMiddleware>().UseRouting();

        builder.EnsureRoutingAndExecution();

        StepTypes(builder).Should().Equal(
            typeof(CustomMiddleware), typeof(CommandRoutingMiddleware), typeof(CommandExecutionMiddleware));
    }

    // verifies that completing the pipeline twice does not duplicate steps
    [Fact]
    public void EnsureRoutingAndExecution_WhenCalledTwice_ShouldNotDuplicateSteps()
    {
        var builder = CreateBuilder();

        builder.EnsureRoutingAndExecution();
        builder.EnsureRoutingAndExecution();

        builder.Steps.Should().HaveCount(2);
    }
}

