using Microsoft.EntityFrameworkCore;
using SistemaVentas.Configuration;
using SistemaVentas.Helpers;
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
            var allRows = CsvParser.ReadFile(AppSettings.OrdersFile, 4).ToList();
            result.Processed = allRows.Count;

            var existingIds = await _context.Orders
                .Select(static o => o.OrderId)
                .ToHashSetAsync();

            var validEntities = allRows
                .Select(static f => new 
                {
                    ParsedId = int.TryParse(f[0], out int id),
                    Id = id,
                    ParsedCustomer = int.TryParse(f[1], out int cid),
                    CustomerId = cid,
                    ParsedDate = DateOnly.TryParse(f[2], out DateOnly d),
                    Date = d,
                    StatusName = f[3].Trim()
                })
                .Where(x => x.ParsedId && !existingIds.Contains(x.Id))
                .Where(x => x.ParsedCustomer && lookup.CustomerIds.Contains(x.CustomerId))
                .Where(x => lookup.StatusMap.ContainsKey(x.StatusName))
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
                _context.Orders.AddRange(validEntities);
                await _context.SaveChangesAsync();
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
            result.Message = ex.Message;
        }

        return result;
    }
}
