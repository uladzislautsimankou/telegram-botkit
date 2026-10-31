using FluentAssertions;
using Microsoft.Extensions.Options;
using Telegram.BotKit.Hosting;

namespace Telegram.BotKit.Tests.Hosting;

public sealed class TelegramBotOptionsValidatorTests
{
    private readonly TelegramBotOptionsValidator _validator = new();

    private static TelegramBotOptions ValidOptions() => new() { Token = "123456:ABC" };

    private ValidateOptionsResult Validate(TelegramBotOptions options) => _validator.Validate(null, options);

    private void AssertSingleFailure(TelegramBotOptions options, string expectedFragment)
    {
        var result = Validate(options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle().Which.Should().Contain(expectedFragment);
    }

    // verifies that minimal polling options are valid
    [Fact]
    public void Validate_WhenOnlyTokenIsSet_ShouldSucceed() =>
        Validate(ValidOptions()).Succeeded.Should().BeTrue();

    // verifies that fully specified webhook options are valid
    [Fact]
    public void Validate_WhenWebhookOptionsAreComplete_ShouldSucceed()
    {
        var options = ValidOptions();
        options.Mode = BotMode.Webhook;
        options.WebhookUrl = "https://example.com/api/bot";
        options.SecretToken = "abc_DEF-123";
        options.IpAddress = "203.0.113.5";
        options.MaxConnections = 40;
        options.BaseUrl = "http://localhost:8081";
        options.AllowedUpdates = ["Message", "callbackquery"];

        Validate(options).Succeeded.Should().BeTrue();
    }

    // verifies that an empty token is rejected
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenTokenIsBlank_ShouldFail(string token)
    {
        var options = ValidOptions();
        options.Token = token;

        AssertSingleFailure(options, "TelegramBot:Token");
    }

    // verifies that webhook mode requires an https url
    [Theory]
    [InlineData("", "WebhookUrl is required")]
    [InlineData("http://example.com/bot", "HTTPS")]
    [InlineData("example.com/bot", "HTTPS")]
    public void Validate_WhenWebhookUrlIsInvalid_ShouldFail(string url, string expectedFragment)
    {
        var options = ValidOptions();
        options.Mode = BotMode.Webhook;
        options.WebhookUrl = url;

        AssertSingleFailure(options, expectedFragment);
    }

    // verifies that the webhook url is not validated in polling mode
    [Fact]
    public void Validate_WhenModeIsPollingAndWebhookUrlIsInvalid_ShouldSucceed()
    {
        var options = ValidOptions();
        options.WebhookUrl = "http://not-validated";

        Validate(options).Succeeded.Should().BeTrue();
    }

    // verifies that only absolute http(s) base urls are accepted
    [Theory]
    [InlineData("ftp://host")]
    [InlineData("not a url")]
    [InlineData("/relative")]
    public void Validate_WhenBaseUrlIsInvalid_ShouldFail(string baseUrl)
    {
        var options = ValidOptions();
        options.BaseUrl = baseUrl;

        AssertSingleFailure(options, "BaseUrl");
    }

    // verifies that secret tokens are restricted to the characters Telegram accepts
    [Theory]
    [InlineData("has space")]
    [InlineData("dot.dot")]
    [InlineData("ключ")]
    public void Validate_WhenSecretTokenContainsInvalidCharacters_ShouldFail(string secret)
    {
        var options = ValidOptions();
        options.SecretToken = secret;

        AssertSingleFailure(options, "SecretToken");
    }

    // verifies the secret token length boundary
    [Fact]
    public void Validate_WhenSecretTokenLengthIsAtAndAboveLimit_ShouldSucceedAndFailRespectively()
    {
        var atLimit = ValidOptions();
        atLimit.SecretToken = new string('a', 256);
        Validate(atLimit).Succeeded.Should().BeTrue();

        var aboveLimit = ValidOptions();
        aboveLimit.SecretToken = new string('a', 257);
        AssertSingleFailure(aboveLimit, "SecretToken");
    }

    // verifies that malformed ip addresses are rejected
    [Theory]
    [InlineData("999.1.1.1")]
    [InlineData("not-an-ip")]
    public void Validate_WhenIpAddressIsInvalid_ShouldFail(string ip)
    {
        var options = ValidOptions();
        options.IpAddress = ip;

        AssertSingleFailure(options, "IpAddress");
    }

    // verifies the max connections boundaries
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(100, true)]
    [InlineData(101, false)]
    [InlineData(-5, false)]
    public void Validate_WhenMaxConnectionsIsSet_ShouldRespectBounds(int value, bool valid)
    {
        var options = ValidOptions();
        options.MaxConnections = value;

        Validate(options).Succeeded.Should().Be(valid);
    }

    // verifies that misspelled update types are reported instead of being silently dropped
    [Theory]
    [InlineData("Mesage")]
    [InlineData("Unknown")]
    public void Validate_WhenAllowedUpdatesContainUnknownType_ShouldFailAndNameIt(string value)
    {
        var options = ValidOptions();
        options.AllowedUpdates = ["Message", value];

        AssertSingleFailure(options, value);
    }

    // verifies that an empty update list (reset to defaults) is valid
    [Fact]
    public void Validate_WhenAllowedUpdatesIsEmpty_ShouldSucceed()
    {
        var options = ValidOptions();
        options.AllowedUpdates = [];

        Validate(options).Succeeded.Should().BeTrue();
    }

    // verifies that all problems are reported at once
    [Fact]
    public void Validate_WhenSeveralOptionsAreInvalid_ShouldReportAllFailures()
    {
        var options = new TelegramBotOptions { Token = "", Mode = BotMode.Webhook, MaxConnections = 0 };

        Validate(options).Failures.Should().HaveCount(3);
    }
}
