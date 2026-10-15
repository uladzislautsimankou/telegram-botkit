namespace Telegram.BotKit.Commands;

/// <summary>
/// Provides fuzzy searching and spelling suggestions for bot commands using Damerau-Levenshtein distance.
/// Searches input up to 64 characters with adaptive edit distance thresholds.
/// </summary>
internal sealed class CommandSearcher(IEnumerable<string> commands)
{
    private const int MaxCommandLength = 64;
    private const int StackallocCharThreshold = 256;
    private const int StackallocIntThreshold = 512;

    private readonly (string Command, string Normalized, string Sorted)[] _index = commands.Select(cmd =>
    {
        var norm = Normalize(cmd);
        return (cmd, norm, SortChars(norm));
    }).ToArray();

    /// <summary>
    /// Searches for registered commands that are similar to the misspelled input string.
    /// </summary>
    /// <param name="input">The misspelled command entered by the user.</param>
    /// <returns>A list of up to three close command suggestions ordered by similarity.</returns>
    public IReadOnlyList<string> FindSimilar(string input)
    {
        if (string.IsNullOrWhiteSpace(input) || input.Length > MaxCommandLength)
        {
            return [];
        }

        var inputNorm = Normalize(input);

        if (inputNorm.Length == 0)
        {
            return [];
        }

        var inputSorted = SortChars(inputNorm);

        // adaptive error threshold: max 1 error for <= 4 chars, max 2 for longer
        var maxDist = inputNorm.Length <= 4 ? 1 : 2;

        var buffer = new TopMatchesBuffer(input.Length);

        for (var i = 0; i < _index.Length; i++)
        {
            var item = _index[i];
            var currentBestDist = int.MaxValue;

            // check only if lengths are comparable
            if (Math.Abs(inputNorm.Length - item.Normalized.Length) <= maxDist)
            {
                var dist = DamerauLevenshtein(inputNorm.AsSpan(), item.Normalized.AsSpan(), maxDist);
                currentBestDist = Math.Min(currentBestDist, dist);
            }

            // check prefix match if command is longer than input
            if (item.Normalized.Length > inputNorm.Length)
            {
                var lengthToTake = Math.Min(item.Normalized.Length, inputNorm.Length + 1);
                var prefix = item.Normalized.AsSpan().Slice(0, lengthToTake);

                var dist = DamerauLevenshtein(inputNorm.AsSpan(), prefix, maxDist);
                currentBestDist = Math.Min(currentBestDist, dist);
            }

            // fallback to sorted character sequence match
            if (currentBestDist > maxDist && Math.Abs(inputNorm.Length - item.Normalized.Length) <= maxDist)
            {
                var dist = DamerauLevenshtein(inputSorted.AsSpan(), item.Sorted.AsSpan(), maxDist);
                currentBestDist = Math.Min(currentBestDist, dist);
            }

            if (currentBestDist <= maxDist)
            {
                buffer.TryAdd(item.Command, currentBestDist);
            }
        }

        return buffer.ToResult();
    }

    private ref struct TopMatchesBuffer(int targetLength)
    {
        private string? _cmd0;
        private int _score0 = int.MaxValue;

        private string? _cmd1;
        private int _score1 = int.MaxValue;

        private string? _cmd2;
        private int _score2 = int.MaxValue;

        public void TryAdd(string cmd, int score)
        {
            if (IsBetter(cmd, score, _cmd0, _score0))
            {
                _cmd2 = _cmd1; _score2 = _score1;
                _cmd1 = _cmd0; _score1 = _score0;
                _cmd0 = cmd; _score0 = score;
            }
            else if (IsBetter(cmd, score, _cmd1, _score1))
            {
                _cmd2 = _cmd1; _score2 = _score1;
                _cmd1 = cmd; _score1 = score;
            }
            else if (IsBetter(cmd, score, _cmd2, _score2))
            {
                _cmd2 = cmd; _score2 = score;
            }
        }

        private readonly bool IsBetter(string newCmd, int newScore, string? currentCmd, int currentScore)
        {
            if (currentCmd is null)
            {
                return true;
            }

            if (newScore < currentScore)
            {
                return true;
            }

            if (newScore > currentScore)
            {
                return false;
            }

            return Math.Abs(newCmd.Length - targetLength) < Math.Abs(currentCmd.Length - targetLength);
        }

        public readonly List<string> ToResult()
        {
            var list = new List<string>(3);
            if (_cmd0 is not null)
            {
                list.Add(_cmd0);
            }

            if (_cmd1 is not null)
            {
                list.Add(_cmd1);
            }

            if (_cmd2 is not null)
            {
                list.Add(_cmd2);
            }

            return list;
        }
    }

    private static string Normalize(string s)
    {
        if (string.IsNullOrEmpty(s))
        {
            return string.Empty;
        }

        // keep only alphanumeric characters and normalize to lowercase
        var buffer = s.Length <= StackallocCharThreshold
            ? stackalloc char[s.Length]
            : new char[s.Length];

        var index = 0;

        foreach (var c in s)
        {
            if (char.IsLetterOrDigit(c))
            {
                buffer[index++] = char.ToLowerInvariant(c);
            }
        }

        return index == 0
            ? string.Empty
            : new string(buffer[..index]);
    }

    private static string SortChars(string s)
    {
        if (string.IsNullOrEmpty(s))
        {
            return string.Empty;
        }

        var buffer = s.Length <= StackallocCharThreshold
            ? stackalloc char[s.Length]
            : new char[s.Length];

        s.AsSpan().CopyTo(buffer);
        buffer.Sort();

        return new string(buffer);
    }

    private static int DamerauLevenshtein(ReadOnlySpan<char> source, ReadOnlySpan<char> target, int threshold)
    {
        if (Math.Abs(source.Length - target.Length) > threshold)
        {
            return threshold + 1;
        }

        var length1 = source.Length;
        var length2 = target.Length;

        var matrixSize = (length1 + 1) * (length2 + 1);

        Span<int> d = matrixSize <= StackallocIntThreshold
            ? stackalloc int[matrixSize]
            : new int[matrixSize];

        var width = length2 + 1;

        for (var i = 0; i <= length1; i++)
        {
            d[i * width] = i;
        }

        for (var j = 0; j <= length2; j++)
        {
            d[j] = j;
        }

        for (var i = 1; i <= length1; i++)
        {
            var minRowDist = int.MaxValue;
            for (var j = 1; j <= length2; j++)
            {
                var cost = source[i - 1] == target[j - 1] ? 0 : 1;

                var deletion = d[(i - 1) * width + j] + 1;
                var insertion = d[i * width + (j - 1)] + 1;
                var substitution = d[(i - 1) * width + (j - 1)] + cost;

                var current = Math.Min(deletion, Math.Min(insertion, substitution));

                // transposition check
                if (i > 1 && j > 1 && source[i - 1] == target[j - 2] && source[i - 2] == target[j - 1])
                {
                    var trans = d[(i - 2) * width + (j - 2)] + cost;
                    current = Math.Min(current, trans);
                }

                d[i * width + j] = current;
                if (current < minRowDist)
                {
                    minRowDist = current;
                }
            }

            // exit early if all edit distances in the row exceed the threshold
            if (minRowDist > threshold)
            {
                return threshold + 1;
            }
        }

        return d[length1 * width + length2];
    }
}
