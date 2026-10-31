using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Telegram.BotKit.Hosting;
using Telegram.BotKit.Shared.Converters;
using Telegram.BotKit.Shared.Startup;

namespace Telegram.BotKit.Tests.Hosting;

public sealed class ServicesRegistrationTests
{
    private static ServiceProvider BuildProvider(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddTelegramBotFramework(configuration);

        return services.BuildServiceProvider();
    }

    // verifies that valid configuration is bound with defaults for omitted values
    [Fact]
    public void AddTelegramBotFramework_WhenConfigurationIsValid_ShouldBindOptions()
    {
        using var provider = BuildProvider(new() { ["TelegramBot:Token"] = "123456:ABC" });

        var options = provider.GetRequiredService<IOptions<TelegramBotOptions>>().Value;

        options.Token.Should().Be("123456:ABC");
        options.Mode.Should().Be(BotMode.Polling);
        options.DeleteWebhookOnShutdown.Should().BeFalse();
    }

    // verifies that the mode is bound case-insensitively
    [Fact]
    public void AddTelegramBotFramework_WhenModeIsLowerCase_ShouldBindWebhookMode()
    {
        using var provider = BuildProvider(new()
        {
            ["TelegramBot:Token"] = "123456:ABC",
            ["TelegramBot:Mode"] = "webhook",
            ["TelegramBot:WebhookUrl"] = "https://example.com/bot"
        });

        provider.GetRequiredService<IOptions<TelegramBotOptions>>().Value.Mode.Should().Be(BotMode.Webhook);
    }

    // verifies that the validator is wired up: invalid configuration fails when options are first resolved
    [Fact]
    public void AddTelegramBotFramework_WhenTokenIsEmpty_ShouldFailValidation()
    {
        using var provider = BuildProvider(new() { ["TelegramBot:Token"] = "" });

        var act = () => provider.GetRequiredService<IOptions<TelegramBotOptions>>().Value;

        act.Should().Throw<OptionsValidationException>().WithMessage("*TelegramBot:Token*");
    }

    // verifies that misspelled update types in configuration are reported
    [Fact]
    public void AddTelegramBotFramework_WhenAllowedUpdatesContainTypo_ShouldFailValidation()
    {
        using var provider = BuildProvider(new()
        {
            ["TelegramBot:Token"] = "123456:ABC",
            ["TelegramBot:AllowedUpdates:0"] = "Mesage"
        });

        var act = () => provider.GetRequiredService<IOptions<TelegramBotOptions>>().Value;

        act.Should().Throw<OptionsValidationException>().WithMessage("*Mesage*");
    }

    // verifies that the concrete and interface registrations share one bot info instance
    [Fact]
    public void AddTelegramBotFramework_ShouldExposeSingleBotInfoInstanceThroughBothServices()
    {
        using var provider = BuildProvider(new() { ["TelegramBot:Token"] = "123456:ABC" });

        provider.GetRequiredService<IBotInfo>().Should().BeSameAs(provider.GetRequiredService<BotInfo>());
    }

    // verifies that the stateless default converter is registered as a singleton
    [Fact]
    public void AddTelegramBotFramework_ShouldRegisterDefaultValueConverterAsSingleton()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddTelegramBotFramework(new ConfigurationBuilder().Build());

        services
            .Single(d => d.ServiceType == typeof(IValueConverter) && d.ImplementationType == typeof(DefaultValueConverter))
            .Lifetime.Should().Be(ServiceLifetime.Singleton);
    }

    // verifies that the update handler check is registered once, however often the framework is added
    [Fact]
    public void AddTelegramBotFramework_WhenCalledTwice_ShouldRegisterUpdateHandlerCheckOnce()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = new ConfigurationBuilder().Build();

        services.AddTelegramBotFramework(configuration);
        services.AddTelegramBotFramework(configuration);

        services
            .Count(d => d.ServiceType == typeof(IStartupCheck) && d.ImplementationType == typeof(UpdateHandlerStartupCheck))
            .Should().Be(1);
    }
}
