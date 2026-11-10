using System.IO.Compression;
using System.Text;

namespace Telegram.BotKit.Example.Widgets;

/// <summary>
/// Reads the HTML of a widget and checks it against the limits of Telegram before the first send:
/// at most 32768 bytes, and at most 4096 bytes after zlib at level 9.
/// </summary>
internal static class WidgetHtml
{
    private const int MaxRawBytes = 32768;
    private const int MaxPackedBytes = 4096;

    public static string Load(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Widgets", fileName);
        var html = File.ReadAllText(path);

        var bytes = Encoding.UTF8.GetBytes(html);
        var packed = PackedSize(bytes);

        // an approximation: the server counts with PHP gzcompress($html, 9) and has the last word
        // (INPUT_MEDIA_WIDGET_TOO_LARGE), but a widget this close to the limit is too big anyway
        if (bytes.Length > MaxRawBytes || packed > MaxPackedBytes)
        {
            throw new InvalidOperationException(
                $"Widget '{fileName}' is too large: {bytes.Length} bytes ({MaxRawBytes} allowed), " +
                $"{packed} bytes packed ({MaxPackedBytes} allowed).");
        }

        return html;
    }

    private static int PackedSize(byte[] bytes)
    {
        using var buffer = new MemoryStream();

        using (var zlib = new ZLibStream(buffer, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            zlib.Write(bytes);
        }

        return (int)buffer.Length;
    }
}
