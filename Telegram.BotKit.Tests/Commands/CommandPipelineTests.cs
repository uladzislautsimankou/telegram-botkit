using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.BotKit.Commands;
using Telegram.BotKit.Commands.Abstractions;
using Telegram.BotKit.Commands.Models;
using Telegram.BotKit.Hosting;
using Telegram.BotKit.Shared.Converters;
using Telegram.BotKit.Shared.Pipeline;

namespace Telegram.BotKit.Tests.Commands;

public sealed class CommandPipelineTests
{
    private sealed class CallLog
    {
        public List<string> Entries { get; } = [];
    }

    private sealed class PingHandler : ICommandHandler<EmptyCommandParams>
    {
        private readonly CallLog _log;

        public PingHandler(CallLog log)
        {
            _log = log;
            log.Entries.Add("ctor:ping");
        }

        public static string Command => "ping";

        public Task HandleAsync(EmptyCommandParams parameters, CommandContext context, CancellationToken cancellationToken = default)
        {
            _log.Entries.Add("handler:ping");
            return Task.CompletedTask;
        }
    }

    private sealed class BeforeRoutingMiddleware(CallLog log) : ICommandMiddleware
    {
        public ValueTask InvokeAsync(CommandContext context, NextDelegate next, CancellationToken cancellationToken = default)
        {
            log.Entries.Add($"before:{context.RouteResult.Status}");
            return next();
        }
    }

    private sealed class StopAfterRoutingMiddleware(CallLog log) : ICommandMiddleware
    {
        public ValueTask InvokeAsync(CommandContext context, NextDelegate next, CancellationToken cancellationToken = default)
        {
            log.Entries.Add("stopped");
            return ValueTask.CompletedTask;
        }
    }

    private sealed class AfterRoutingMiddleware(CallLog log) : ICommandMiddleware
    {
        public ValueTask InvokeAsync(CommandContext context, NextDelegate next, CancellationToken cancellationToken = default)
        {
            log.Entries.Add($"after:{context.RouteResult.Status}");
            return next();
        }
    }

    private sealed class BlockingMiddleware(CallLog log) : ICommandMiddleware
    {
        public ValueTask InvokeAsync(CommandContext context, NextDelegate next, CancellationToken cancellationToken = default)
        {
            log.Entries.Add("blocked");
            return ValueTask.CompletedTask;
        }
    }

    private static ServiceProvider CreateProvider(CallLog log, Action<CommandPipelineBuilder> configure)
    {
        var botInfo = Substitute.For<IBotInfo>();
        botInfo.Username.Returns("TestBot");

        var services = new ServiceCollection();
        services.AddSingleton(log);
        services.AddSingleton(botInfo);
        services.AddSingleton<IValueConverter, DefaultValueConverter>();

        // the library assembly declares no handlers, so only explicitly added ones are registered
        services.AddCommands(configure, typeof(CommandSearcher).Assembly);

        return services.BuildServiceProvider();
    }

    private static Message CreateMessage(string text, ChatType chatType) => new()
    {
        Id = 1,
        Chat = new Chat { Id = 1, Type = chatType },
        From = new User { Id = 2, FirstName = "User" },
        Text = text
    };


    // verifies that middleware placed after routing runs for every routing outcome and the handler runs only on success
    [Theory]
    [InlineData("/ping", ChatType.Private, CommandRouteStatus.Success)]
    [InlineData("/ping@TestBot", ChatType.Group, CommandRouteStatus.Success)]
    [InlineData("/pnig", ChatType.Private, CommandRouteStatus.Suggestions)]
    [InlineData("/zzzzzz", ChatType.Private, CommandRouteStatus.NotFound)]
    [InlineData("/zzzzzz", ChatType.Group, CommandRouteStatus.Ignored)]
    [InlineData("/ping@OtherBot", ChatType.Group, CommandRouteStatus.Ignored)]
    public async Task DispatchAsync_WhenCommandIsRouted_ShouldRunMiddlewareAfterRoutingForEveryOutcome(
        string text,
        ChatType chatType,
        CommandRouteStatus expectedStatus)
    {
        // arrange
        var log = new CallLog();
        await using var provider = CreateProvider(log, p => p
            .UseMiddleware<BeforeRoutingMiddleware>()
            .UseRouting()
            .UseMiddleware<AfterRoutingMiddleware>()
            .AddHandler<PingHandler>());

        var dispatcher = provider.GetRequiredService<ICommandDispatcher>();

        // act
        await dispatcher.DispatchAsync(CreateMessage(text, chatType), TestContext.Current.CancellationToken);

        // assert
        var expected = new List<string> { "before:None", $"after:{expectedStatus}" };
        if (expectedStatus is CommandRouteStatus.Success)
        {
            expected.Add("ctor:ping");
            expected.Add("handler:ping");
        }

        log.Entries.Should().Equal(expected);
    }

    // verifies that a middleware before routing can stop the command so neither routing nor the handler run
    [Fact]
    public async Task DispatchAsync_WhenMiddlewareBeforeRoutingDoesNotCallNext_ShouldSkipRoutingAndHandler()
    {
        // arrange
        var log = new CallLog();
        await using var provider = CreateProvider(log, p => p
            .UseMiddleware<BlockingMiddleware>()
            .UseRouting()
            .UseMiddleware<AfterRoutingMiddleware>()
            .AddHandler<PingHandler>());

        var dispatcher = provider.GetRequiredService<ICommandDispatcher>();

        // act
        await dispatcher.DispatchAsync(CreateMessage("/ping", ChatType.Private), TestContext.Current.CancellationToken);

        // assert
        log.Entries.Should().Equal("blocked");
    }

    // verifies that routing is inserted before all user middleware when UseRouting is not called explicitly
    [Fact]
    public async Task DispatchAsync_WhenRoutingIsNotConfigured_ShouldInsertItBeforeUserMiddleware()
    {
        // arrange
        var log = new CallLog();
        await using var provider = CreateProvider(log, p => p
            .UseMiddleware<AfterRoutingMiddleware>()
            .AddHandler<PingHandler>());

        var dispatcher = provider.GetRequiredService<ICommandDispatcher>();

        // act
        await dispatcher.DispatchAsync(CreateMessage("/ping", ChatType.Private), TestContext.Current.CancellationToken);

        // assert
        log.Entries.Should().Equal("after:Success", "ctor:ping", "handler:ping");
    }

    // verifies that a middleware stopping the pipeline after routing prevents the handler from being constructed
    [Fact]
    public async Task DispatchAsync_WhenMiddlewareAfterRoutingStopsPipeline_ShouldNotCreateHandler()
    {
        // arrange
        var log = new CallLog();
        await using var provider = CreateProvider(log, p => p
            .UseRouting()
            .UseMiddleware<StopAfterRoutingMiddleware>()
            .AddHandler<PingHandler>());

        var dispatcher = provider.GetRequiredService<ICommandDispatcher>();

        // act
        await dispatcher.DispatchAsync(CreateMessage("/ping", ChatType.Private), TestContext.Current.CancellationToken);

        // assert
        log.Entries.Should().Equal("stopped");
    }
}
