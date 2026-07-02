using Microsoft.EntityFrameworkCore;
using SistemaVentas.Configuration;
using SistemaVentas.Helpers;
using SistemaVentas.Models;
using SistemaVentas.Result;

namespace SistemaVentas.Services;

internal sealed class CityService : IEtlService
{
    private readonly SalesAnalyticsDBContext _context;

    internal CityService(SalesAnalyticsDBContext context) => _context = context;

    public async Task<OperationResult> LoadAsync(LookupContext lookup)
    {
        var result     = new OperationResult();
        var countryMap = lookup.CountryMap;

        try
        {
            var uniquePairs = CsvParser.ReadFile(AppSettings.CustomersFile, 7)
                .Select(static f => (City: f[5].Trim(), Country: f[6].Trim()))
                .Where(static x => !string.IsNullOrWhiteSpace(x.City) && !string.IsNullOrWhiteSpace(x.Country))
                .DistinctBy(static x => (x.City.ToUpperInvariant(), x.Country.ToUpperInvariant()))
                .ToList();

            result.Processed = uniquePairs.Count;
            result.Rejected  = uniquePairs.Count(x => !countryMap.ContainsKey(x.Country));

            var existingKeys = (await _context.Cities
                .Select(static c => new { c.CityName, c.CountryId })
                .ToListAsync())
                .Select(static c => (c.CityName, c.CountryId))
                .ToHashSet();

            var newEntities = uniquePairs
                .Where(x => countryMap.ContainsKey(x.Country))
                .Select(x => (City: x.City, CountryId: countryMap[x.Country]))
                .Where(x => !existingKeys.Contains((x.City, x.CountryId)))
                .Select(static x => new City { CityName = x.City, CountryId = x.CountryId })
                .ToList();

            if (newEntities.Count > 0)
            {
                _context.Cities.AddRange(newEntities);
                await _context.SaveChangesAsync();
            }

            result.Inserted = newEntities.Count;

            lookup.CityMap = (await _context.Cities
                .Select(static c => new { c.CityName, c.CountryId, c.CityId })
                .ToListAsync())
                .ToDictionary(
                    static c => (c.CityName, c.CountryId),
                    static c => c.CityId);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = ex.Message;
        }

        return result;
    }
}
