using System.Reflection;

namespace Telegram.BotKit.Shared.Registration;

/// <summary>
/// Finds handler types in assemblies.
/// </summary>
internal static class AssemblyScanner
{
    /// <summary>
    /// Finds concrete (non-abstract, non-generic) types matching the predicate. The result is ordered by full type name,
    /// so registration order never depends on the metadata order of an assembly.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <param name="isMatch">The predicate a type has to satisfy.</param>
    /// <returns>The matching types ordered by full type name.</returns>
    public static Type[] FindTypes(IEnumerable<Assembly> assemblies, Func<Type, bool> isMatch) =>
        assemblies
            .SelectMany(GetLoadableTypes)
            .Where(t => t is { IsAbstract: false, IsInterface: false, IsGenericTypeDefinition: false } && isMatch(t))
            .Distinct()
            .OrderBy(t => t.FullName, StringComparer.Ordinal)
            .ToArray();

    /// <summary>
    /// Determines whether the type implements a closed form of the open generic interface.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <param name="openGenericInterface">The open generic interface definition.</param>
    /// <returns>True if the type implements the interface with some type arguments; otherwise, false.</returns>
    public static bool Implements(Type type, Type openGenericInterface) =>
        type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == openGenericInterface);

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.OfType<Type>();
        }
    }
}
