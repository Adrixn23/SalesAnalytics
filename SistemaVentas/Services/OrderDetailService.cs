using Microsoft.EntityFrameworkCore;
using SistemaVentas.Configuration;
using SistemaVentas.Helpers;
using SistemaVentas.Models;
using SistemaVentas.Result;

namespace SistemaVentas.Services;

public sealed class OrderDetailService : IEtlService
{
    private readonly SalesAnalyticsDBContext _context;

    public OrderDetailService(SalesAnalyticsDBContext context) => _context = context;

    public async Task<OperationResult> LoadAsync(LookupContext lookup)
    {
        var result = new OperationResult();

        try
        {
            var allRows = CsvParser.ReadFile(AppSettings.OrderDetailsFile, 4).ToList();
            result.Processed = allRows.Count;

            var existingKeys = (await _context.OrderDetails
                .Select(static od => new { od.OrderId, od.ProductId })
                .ToListAsync())
                .Select(static od => (od.OrderId, od.ProductId))
                .ToHashSet();

            var validEntities = allRows
                .Select(static f => new 
                {
                    ParsedOrder = int.TryParse(f[0], out int oid),
                    OrderId = oid,
                    ParsedProduct = int.TryParse(f[1], out int pid),
                    ProductId = pid,
                    ParsedQty = int.TryParse(f[2], out int qty),
                    Quantity = qty,
                    ParsedPrice = decimal.TryParse(f[3], out decimal price),
                    TotalPrice = price
                })
                .Where(x => x.ParsedOrder && x.ParsedProduct)
                .Where(x => !existingKeys.Contains((x.OrderId, x.ProductId)))
                .Where(x => lookup.OrderIds.Contains(x.OrderId) && lookup.ProductIds.Contains(x.ProductId))
                .Select(x => new OrderDetail
                {
                    OrderId = x.OrderId,
                    ProductId = x.ProductId,
                    Quantity = x.ParsedQty ? x.Quantity : 1,
                    TotalPrice = x.ParsedPrice ? x.TotalPrice : 0m
                })
                .ToList();

            if (validEntities.Count > 0)
            {
                _context.OrderDetails.AddRange(validEntities);
                await _context.SaveChangesAsync();
            }

            result.Inserted = validEntities.Count;
            result.Rejected = result.Processed - result.Inserted - existingKeys.Count;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = ex.Message;
        }

        return result;
    }
}
