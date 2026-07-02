using Microsoft.EntityFrameworkCore;
using SistemaVentas.Configuration;
using SistemaVentas.Helpers;
using SistemaVentas.Models;
using SistemaVentas.Result;

namespace SistemaVentas.Services;

internal sealed class CountryService : IEtlService
{
    private readonly SalesAnalyticsDBContext _context;

    internal CountryService(SalesAnalyticsDBContext context) => _context = context;

    public async Task<OperationResult> LoadAsync(LookupContext lookup)
    {
        var result = new OperationResult();

        try
        {
            var allRows = CsvParser.ReadFile(AppSettings.CustomersFile, 7).ToList();

            var validNames = allRows
                .Select(static f => f[6].Trim())
                .Where(static name => !string.IsNullOrWhiteSpace(name))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            result.Processed = validNames.Count;
            result.Rejected  = allRows.Count(static f => string.IsNullOrWhiteSpace(f[6].Trim()));

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

            result.Inserted    = newEntities.Count;
            lookup.CountryMap  = await _context.Countries
                .ToDictionaryAsync(
                    static c => c.CountryName,
                    static c => c.CountryId,
                    StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = ex.Message;
        }

        return result;
    }
}
