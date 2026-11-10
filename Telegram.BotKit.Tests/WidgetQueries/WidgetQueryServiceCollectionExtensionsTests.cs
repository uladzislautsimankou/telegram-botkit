using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Types;
using Telegram.BotKit.Shared.Converters;
using Telegram.BotKit.Shared.Exceptions;
using Telegram.BotKit.Shared.Pipeline;
using Telegram.BotKit.Shared.Startup;
using Telegram.BotKit.WidgetQueries.Abstractions;
using Telegram.BotKit.WidgetQueries.Attributes;
using Telegram.BotKit.WidgetQueries.Models;

namespace Telegram.BotKit.Tests.WidgetQueries;

/// <summary>
/// End-to-end tests of <c>AddWidgetQueries</c>: the real registry, binder, pipeline and dispatcher built by the container.
/// </summary>
public sealed class WidgetQueryServiceCollectionExtensionsTests
{
    public sealed class Recorder
    {
        public List<(Type Handler, WidgetQueryContext Context, object Params)> Calls { get; } = [];

        // what a middleware saw: whether a handler was already matched when it ran
        public Dictionary<string, bool> MatchedWhenSeen { get; } = [];
    }

    public sealed class VoteParams
    {
        [WidgetParam(Name = "o", Required = true)]
        public int Option { get; set; }
    }

    private sealed class VoteHandler(Recorder recorder) : IWidgetQueryHandler<VoteParams>
    {
        public static string Key => "poll:vote";

        public Task HandleAsync(VoteParams parameters, WidgetQueryContext context, CancellationToken cancellationToken = default)
        {
            recorder.Calls.Add((typeof(VoteHandler), context, parameters));
            return Task.CompletedTask;
        }
    }

    public sealed class UnsupportedValue;

    public sealed class UnsupportedParams
    {
        public UnsupportedValue? Value { get; set; }
    }

    private sealed class UnsupportedHandler : IWidgetQueryHandler<UnsupportedParams>
    {
        public static string Key => "unsupported";

