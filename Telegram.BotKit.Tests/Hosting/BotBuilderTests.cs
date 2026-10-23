using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.BotKit.Commands.Abstractions;
using Telegram.BotKit.Commands.Attributes;
using Telegram.BotKit.Commands.Models;
using Telegram.BotKit.Hosting;
using Telegram.BotKit.Shared.Exceptions;

namespace Telegram.BotKit.Tests.Hosting;

public sealed class BotBuilderTests
{
    private interface IMissingDependency;

    private sealed class UnsupportedValue;

    private sealed class BadModelParams
    {
        [CommandParam(Position = 0)]
        public UnsupportedValue? Value { get; set; }
    }

    private sealed class BadModelHandler : ICommandHandler<BadModelParams>
    {
        public static string Command => "bad_model";

        public Task HandleAsync(BadModelParams parameters, CommandContext context, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class HandlerWithMissingDependency(IMissingDependency dependency) : IUpdateHandler
    {
        public IMissingDependency Dependency { get; } = dependency;

        public Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private static BotBuilder CreateBuilder(string mode) => BotApplication.CreateBuilder(
    [
        "--TelegramBot:Token=123456:ABC",
        $"--TelegramBot:Mode={mode}",
        "--TelegramBot:WebhookUrl=https://example.com/api/bot"
    ]);

    // verifies that the environment passed on the command line is honored
    [Fact]
    public void CreateBuilder_WhenEnvironmentIsPassedAsArgument_ShouldUseIt()
    {
        var builder = BotApplication.CreateBuilder(["--environment=Staging"]);

        builder.Environment.EnvironmentName.Should().Be("Staging");
    }

    // verifies that webhook mode builds on the web application host
    [Theory]
    [InlineData("Webhook")]
    [InlineData("webhook")]
    public void CreateBuilder_WhenModeIsWebhook_ShouldUseWebApplicationBuilder(string mode)
    {
        var builder = BotApplication.CreateBuilder([$"--TelegramBot:Mode={mode}"]);

        builder.WebApplicationBuilder.Should().NotBeNull();
        builder.HostBuilder.Should().BeNull();
    }

    // verifies that polling mode builds on the generic host
    [Theory]
    [InlineData("Polling")]
    [InlineData("polling")]
    public void CreateBuilder_WhenModeIsPolling_ShouldUseGenericHostBuilder(string mode)
    {
        var builder = BotApplication.CreateBuilder([$"--TelegramBot:Mode={mode}"]);

        builder.HostBuilder.Should().NotBeNull();
        builder.WebApplicationBuilder.Should().BeNull();
    }

    // verifies that unknown and numeric (undefined) modes are rejected
    [Theory]
    [InlineData("Carrier")]
    [InlineData("7")]
    public void CreateBuilder_WhenModeIsInvalid_ShouldThrowBotStartupException(string mode)
    {
        var act = () => BotApplication.CreateBuilder([$"--TelegramBot:Mode={mode}"]);

        act.Should().Throw<BotStartupException>().WithMessage("*Invalid TelegramBot:Mode*");
    }

    // verifies that a missing update handler is reported by name instead of as an obscure container error
    [Theory]
    [InlineData("Polling")]
    [InlineData("Webhook")]
    public void Build_WhenUpdateHandlerIsNotRegistered_ShouldThrowBotStartupException(string mode)
    {
        var builder = CreateBuilder(mode);

        var act = () => builder.Build();

        act.Should().Throw<BotStartupException>()
            .WithMessage("*No IUpdateHandler is registered*AddSingleton<IUpdateHandler, YourUpdateHandler>()*");
    }

    // verifies that the missing handler is reported even when the container validates itself on build (Development)
    [Fact]
    public void Build_WhenUpdateHandlerIsNotRegisteredInDevelopment_ShouldStillThrowBotStartupException()
    {
        var builder = BotApplication.CreateBuilder(
        [
            "--environment=Development",
            "--TelegramBot:Token=123456:ABC"
        ]);

        var act = () => builder.Build();

        act.Should().Throw<BotStartupException>().WithMessage("*No IUpdateHandler is registered*");
    }

    // verifies that a registered update handler lets the application be built
    [Theory]
    [InlineData("Polling")]
    [InlineData("Webhook")]
    public void Build_WhenUpdateHandlerIsRegistered_ShouldReturnApplication(string mode)
    {
        var builder = CreateBuilder(mode);
        builder.Services.AddSingleton(Substitute.For<IUpdateHandler>());

        var app = builder.Build();

        app.Should().NotBeNull();
    }

    // verifies that a handler whose dependencies cannot be resolved is reported once the container exists
    [Fact]
    public void Build_WhenUpdateHandlerDependencyIsMissing_ShouldThrowBotStartupException()
    {
        var builder = CreateBuilder("Polling");
        builder.Services.AddSingleton<IUpdateHandler, HandlerWithMissingDependency>();

        var act = () => builder.Build();

        act.Should().Throw<BotStartupException>()
            .WithMessage("*IUpdateHandler is registered, but it cannot be created*IMissingDependency*");
    }

    // verifies that an unusable parameter model fails the build and names the handler and the property
    [Fact]
    public void Build_WhenCommandParameterModelIsInvalid_ShouldReportHandlerAndProperty()
    {
        var builder = CreateBuilder("Polling");
        builder.Services.AddSingleton(Substitute.For<IUpdateHandler>());
        builder.Services.AddCommands(b => b.AddHandler<BadModelHandler>(), typeof(IUpdateHandler).Assembly);

        var act = () => builder.Build();

        act.Should().Throw<BotStartupException>()
            .WithMessage("*Command handler 'BadModelHandler'*BadModelParams.Value*IValueConverter*");
    }

    // verifies that all setup problems are reported together, so they can be fixed in one go
    [Fact]
    public void Build_WhenSeveralProblemsExist_ShouldReportAllOfThem()
    {
        var builder = CreateBuilder("Polling");
        builder.Services.AddSingleton<IUpdateHandler, HandlerWithMissingDependency>();
        builder.Services.AddCommands(b => b.AddHandler<BadModelHandler>(), typeof(IUpdateHandler).Assembly);

        var act = () => builder.Build();

        act.Should().Throw<BotStartupException>()
            .WithMessage("*2 problem(s)*cannot be created*IMissingDependency*Command handler 'BadModelHandler'*");
    }
}
