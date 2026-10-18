using System.Reflection;

namespace Telegram.BotKit.Shared.Registration;

/// <summary>
/// Calls a generic static method for types known only at runtime (assembly scanning).
/// This is the only reflection step of handler registration: everything after it is compiled generic code.
/// </summary>
internal static class GenericMethodInvoker
{
    /// <summary>
    /// Calls a static generic method with the given type arguments.
    /// </summary>
    /// <param name="owner">The type that declares the method.</param>
    /// <param name="methodName">The name of the method.</param>
    /// <param name="typeArguments">The type arguments to close the method with.</param>
    /// <param name="arguments">The arguments passed to the method.</param>
    /// <exception cref="InvalidOperationException">Thrown when the method does not exist.</exception>
    public static void Invoke(Type owner, string methodName, Type[] typeArguments, params object[] arguments)
    {
        var method = owner.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Static method '{owner.Name}.{methodName}' was not found.");

        // DoNotWrapExceptions: validation errors surface as-is instead of TargetInvocationException
        method.MakeGenericMethod(typeArguments).Invoke(null, BindingFlags.DoNotWrapExceptions, null, arguments, null);
    }
}
