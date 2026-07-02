using Microsoft.EntityFrameworkCore;
using SistemaVentas.Configuration;
using SistemaVentas.Helpers;
using SistemaVentas.Models;
using SistemaVentas.Result;

namespace SistemaVentas.Services;

public sealed class CustomerService : IEtlService
{
    private readonly SalesAnalyticsDBContext _context;

    public CustomerService(SalesAnalyticsDBContext context) => _context = context;

    public async Task<OperationResult> LoadAsync(LookupContext lookup)
    {
        var result = new OperationResult();

        try
        {
            var allRows = CsvParser.ReadFile(AppSettings.CustomersFile, 7).ToList();
            result.Processed = allRows.Count;

            var existingIds = await _context.Customers
                .Select(static c => c.CustomerId)
                .ToHashSetAsync();

            var validEntities = allRows
                .Select(static f => new 
                {
                    Parsed = int.TryParse(f[0], out int id),
                    Id = id,
                    FirstName = f[1].Trim(),
                    LastName = f[2].Trim(),
                    Email = f[3].Trim(),
                    Phone = f[4].Trim(),
                    City = f[5].Trim(),
                    Country = f[6].Trim()
                })
                .Where(x => x.Parsed && !existingIds.Contains(x.Id))
                .Where(x => lookup.CountryMap.ContainsKey(x.Country))
                .Select(x => new
                {
                    x.Id,
                    x.FirstName,
                    x.LastName,
                    x.Email,
                    x.Phone,
                    x.City,
                    CountryId = lookup.CountryMap[x.Country]
                })
                .Where(x => lookup.CityMap.ContainsKey((x.City, x.CountryId)))
                .Select(x => new Customer
                {
                    CustomerId = x.Id,
                    FirstName = x.FirstName,
                    LastName = x.LastName,
                    Email = x.Email,
                    Phone = x.Phone,
                    CityId = lookup.CityMap[(x.City, x.CountryId)]
                })
                .ToList();

            if (validEntities.Count > 0)
            {
                _context.Customers.AddRange(validEntities);
                await _context.SaveChangesAsync();
            }

            result.Inserted = validEntities.Count;
            result.Rejected = result.Processed - result.Inserted - existingIds.Count;

            lookup.CustomerIds = await _context.Customers
                .Select(static c => c.CustomerId)
                .ToHashSetAsync();
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = ex.Message;
        }

        return result;
    }
}
