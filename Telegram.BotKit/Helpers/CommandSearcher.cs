namespace Telegram.BotKit.Helpers;

internal sealed class CommandSearcher(IEnumerable<string> commands)
{
    private readonly (string Command, string Normalized, string Sorted)[] _index = commands.Select(cmd =>
    {
        var norm = Normalize(cmd);
        return (cmd, norm, SortChars(norm));
    }).ToArray();

    public List<string> FindSimilar(string input)
    {
        var inputNorm = Normalize(input);
        if (inputNorm.Length == 0) return new List<string>();

        var inputSorted = SortChars(inputNorm);

        // aдаптивный порог ошибок
        // до 4 букв (ping) - макс 1 ошибка
        // от 4 букв (weather) - макс 2 ошибки
        int maxDist = inputNorm.Length <= 4 ? 1 : 2;

        var candidates = new List<(string Cmd, int Score)>();

        foreach (var item in _index)
        {
            int currentBestDist = int.MaxValue;

            // проверяем, только если длины сопоставимы (png -> ping)
            if (Math.Abs(inputNorm.Length - item.Normalized.Length) <= maxDist)
            {
                int dist = DamerauLevenshtein(inputNorm.AsSpan(), item.Normalized.AsSpan(), maxDist);
                currentBestDist = Math.Min(currentBestDist, dist);
            }

            // если команда длиннее ввода, проверяем её начало (wether -> weather_minsk)
            if (item.Normalized.Length > inputNorm.Length)
            {
                // кусок команды равный длине ввода + 1 (на случай пропущенной буквы)
                int lengthToTake = Math.Min(item.Normalized.Length, inputNorm.Length + 1);
                var prefix = item.Normalized.AsSpan().Slice(0, lengthToTake);

                int dist = DamerauLevenshtein(inputNorm.AsSpan(), prefix, maxDist);
                currentBestDist = Math.Min(currentBestDist, dist);
            }

            // если обычные методы не дали хорошего результата (check_bday -> bday_check)
            if (currentBestDist > maxDist)
            {
                // для sorted логики префиксы не работают, нужны сопоставимые длины
                if (Math.Abs(inputNorm.Length - item.Normalized.Length) <= maxDist)
                {
                    int dist = DamerauLevenshtein(inputSorted.AsSpan(), item.Sorted.AsSpan(), maxDist);
                    currentBestDist = Math.Min(currentBestDist, dist);
                }
            }

            if (currentBestDist <= maxDist)
            {
                candidates.Add((item.Command, currentBestDist));
            }
        }

        return candidates
            .OrderBy(x => x.Score)
            .ThenBy(x => Math.Abs(x.Cmd.Length - input.Length)) // при прочих равных берем более близкую по длине
            .Take(3)
            .Select(x => x.Cmd)
            .ToList();
    }

    private static string Normalize(string s)
    {
        // оставляем только буквы и цифры (weather_minsk -> weatherminsk)
        return new string(s.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
    }

    private static string SortChars(string s)
    {
        var chars = s.ToCharArray();
        Array.Sort(chars);
        return new string(chars);
    }

    private static int DamerauLevenshtein(ReadOnlySpan<char> source, ReadOnlySpan<char> target, int threshold)
    {
        // если разница длин уже больше порога — сразу отказ
        if (Math.Abs(source.Length - target.Length) > threshold) return threshold + 1;

        int length1 = source.Length;
        int length2 = target.Length;

        Span<int> d = stackalloc int[(length1 + 1) * (length2 + 1)];
        int width = length2 + 1;

        for (int i = 0; i <= length1; i++) d[i * width] = i;
        for (int j = 0; j <= length2; j++) d[j] = j;

        for (int i = 1; i <= length1; i++)
        {
            int minRowDist = int.MaxValue;
            for (int j = 1; j <= length2; j++)
            {
                int cost = source[i - 1] == target[j - 1] ? 0 : 1;

                int deletion = d[(i - 1) * width + j] + 1;
                int insertion = d[i * width + (j - 1)] + 1;
                int substitution = d[(i - 1) * width + (j - 1)] + cost;

                int current = Math.Min(deletion, Math.Min(insertion, substitution));

                // транспозиция (pnig -> ping)
                if (i > 1 && j > 1 && source[i - 1] == target[j - 2] && source[i - 2] == target[j - 1])
                {
                    int trans = d[(i - 2) * width + (j - 2)] + cost;
                    current = Math.Min(current, trans);
                }

                d[i * width + j] = current;
                if (current < minRowDist) minRowDist = current;
            }

            // если в строке матрицы все значения уже больше порога, выходим
            if (minRowDist > threshold) return threshold + 1;
        }

        return d[length1 * width + length2];
    }
}
