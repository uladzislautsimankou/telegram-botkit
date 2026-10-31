using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telegram.BotKit.Shared.Exceptions;

namespace Telegram.BotKit.Shared.Startup;

/// <summary>
/// Runs all registered <see cref="IStartupCheck"/>s and reports every problem at once.
/// </summary>
internal static class StartupValidator
{
    /// <summary>
    /// Runs the checks. When any problem is found it is written to the log and a <see cref="BotStartupException"/> is thrown.
    /// </summary>
    /// <param name="services">The root service provider of the built application.</param>
    /// <exception cref="BotStartupException">Thrown when at least one check reported a problem.</exception>
    public static void Run(IServiceProvider services)
    {
        var problems = services
            .GetServices<IStartupCheck>()
            .SelectMany(check => check.Validate(services))
            .ToList();

        ThrowIfAny(problems, services.GetService<ILoggerFactory>());
    }

    /// <summary>
    /// Throws a <see cref="BotStartupException"/> listing the problems, after logging them when a logger is available.
    /// </summary>
    /// <param name="problems">The problems found; nothing happens when empty.</param>
    /// <param name="loggerFactory">The logger factory, or null before the container is built.</param>
    /// <exception cref="BotStartupException">Thrown when there is at least one problem.</exception>
    public static void ThrowIfAny(IReadOnlyList<string> problems, ILoggerFactory? loggerFactory)
    {
        if (problems.Count is 0)
        {
            return;
        }

        var message = Format(problems);

        loggerFactory
            ?.CreateLogger("Telegram.BotKit.Startup")
            .LogCritical("{Message}", message);

        throw new BotStartupException(message);
    }

    private static string Format(IReadOnlyList<string> problems)
    {
        var builder = new StringBuilder()
            .Append("Bot startup validation failed with ")
            .Append(problems.Count)
            .Append(" problem(s):");

        for (var i = 0; i < problems.Count; i++)
        {
            builder.AppendLine().Append("  ").Append(i + 1).Append(". ").Append(problems[i]);
        }

        return builder.ToString();
    }
}