        public Task HandleAsync(UnsupportedParams parameters, WidgetQueryContext context, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private abstract class ProbeMiddleware(Recorder recorder, string name) : IWidgetQueryMiddleware
    {
        public ValueTask InvokeAsync(WidgetQueryContext context, NextDelegate next, CancellationToken cancellationToken = default)
        {
            recorder.MatchedWhenSeen[name] = context.MatchedWidgetQueryMetadata is not null;
            return next();
        }
    }

    private sealed class BeforeRoutingProbe(Recorder recorder) : ProbeMiddleware(recorder, "before");

    private sealed class AfterRoutingProbe(Recorder recorder) : ProbeMiddleware(recorder, "after");

    private static (ServiceProvider Provider, Recorder Recorder) CreateProvider(Action<WidgetQueryPipelineBuilder> configure)
    {
        var recorder = new Recorder();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(recorder);
        services.AddSingleton<IValueConverter, DefaultValueConverter>();

        // scanning is limited to the library assembly, which has no handlers: the test assembly holds handlers
        // with clashing keys for other tests, so the handlers of a test are added explicitly
        services.AddWidgetQueries(configure, typeof(WidgetQueryContext).Assembly);

        return (services.BuildServiceProvider(), recorder);
    }

    // verifies that a query of an original message reaches the handler with bound parameters
    [Fact]
    public async Task Dispatch_WhenOriginalQueryMatchesHandler_ShouldBindParametersAndCallHandler()
    {
        // arrange
        var (provider, recorder) = CreateProvider(b => b.AddHandler<VoteHandler>());
        await using var providerScope = provider;
        var dispatcher = provider.GetRequiredService<IWidgetQueryDispatcher>();

        // act
        await dispatcher.DispatchAsync(WidgetQueryTestData.Original("poll:vote?o=2"), TestContext.Current.CancellationToken);

        // assert
        var call = recorder.Calls.Should().ContainSingle().Which;
        call.Handler.Should().Be(typeof(VoteHandler));
        call.Context.IsCopy.Should().BeFalse();
        call.Params.Should().BeOfType<VoteParams>().Which.Option.Should().Be(2);
    }

    // verifies that a query of a forwarded copy reaches the same handler and tells that it is a copy
    [Fact]
    public async Task Dispatch_WhenSharedQueryMatchesHandler_ShouldCallHandlerWithCopyContext()
    {
        // arrange
        var (provider, recorder) = CreateProvider(b => b.AddHandler<VoteHandler>());
        await using var providerScope = provider;
        var dispatcher = provider.GetRequiredService<IWidgetQueryDispatcher>();

        // act
        await dispatcher.DispatchAsync(WidgetQueryTestData.Copy("poll:vote?o=3", widgetId: "widget_9"), TestContext.Current.CancellationToken);

        // assert
        var call = recorder.Calls.Should().ContainSingle().Which;
        call.Context.IsCopy.Should().BeTrue();
        call.Context.WidgetId.Should().Be("widget_9");
        call.Context.Chat.Should().BeNull();
        call.Params.Should().BeOfType<VoteParams>().Which.Option.Should().Be(3);
    }

    // verifies that a query of any type reaches the handler by its data, and the handler can read the type
    [Fact]
    public async Task Dispatch_WhenOriginalQueryIsSentTransaction_ShouldRouteByDataAndExposeQueryType()
    {
        // arrange
        var type = new WidgetQueryTypeSentTransaction();
        var (provider, recorder) = CreateProvider(b => b.AddHandler<VoteHandler>());
        await using var providerScope = provider;
        var dispatcher = provider.GetRequiredService<IWidgetQueryDispatcher>();

        // act
        await dispatcher.DispatchAsync(WidgetQueryTestData.Original("poll:vote?o=4", type: type), TestContext.Current.CancellationToken);

        // assert
        var call = recorder.Calls.Should().ContainSingle().Which;
        call.Context.QueryType.Should().BeSameAs(type);
        call.Params.Should().BeOfType<VoteParams>().Which.Option.Should().Be(4);
    }

    [Fact]
    public async Task Dispatch_WhenSharedQueryIsSignData_ShouldRouteByDataAndExposeQueryType()
    {
        // arrange
        var type = new WidgetQueryTypeSignData();
        var (provider, recorder) = CreateProvider(b => b.AddHandler<VoteHandler>());
        await using var providerScope = provider;
        var dispatcher = provider.GetRequiredService<IWidgetQueryDispatcher>();

        // act
        await dispatcher.DispatchAsync(WidgetQueryTestData.Copy("poll:vote?o=5", type: type), TestContext.Current.CancellationToken);

        // assert
        var call = recorder.Calls.Should().ContainSingle().Which;
        call.Context.IsCopy.Should().BeTrue();
        call.Context.QueryType.Should().BeSameAs(type);
        call.Params.Should().BeOfType<VoteParams>().Which.Option.Should().Be(5);
    }

    // verifies that a missing required parameter fails the dispatch instead of calling the handler with a half-filled model
    [Fact]
    public async Task Dispatch_WhenRequiredParameterIsMissing_ShouldThrowAndNotCallHandler()
    {
        // arrange
        var (provider, recorder) = CreateProvider(b => b.AddHandler<VoteHandler>());
        await using var providerScope = provider;
        var dispatcher = provider.GetRequiredService<IWidgetQueryDispatcher>();

        // act
        var act = () => dispatcher.DispatchAsync(WidgetQueryTestData.Original("poll:vote"), TestContext.Current.CancellationToken);

        // assert
        await act.Should().ThrowAsync<MissingParameterException>();
        recorder.Calls.Should().BeEmpty();
    }

    // verifies that an unknown key is not an error: no handler is called and the dispatch completes
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Dispatch_WhenKeyIsUnknown_ShouldCompleteWithoutCallingHandler(bool copy)
    {
        // arrange
        var (provider, recorder) = CreateProvider(b => b.AddHandler<VoteHandler>());
        await using var providerScope = provider;
        var dispatcher = provider.GetRequiredService<IWidgetQueryDispatcher>();

        // act
        var act = () => copy
            ? dispatcher.DispatchAsync(WidgetQueryTestData.Copy("old:button"), TestContext.Current.CancellationToken)
            : dispatcher.DispatchAsync(WidgetQueryTestData.Original("old:button"), TestContext.Current.CancellationToken);

        // assert
        await act.Should().NotThrowAsync();
        recorder.Calls.Should().BeEmpty();
    }

    // verifies that middleware added before UseRouting runs before a handler is matched, and the one added after it, after
    [Fact]
    public async Task Dispatch_WhenMiddlewareSurroundsRouting_ShouldSeeMatchOnlyAfterRouting()
    {
        // arrange
        var (provider, recorder) = CreateProvider(b => b
            .UseMiddleware<BeforeRoutingProbe>()
            .UseRouting()
            .UseMiddleware<AfterRoutingProbe>()
            .AddHandler<VoteHandler>());
        await using var providerScope = provider;
        var dispatcher = provider.GetRequiredService<IWidgetQueryDispatcher>();

        // act
        await dispatcher.DispatchAsync(WidgetQueryTestData.Original("poll:vote?o=1"), TestContext.Current.CancellationToken);

        // assert
        recorder.MatchedWhenSeen["before"].Should().BeFalse();
        recorder.MatchedWhenSeen["after"].Should().BeTrue();
        recorder.Calls.Should().ContainSingle();
    }

    // verifies that middleware placed after routing still runs when no handler matched, so it can answer the widget itself
    [Fact]
    public async Task Dispatch_WhenKeyIsUnknown_ShouldStillRunMiddlewareAfterRoutingWithoutMatch()
    {
        // arrange
        var (provider, recorder) = CreateProvider(b => b
            .UseRouting()
            .UseMiddleware<AfterRoutingProbe>()
            .AddHandler<VoteHandler>());
        await using var providerScope = provider;
        var dispatcher = provider.GetRequiredService<IWidgetQueryDispatcher>();

        // act
        await dispatcher.DispatchAsync(WidgetQueryTestData.Original("old:button"), TestContext.Current.CancellationToken);

        // assert
        recorder.MatchedWhenSeen["after"].Should().BeFalse();
        recorder.Calls.Should().BeEmpty();
    }

    // verifies that the dispatcher is a singleton, as the compiled pipeline it runs is
    [Fact]
    public async Task AddWidgetQueries_WhenResolvedTwice_ShouldReturnSameDispatcher()
    {
        var (provider, _) = CreateProvider(b => b.AddHandler<VoteHandler>());
        await using var providerScope = provider;

        provider.GetRequiredService<IWidgetQueryDispatcher>()
            .Should().BeSameAs(provider.GetRequiredService<IWidgetQueryDispatcher>());
    }

    // verifies that valid parameter models pass the startup validation
    [Fact]
    public async Task StartupValidator_WhenParameterModelsAreValid_ShouldNotThrow()
    {
        var (provider, _) = CreateProvider(b => b.AddHandler<VoteHandler>());
        await using var providerScope = provider;

        var act = () => StartupValidator.Run(provider);

        act.Should().NotThrow();
    }

    // verifies that a parameter model nobody can convert is reported at startup and names the handler
    [Fact]
    public async Task StartupValidator_WhenParameterModelHasUnsupportedType_ShouldFailAndNameTheHandler()
    {
        var (provider, _) = CreateProvider(b => b.AddHandler<UnsupportedHandler>());
        await using var providerScope = provider;

        var act = () => StartupValidator.Run(provider);

        act.Should().Throw<BotStartupException>()
            .Which.Message.Should().Contain("Widget query handler 'UnsupportedHandler'").And.Contain("IValueConverter");
    }
}
