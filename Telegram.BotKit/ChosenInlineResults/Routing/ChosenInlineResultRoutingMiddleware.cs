using Telegram.Bot.Types;
using Telegram.BotKit.ChosenInlineResults.Abstractions;
using Telegram.BotKit.ChosenInlineResults.Models;
using Telegram.BotKit.Shared.Pipeline;

namespace Telegram.BotKit.ChosenInlineResults.Routing;

/// <summary>
/// Routing middleware responsible for identifying the appropriate chosen inline result handler.
/// </summary>
/// <param name="registry">The registry of chosen inline result handlers.</param>
/// <param name="serviceProvider">The service provider used to create the matched handler.</param>
internal sealed class ChosenInlineResultRoutingMiddleware(
    ChosenInlineResultHandlerRegistry registry,
    IServiceProvider serviceProvider) : IChosenInlineResultMiddleware
{
    /// <inheritdoc />
    public ValueTask InvokeAsync(ChosenInlineResultContext context, NextDelegate next, CancellationToken cancellationToken = default)
    {
        if (TryFindMatch(registry.GetHandlers(), context.Result, out var descriptor))
        {
            context.MatchedChosenInlineResultMetadata = CreateMetadata(descriptor);
        }

        return next();
    }

    // the handler is created lazily by the execution middleware
    private ChosenInlineResultMetadata CreateMetadata(ChosenInlineResultHandlerDescriptor descriptor) =>
        new(descriptor.HandlerType, () => descriptor.Factory(serviceProvider));

    private static bool TryFindMatch(
        ReadOnlySpan<ChosenInlineResultHandlerDescriptor> handlers,
        ChosenInlineResult result,
        out ChosenInlineResultHandlerDescriptor match)
    {
        for (var i = 0; i < handlers.Length; i++)
        {
            if (handlers[i].CanHandle(result))
            {
                match = handlers[i];
                return true;
            }
        }

        match = default;
        return false;
    }
}
