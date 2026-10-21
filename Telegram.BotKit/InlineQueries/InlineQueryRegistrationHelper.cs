using Microsoft.Extensions.DependencyInjection.Extensions;
using Telegram.BotKit.InlineQueries.Abstractions;
using Telegram.BotKit.InlineQueries.Routing;
using Telegram.BotKit.Shared.Registration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registers inline query handlers in the registry and the dependency injection container.
/// </summary>
internal static class InlineQueryRegistrationHelper
{
    /// <summary>
    /// Determines whether the type implements <see cref="IInlineQueryHandler"/>.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type implements <see cref="IInlineQueryHandler"/>; otherwise, false.</returns>
    public static bool IsHandler(Type type) => typeof(IInlineQueryHandler).IsAssignableFrom(type);

    /// <summary>
    /// Registers a handler known only by its runtime type by continuing in the generic <see cref="RegisterHandler{THandler}"/>.
    /// </summary>
    /// <param name="services">The service collection the handler is registered in.</param>
    /// <param name="registry">The registry that stores the handler descriptors.</param>
    /// <param name="handlerType">The runtime type of the handler.</param>
    public static void RegisterHandlerByType(IServiceCollection services, InlineQueryHandlerRegistry registry, Type handlerType) =>
        GenericMethodInvoker.Invoke(
            typeof(InlineQueryRegistrationHelper),
            nameof(RegisterHandler),
            [handlerType],
            services,
            registry);

    /// <summary>
    /// Registers a handler: reads its static matching predicate and order and stores them in a descriptor.
    /// </summary>
    /// <typeparam name="THandler">The handler type.</typeparam>
    /// <param name="services">The service collection the handler is registered in.</param>
    /// <param name="registry">The registry that stores the handler descriptors.</param>
    public static void RegisterHandler<THandler>(IServiceCollection services, InlineQueryHandlerRegistry registry)
        where THandler : class, IInlineQueryHandler
    {
        services.TryAddTransient<THandler>();

        registry.Register(new InlineQueryHandlerDescriptor(
            typeof(THandler),
            THandler.CanHandle,
            static sp => sp.GetRequiredService<THandler>(),
            THandler.Order));
    }
}

