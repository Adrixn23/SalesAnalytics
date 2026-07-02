using System.Text.RegularExpressions;

namespace SistemaVentas.Helpers;

internal static class CsvParser
{
    private static readonly Regex FieldRegex = new(
        @"(?:^|,)(""(?:[^""]|"""")*""|[^,]*)",
        RegexOptions.Compiled);

    internal static string[] ParseLine(string line) =>
        FieldRegex.Matches(line)
                  .Select(m =>
                  {
                      var v = m.Groups[1].Value;
                      return (v.StartsWith('"') && v.EndsWith('"'))
                          ? v[1..^1].Replace("\"\"", "\"").Trim()
                          : v.Trim();
                  })
                  .ToArray();

    internal static IEnumerable<string[]> ReadFile(string filePath, int expectedColumns) =>
        File.ReadLines(filePath)
            .Skip(1)
            .Where(static line => !string.IsNullOrWhiteSpace(line))
            .Select(ParseLine)
            .Where(fields => fields.Length >= expectedColumns);
}
