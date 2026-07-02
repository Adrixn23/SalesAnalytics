using Microsoft.EntityFrameworkCore;
using SistemaVentas.Configuration;
using SistemaVentas.Interfaces;
using SistemaVentas.Models;
using SistemaVentas.Result;

namespace SistemaVentas.Services;

public sealed class OrderService : IEtlService
{
    private readonly SalesAnalyticsDBContext _context;

    public OrderService(SalesAnalyticsDBContext context) => _context = context;

    public async Task<OperationResult> LoadAsync(LookupContext lookup)
    {
        var result = new OperationResult();

        try
        {
            using var reader = new System.IO.StreamReader(AppSettings.OrdersFile);
            using var csv = new CsvHelper.CsvReader(reader, System.Globalization.CultureInfo.InvariantCulture);
            var allRows = csv.GetRecords<Models.Csv.OrderRow>().ToList();
            
            result.Processed = allRows.Count;

            var existingIds = await _context.Orders
                .Select(static o => o.OrderId)
                .ToHashSetAsync();

            var validEntities = allRows
                .Select(static f => new 
                {
                    ParsedId = int.TryParse(f.OrderId, out int id),
                    Id = id,
                    ParsedCustomer = int.TryParse(f.CustomerId, out int cid),
                    CustomerId = cid,
                    ParsedDate = DateOnly.TryParse(f.OrderDate, out DateOnly d),
                    Date = d,
                    StatusName = f.Status?.Trim()
                })
                .Where(x => x.ParsedId && !existingIds.Contains(x.Id))
                .Where(x => x.ParsedCustomer && lookup.CustomerIds.Contains(x.CustomerId))
                .Where(x => x.StatusName != null && lookup.StatusMap.ContainsKey(x.StatusName))
                .Select(x => new Order
                {
                    OrderId = x.Id,
                    CustomerId = x.CustomerId,
                    OrderDate = x.ParsedDate ? x.Date : DateOnly.MinValue,
                    StatusId = lookup.StatusMap[x.StatusName]
                })
                .ToList();

            if (validEntities.Count > 0)
            {
                await _context.Database.OpenConnectionAsync();
                try
                {
                    await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Sales.Orders ON");
                    _context.Orders.AddRange(validEntities);
                    await _context.SaveChangesAsync();
                    await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Sales.Orders OFF");
                }
                finally
                {
                    await _context.Database.CloseConnectionAsync();
                }
            }

            result.Inserted = validEntities.Count;
            result.Rejected = result.Processed - result.Inserted - existingIds.Count;

            lookup.OrderIds = await _context.Orders
                .Select(static o => o.OrderId)
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
