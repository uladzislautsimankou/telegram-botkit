using System.Collections.Concurrent;
using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telegram.Bot;

namespace Telegram.BotKit.Tests.Hosting;

public sealed class TelegramHttpClientTests
{
    private const string Token = "123456:SECRET-TOKEN";

    private const string GetMeResponse =
        """{"ok":true,"result":{"id":1,"is_bot":true,"first_name":"Bot","username":"test_bot"}}""";

    private sealed class StubHandler : HttpMessageHandler
    {
        public List<Uri?> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request.RequestUri);

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(GetMeResponse, Encoding.UTF8, "application/json")
            });
        }
    }

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        public ConcurrentQueue<string> Messages { get; } = new();

        public ILogger CreateLogger(string categoryName) => new CapturingLogger(Messages);

        public void Dispose()
        {
        }

        private sealed class CapturingLogger(ConcurrentQueue<string> messages) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter) => messages.Enqueue(formatter(state, exception));
        }
    }

    private static ServiceCollection CreateServices(CapturingLoggerProvider? loggerProvider = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["TelegramBot:Token"] = Token })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Trace);

            if (loggerProvider is not null)
            {
                builder.AddProvider(loggerProvider);
            }
        });
        services.AddTelegramBotFramework(configuration);

        return services;
    }

    // verifies that the token, which is a part of every Bot API address, is never written to the logs
    [Fact]
    public async Task TelegramClient_WhenRequestIsSent_ShouldNotWriteTokenToLogs()
    {
        var loggerProvider = new CapturingLoggerProvider();
        var handler = new StubHandler();

        var services = CreateServices(loggerProvider);
        services.AddHttpClient("tg_client").ConfigurePrimaryHttpMessageHandler(() => handler);

        await using var provider = services.BuildServiceProvider();
        var bot = provider.GetRequiredService<ITelegramBotClient>();

        var me = await bot.GetMe(TestContext.Current.CancellationToken);

        // the request did go through the client, and its address contains the token
        me.Username.Should().Be("test_bot");
        handler.Requests.Should().ContainSingle().Which!.ToString().Should().Contain(Token);

        loggerProvider.Messages.Should().NotContain(message => message.Contains(Token));
    }
}
