namespace Telegram.BotKit.Commands;

/// <summary>
/// Provides fuzzy searching and spelling suggestion capabilities for bot commands using Damerau-Levenshtein distance.
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
    public List<string> FindSimilar(string input)
    {
        if (string.IsNullOrWhiteSpace(input) || input.Length > MaxCommandLength)
            return new List<string>();

        var inputNorm = Normalize(input);

        if (inputNorm.Length == 0)
            return new List<string>();

        var inputSorted = SortChars(inputNorm);

        // adaptive error threshold
        // up to 4 characters (e.g. ping): maximum 1 error
        // more than 4 characters (e.g. weather): maximum 2 errors
        var maxDist = inputNorm.Length <= 4 ? 1 : 2;

        var candidates = new List<(string Cmd, int Score)>();

        foreach (var item in _index)
        {
            var currentBestDist = int.MaxValue;

            // check only if lengths are comparable (e.g., png -> ping)
            if (Math.Abs(inputNorm.Length - item.Normalized.Length) <= maxDist)
            {
                var dist = DamerauLevenshtein(inputNorm.AsSpan(), item.Normalized.AsSpan(), maxDist);
                currentBestDist = Math.Min(currentBestDist, dist);
            }

            // if the command is longer than the input, check its prefix (e.g., wether -> weather_minsk)
            if (item.Normalized.Length > inputNorm.Length)
            {
                // take a slice of the command equal to input length + 1 to account for a missing character
                var lengthToTake = Math.Min(item.Normalized.Length, inputNorm.Length + 1);
                var prefix = item.Normalized.AsSpan().Slice(0, lengthToTake);

                var dist = DamerauLevenshtein(inputNorm.AsSpan(), prefix, maxDist);
                currentBestDist = Math.Min(currentBestDist, dist);
            }

            // fallback: if standard matches yield no results, check sorted character sequence (e.g., check_bday -> bday_check)
            if (currentBestDist > maxDist)
            {
                // sorted character matching requires comparable lengths as prefix matching does not apply
                if (Math.Abs(inputNorm.Length - item.Normalized.Length) <= maxDist)
                {
                    var dist = DamerauLevenshtein(inputSorted.AsSpan(), item.Sorted.AsSpan(), maxDist);
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
            .ThenBy(x => Math.Abs(x.Cmd.Length - input.Length)) // tie-breaker: prefer candidate with closer length to input
            .Take(3)
            .Select(x => x.Cmd)
            .ToList();
    }

    private static string Normalize(string s)
    {
        if (string.IsNullOrEmpty(s))
            return string.Empty;

        // keep only alphanumeric characters and normalize to lowercase (e.g., weather_minsk -> weatherminsk)
        var buffer = s.Length <= StackallocCharThreshold
            ? stackalloc char[s.Length]
            : new char[s.Length];

        var index = 0;

        foreach (char c in s)
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
            return string.Empty;

        var buffer = s.Length <= StackallocCharThreshold
            ? stackalloc char[s.Length]
            : new char[s.Length];

        s.AsSpan().CopyTo(buffer);
        buffer.Sort();

        return new string(buffer);
    }

    private static int DamerauLevenshtein(ReadOnlySpan<char> source, ReadOnlySpan<char> target, int threshold)
    {
        // return early if length difference exceeds the threshold
        if (Math.Abs(source.Length - target.Length) > threshold)
            return threshold + 1;

        var length1 = source.Length;
        var length2 = target.Length;

        var matrixSize = (length1 + 1) * (length2 + 1);

        Span<int> d = matrixSize <= StackallocIntThreshold
            ? stackalloc int[matrixSize]
            : new int[matrixSize];

        var width = length2 + 1;

        for (int i = 0; i <= length1; i++) d[i * width] = i;
        for (int j = 0; j <= length2; j++) d[j] = j;

        for (int i = 1; i <= length1; i++)
        {
            var minRowDist = int.MaxValue;
            for (int j = 1; j <= length2; j++)
            {
                var cost = source[i - 1] == target[j - 1] ? 0 : 1;

                var deletion = d[(i - 1) * width + j] + 1;
                var insertion = d[i * width + (j - 1)] + 1;
                var substitution = d[(i - 1) * width + (j - 1)] + cost;

                var current = Math.Min(deletion, Math.Min(insertion, substitution));

                // transposition (e.g., pnig -> ping)
                if (i > 1 && j > 1 && source[i - 1] == target[j - 2] && source[i - 2] == target[j - 1])
                {
                    var trans = d[(i - 2) * width + (j - 2)] + cost;
                    current = Math.Min(current, trans);
                }

                d[i * width + j] = current;
                if (current < minRowDist) minRowDist = current;
            }

            // exit early if all edit distances in the row exceed the threshold
            if (minRowDist > threshold)
                return threshold + 1;
        }

        return d[length1 * width + length2];
    }
}