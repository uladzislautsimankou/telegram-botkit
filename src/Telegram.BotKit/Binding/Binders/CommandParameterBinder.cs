using System.Collections.Concurrent;
using System.Reflection;
using Telegram.BotKit.Abstractions;
using Telegram.BotKit.Attributes;
using Telegram.BotKit.Exceptions;

namespace Telegram.BotKit.Binding.Binders;

internal sealed class CommandParameterBinder(IEnumerable<IValueConverter> converters) : ICommandParameterBinder
{
    // кэш, что бы каждый раз не дергать рефлексию на кадждую комманду
    private static readonly ConcurrentDictionary<Type, List<ParamMetadata>> _cache = new();

    private record ParamMetadata(PropertyInfo PropertyInfo, CommandParamAttribute Attr)
    {
        public string? Name => Attr.Name;
        public int Position => Attr.Position;
        public bool Required => Attr.Required;
        public Type PropertyType => PropertyInfo.PropertyType;
    }

    public TParams Bind<TParams>(CommandContext context) where TParams : class, new()
    {
        var (positionalArgs, namedArgs) = SplitParams(context.RawParams);

        var instance = new TParams();
        var metadataList = _cache.GetOrAdd(typeof(TParams), BuildMetadata);

        // сортируем свойства по позиции, чтобы забирать из очереди в правильном порядке
        foreach (var meta in metadataList.OrderBy(m => m.Position))
        {
            var rawValue = GetRawValue(meta, namedArgs, positionalArgs);

            if (rawValue is null && meta.Required)
                throw new MissingParameterException(meta.Name ?? meta.PropertyInfo.Name);

            try
            {
                var value = ConvertValue(rawValue, meta.PropertyType);
                meta.PropertyInfo.SetValue(instance, value);
            }
            catch (Exception)
            {
                throw new InvalidParameterTypeException(
                    parameterName: meta.Name ?? meta.PropertyInfo.Name,
                    rawValue: rawValue,
                    targetType: meta.PropertyType
                );
            }
        }

        return instance;
    }

    private static string? GetRawValue(ParamMetadata meta, Dictionary<string, string> named, Queue<string> positional)
    {
        var key = meta.Name ?? meta.PropertyInfo.Name;

        // сначала ищем именованный
        if (named.TryGetValue(key, out var namedVal))
            return namedVal;

        // если нет, то просто берем следующий доступный из очереди позиционных
        // берем из очереди только если у параметра реально задана позиция
        if (meta.Position >= 0 && positional.Count > 0)
            return positional.Dequeue();

        return null;
    }

    private static (Queue<string> Positional, Dictionary<string, string> Named) SplitParams(List<string> rawParams)
    {
        var positional = new Queue<string>(rawParams.Count);
        var named = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var param in rawParams)
        {
            var span = param.AsSpan();

            var eqIndex = span.IndexOf('=');

            // если именованный параметр (key=value)
            if (eqIndex > 0)
            {
                var key = span.Slice(0, eqIndex).ToString();
                var val = span.Slice(eqIndex + 1).ToString();

                // если по какой-то причине у нас уже есть значение, то перезаписываем его просто
                named[key] = val;
                continue;
            }

            // если позиционный параметр (value)
            positional.Enqueue(param);
        }

        return (positional, named);
    }

    private static List<ParamMetadata> BuildMetadata(Type type)
    {
        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => (Prop: p, Attr: p.GetCustomAttribute<CommandParamAttribute>()))
            .Where(x => x.Attr is not null)
            .Select(x => new ParamMetadata(x.Prop, x.Attr!))
            .ToList();
    }

    private object? ConvertValue(string? input, Type type)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;

        foreach (var converter in converters.Reverse())
        {
            if (converter.TryConvert(input, type, out var result))
            {
                return result;
            }
        }

        throw new NotSupportedException($"No converter found for type {type.Name}");
    }
}