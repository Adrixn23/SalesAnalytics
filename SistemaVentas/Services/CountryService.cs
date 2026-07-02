using Microsoft.EntityFrameworkCore;
using SistemaVentas.Configuration;
using SistemaVentas.Interfaces;
using SistemaVentas.Models;
using SistemaVentas.Result;

namespace SistemaVentas.Services;

public sealed class CountryService : IEtlService
{
    private readonly SalesAnalyticsDBContext _context;

    public CountryService(SalesAnalyticsDBContext context) => _context = context;

    public async Task<OperationResult> LoadAsync(LookupContext lookup)
    {
        var result = new OperationResult();

        try
        {
            using var reader = new System.IO.StreamReader(AppSettings.CustomersFile);
            using var csv = new CsvHelper.CsvReader(reader, System.Globalization.CultureInfo.InvariantCulture);
            var allRows = csv.GetRecords<Models.Csv.CustomerRow>().ToList();

            var validNames = allRows
                .Select(static f => f.Country?.Trim())
                .Where(static name => !string.IsNullOrWhiteSpace(name))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            result.Processed = validNames.Count;
            result.Rejected  = allRows.Count(static f => string.IsNullOrWhiteSpace(f.Country?.Trim()));

            var existingNames = (await _context.Countries
                .Select(static c => c.CountryName)
                .ToListAsync())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var newEntities = validNames
                .Where(name => !existingNames.Contains(name))
                .Select(static name => new Country { CountryName = name })
                .ToList();

            if (newEntities.Count > 0)
            {
                _context.Countries.AddRange(newEntities);
                await _context.SaveChangesAsync();
            }

            result.Inserted = newEntities.Count;

            lookup.CountryMap = await _context.Countries
                .ToDictionaryAsync(
                    static c => c.CountryName,
                    static c => c.CountryId,
                    StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
        }

        return result;
    }
}
