using Microsoft.EntityFrameworkCore;
using SistemaVentas.Configuration;
using SistemaVentas.Interfaces;
using SistemaVentas.Models;
using SistemaVentas.Result;

namespace SistemaVentas.Services;

public sealed class CityService : IEtlService
{
    private readonly SalesAnalyticsDBContext _context;

    public CityService(SalesAnalyticsDBContext context) => _context = context;

    public async Task<OperationResult> LoadAsync(LookupContext lookup)
    {
        var result = new OperationResult();

        try
        {
            using var reader = new System.IO.StreamReader(AppSettings.CustomersFile);
            using var csv = new CsvHelper.CsvReader(reader, System.Globalization.CultureInfo.InvariantCulture);
            var allRows = csv.GetRecords<Models.Csv.CustomerRow>().ToList();

            var validPairs = allRows
                .Select(static f => new 
                { 
                    CityName = f.City?.Trim(), 
                    CountryName = f.Country?.Trim() 
                })
                .Where(static x => !string.IsNullOrWhiteSpace(x.CityName) && !string.IsNullOrWhiteSpace(x.CountryName))
                .ToHashSet();

            result.Processed = validPairs.Count;

            var existingKeys = (await _context.Cities
                .Select(static c => new { c.CityName, c.CountryId })
                .ToListAsync())
                .Select(static c => (c.CityName, c.CountryId))
                .ToHashSet();

            var newEntities = validPairs
                .Where(x => lookup.CountryMap.ContainsKey(x.CountryName))
                .Select(x => new City 
                { 
                    CityName = x.CityName,
                    CountryId = lookup.CountryMap[x.CountryName]
                })
                .Where(c => !existingKeys.Contains((c.CityName, c.CountryId)))
                .ToList();

            if (newEntities.Count > 0)
            {
                _context.Cities.AddRange(newEntities);
                await _context.SaveChangesAsync();
            }

            result.Inserted = newEntities.Count;
            result.Rejected = validPairs.Count - newEntities.Count - existingKeys.Count;

            var allCities = await _context.Cities
                .Select(static c => new { c.CityName, c.CountryId, c.CityId })
                .ToListAsync();

            lookup.CityMap = allCities
                .ToDictionary(
                    static c => (c.CityName, c.CountryId),
                    static c => c.CityId
                );
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
        }

        return result;
    }
}
