namespace SeverityBeacon;

public static class SeverityDefaults
{
    public static Dictionary<string, SeverityOption> CreateSeverityOptions()
    {
        return new Dictionary<string, SeverityOption>(StringComparer.OrdinalIgnoreCase)
        {
            { "disaster", new("#FF0101", 5, "#0101FF", 125) },
            { "high", new("#FF0101", 4, "#000000", 125) },
            { "average", new("#FFA501", 3, "#000000", 3000, 500) },
            { "warning", new("#FFFF01", 2, "#000000", 3000, 500) }
        };
    }
}
