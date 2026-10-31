namespace Telegram.BotKit.Shared.Exceptions;

/// <summary>
/// Thrown when generated callback data exceeds the limit Telegram accepts.
/// </summary>
/// <param name="rawData">The generated callback data.</param>
/// <param name="byteCount">The size of the data in bytes (UTF-8).</param>
/// <param name="maxBytes">The maximum size Telegram accepts, in bytes.</param>
public sealed class CallbackDataTooLongException(string rawData, int byteCount, int maxBytes)
    : BotKitException(
        $"Callback data is {byteCount} bytes but Telegram allows at most {maxBytes}: '{rawData}'. " +
        "Shorten the key or the parameter names ([CallbackParam(Name = ...)]), or pass less data.")
{
    /// <summary>
    /// Gets the generated callback data.
    /// </summary>
    public string RawData { get; } = rawData;
    /// <summary>
    /// Gets the size of the callback data in bytes (UTF-8).
    /// </summary>
    public int ByteCount { get; } = byteCount;
    /// <summary>
    /// Gets the maximum size of callback data Telegram accepts, in bytes.
    /// </summary>
    public int MaxBytes { get; } = maxBytes;
}

