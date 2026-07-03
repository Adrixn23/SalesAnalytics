using Microsoft.EntityFrameworkCore;
using SistemaVentas.Configuration;
using SistemaVentas.Interfaces;
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
            using var reader = new System.IO.StreamReader(AppSettings.CustomersFile);
            using var csv = new CsvHelper.CsvReader(reader, System.Globalization.CultureInfo.InvariantCulture);
            var allRows = csv.GetRecords<Models.Csv.CustomerRow>().ToList();
            
            result.Processed = allRows.Count;

            var existingIds = await _context.Customers
                .Select(static c => c.CustomerId)
                .ToHashSetAsync();

            var validEntities = allRows
                .Select(static f => new 
                {
                    Parsed = int.TryParse(f.CustomerId, out int id),
                    Id = id,
                    FirstName = f.FirstName?.Trim(),
                    LastName = f.LastName?.Trim(),
                    Email = f.Email?.Trim(),
                    Phone = f.Phone?.Trim(),
                    City = f.City?.Trim(),
                    Country = f.Country?.Trim()
                })
                .Where(x => x.Parsed && !existingIds.Contains(x.Id))
                .Where(x => x.Country != null && lookup.CountryMap.ContainsKey(x.Country))
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
                .Where(x => x.City != null && lookup.CityMap.ContainsKey((x.City, x.CountryId)))
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
                await _context.Database.OpenConnectionAsync();
                try
                {
                    await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT People.Customers ON");
                    _context.Customers.AddRange(validEntities);
                    await _context.SaveChangesAsync();
                    await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT People.Customers OFF");
                }
                finally
                {
                    await _context.Database.CloseConnectionAsync();
                }
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
            result.Message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
        }

        return result;
    }
}
