namespace SistemaVentas.Models;

public sealed class LookupContext
{
    public Dictionary<string, int>        CountryMap  { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<(string, int), int> CityMap     { get; set; } = new();
    public Dictionary<string, int>        CategoryMap { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int>        StatusMap   { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<int>                   CustomerIds { get; set; } = new();
    public HashSet<int>                   ProductIds  { get; set; } = new();
    public HashSet<int>                   OrderIds    { get; set; } = new();
}
