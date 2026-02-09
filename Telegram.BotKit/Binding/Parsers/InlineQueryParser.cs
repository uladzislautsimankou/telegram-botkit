using Telegram.BotKit.Helpers;

namespace Telegram.BotKit.Binding.Parsers;

internal static class InlineQueryParser
{
    public static (string Key, List<string> Parameters) Parse(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return (string.Empty, new());

        var trimmed = input.Trim();
        var firstSpaceIndex = trimmed.IndexOf(' ');

        string key;
        string rawArgs;

        // если пробелов нет, то у нас просто комманда без аргументов
        if (firstSpaceIndex == -1)
        {
            key = trimmed;
            rawArgs = string.Empty;
        }
        else
        {
            key = input[0..firstSpaceIndex]; // вызераем ключ
            rawArgs = input[(firstSpaceIndex + 1)..]; // аругменты
        }

        var parameters = ArgumentParser.ParseArgs(rawArgs);

        return (key, parameters);
    }
}
