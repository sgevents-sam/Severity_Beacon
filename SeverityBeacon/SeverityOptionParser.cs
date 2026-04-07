namespace SeverityBeacon;

public static class SeverityOptionParser
{
    private static readonly Dictionary<string, int> SeverityMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["disaster"] = 5,
        ["high"] = 4,
        ["average"] = 3,
        ["warning"] = 2,
        ["information"] = 1,
        ["not classified"] = 0
    };

    public static Dictionary<string, SeverityOption> Parse(IEnumerable<string> severityOptions)
    {
        var options = new Dictionary<string, SeverityOption>(StringComparer.OrdinalIgnoreCase);

        foreach (var rawOption in severityOptions)
        {
            var splitOption = rawOption
                .Replace("[", string.Empty)
                .Replace("]", string.Empty)
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            if (splitOption.Length < 2)
            {
                throw new ArgumentException($"Invalid severity option: {rawOption}");
            }

            var name = splitOption[0];
            if (!SeverityMap.TryGetValue(name, out var severityValue))
            {
                throw new ArgumentException($"Unknown severity value: {name}");
            }

            var state1 = splitOption[1];
            var option = splitOption.Length switch
            {
                2 => new SeverityOption(state1, severityValue),
                4 => new SeverityOption(state1, severityValue, splitOption[2], int.Parse(splitOption[3])),
                5 => new SeverityOption(state1, severityValue, splitOption[2], int.Parse(splitOption[3]), int.Parse(splitOption[4])),
                _ => throw new ArgumentException($"Invalid severity option: {rawOption}")
            };

            options[name] = option;
        }

        return options;
    }
}
