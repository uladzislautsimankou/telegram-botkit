using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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

    // stands in for a container other than the default one (Autofac, ...): the update handler is registered
    // inside the container only, so it is never visible in builder.Services
    private sealed class ReplacedContainerFactory : IServiceProviderFactory<IServiceCollection>
    {
        public IServiceCollection CreateBuilder(IServiceCollection services) => services;

        public IServiceProvider CreateServiceProvider(IServiceCollection containerBuilder)
        {
            containerBuilder.AddSingleton(Substitute.For<IUpdateHandler>());
            return containerBuilder.BuildServiceProvider();
        }
    }

    // the environment is passed explicitly: the host validates the container on build in Development only,
    // and the result of a test must not depend on the environment variables of the machine that runs it
    private static BotBuilder CreateBuilder(string mode, string environment = "Production") => BotApplication.CreateBuilder(
    [
        $"--environment={environment}",
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

    // verifies that a missing update handler is reported by name in both modes, not only where the host would notice it
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

    // verifies that a handler registered only inside a replaced container is found: the checks use the built provider,
    // not the service collection
    [Fact]
    public void Build_WhenUpdateHandlerExistsOnlyInReplacedContainer_ShouldReturnApplication()
    {
        var builder = CreateBuilder("Polling");
        builder.HostBuilder!.ConfigureContainer(new ReplacedContainerFactory());

        var app = builder.Build();

        app.Should().NotBeNull();
    }

    // verifies that a handler whose dependencies cannot be resolved is reported in both modes once the container exists
    [Theory]
    [InlineData("Polling")]
    [InlineData("Webhook")]
    public void Build_WhenUpdateHandlerDependencyIsMissing_ShouldThrowBotStartupException(string mode)
    {
        var builder = CreateBuilder(mode);
        builder.Services.AddSingleton<IUpdateHandler, HandlerWithMissingDependency>();

        var act = () => builder.Build();

        act.Should().Throw<BotStartupException>()
            .WithMessage("*IUpdateHandler is registered, but it cannot be created*IMissingDependency*");
    }

    // verifies that invalid bot options fail Build() in both modes (webhook needs them to map the endpoint)
    [Theory]
    [InlineData("Polling")]
    [InlineData("Webhook")]
    public void Build_WhenOptionsAreInvalid_ShouldThrowOptionsValidationException(string mode)
    {
        var builder = BotApplication.CreateBuilder(
        [
            "--environment=Production",
            "--TelegramBot:Token=",
            $"--TelegramBot:Mode={mode}",
            "--TelegramBot:WebhookUrl=https://example.com/api/bot"
        ]);
        builder.Services.AddSingleton(Substitute.For<IUpdateHandler>());

        var act = () => builder.Build();

        act.Should().Throw<OptionsValidationException>().WithMessage("*TelegramBot:Token*");
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

    // verifies that the checks of the library still run when the host validates the container on build (Development)
    [Fact]
    public void Build_WhenCommandParameterModelIsInvalidInDevelopment_ShouldThrowBotStartupException()
    {
        var builder = CreateBuilder("Polling", environment: "Development");
        builder.Services.AddSingleton(Substitute.For<IUpdateHandler>());
        builder.Services.AddCommands(b => b.AddHandler<BadModelHandler>(), typeof(IUpdateHandler).Assembly);

        var act = () => builder.Build();

        act.Should().Throw<BotStartupException>().WithMessage("*Command handler 'BadModelHandler'*");
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

