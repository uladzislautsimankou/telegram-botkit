using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Types;
using Telegram.BotKit.Commands;
using Telegram.BotKit.Commands.Abstractions;
using Telegram.BotKit.Commands.Models;
using Telegram.BotKit.Commands.Routing;
using Telegram.BotKit.Shared.Pipeline;

namespace Telegram.BotKit.Tests.Shared.Pipeline;

public sealed class DispatcherScopeTests
{
    private sealed class ScopedProbe : IDisposable
    {
        public bool Disposed { get; private set; }

        public void Dispose() => Disposed = true;
    }

    private sealed class ProbeSink
    {
        public List<ScopedProbe> Probes { get; } = [];
    }

    private sealed class ProbeMiddleware(ScopedProbe probe, ProbeSink sink) : ICommandMiddleware
    {
        public ValueTask InvokeAsync(CommandContext context, NextDelegate next, CancellationToken cancellationToken = default)
        {
            sink.Probes.Add(probe);
            return next();
        }
    }

    private sealed class ThrowingProbeMiddleware(ScopedProbe probe, ProbeSink sink) : ICommandMiddleware
    {
        public ValueTask InvokeAsync(CommandContext context, NextDelegate next, CancellationToken cancellationToken = default)
        {
            sink.Probes.Add(probe);
            throw new InvalidOperationException("Simulated pipeline failure");
        }
    }

    private static readonly Message Message = new() { Id = 1, Chat = new Chat { Id = 1 }, Text = "/start" };

    private static (CommandDispatcher Dispatcher, ProbeSink Sink, ServiceProvider Provider) Create<TMiddleware>()
        where TMiddleware : class, ICommandMiddleware
    {
        var sink = new ProbeSink();
        var services = new ServiceCollection();
        services.AddSingleton(sink);
        services.AddScoped<ScopedProbe>();

        var pipelineBuilder = new CommandPipelineBuilder(services, new CommandHandlerRegistry());
        pipelineBuilder.UseMiddleware<TMiddleware>();
        var pipeline = pipelineBuilder.Build();

        var provider = services.BuildServiceProvider();
        return (new CommandDispatcher(provider, pipeline), sink, provider);
    }

    // verifies that the scope of an update is disposed when the pipeline completes
    [Fact]
    public async Task DispatchAsync_WhenPipelineCompletes_ShouldDisposeScope()
    {
        var (dispatcher, sink, provider) = Create<ProbeMiddleware>();
        await using var _ = provider;

        await dispatcher.DispatchAsync(Message, TestContext.Current.CancellationToken);

        sink.Probes.Should().ContainSingle().Which.Disposed.Should().BeTrue();
    }

    // verifies that the scope of an update is disposed also when the pipeline throws
    [Fact]
    public async Task DispatchAsync_WhenPipelineThrows_ShouldStillDisposeScope()
    {
        var (dispatcher, sink, provider) = Create<ThrowingProbeMiddleware>();
        await using var _ = provider;

        var act = () => dispatcher.DispatchAsync(Message, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>();
        sink.Probes.Should().ContainSingle().Which.Disposed.Should().BeTrue();
    }

    // verifies that scoped services are not shared between two updates
    [Fact]
    public async Task DispatchAsync_WhenCalledTwice_ShouldUseSeparateScopes()
    {
        var (dispatcher, sink, provider) = Create<ProbeMiddleware>();
        await using var _ = provider;

        await dispatcher.DispatchAsync(Message, TestContext.Current.CancellationToken);
        await dispatcher.DispatchAsync(Message, TestContext.Current.CancellationToken);

        sink.Probes.Should().HaveCount(2);
        sink.Probes[0].Should().NotBeSameAs(sink.Probes[1]);
    }
}
