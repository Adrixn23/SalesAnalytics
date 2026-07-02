using SistemaVentas.Interfaces;
using Microsoft.EntityFrameworkCore;
using SistemaVentas.Models;
using SistemaVentas.Result;

namespace SistemaVentas.Services;

public sealed class OrderStatusService : IEtlService
{
    private readonly SalesAnalyticsDBContext _context;

    public OrderStatusService(SalesAnalyticsDBContext context) => _context = context;

    public async Task<OperationResult> LoadAsync(LookupContext lookup)
    {
        var result = new OperationResult();

        try
        {
            var requiredStatuses = new[] 
            { 
                "Pending", "Shipped", "Delivered", "Cancelled", "Returned" 
            };

            result.Processed = requiredStatuses.Length;

            var existingNames = (await _context.OrderStatuses
                .Select(static s => s.StatusName)
                .ToListAsync())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var newEntities = requiredStatuses
                .Where(name => !existingNames.Contains(name))
                .Select(static name => new OrderStatus { StatusName = name })
                .ToList();

            if (newEntities.Count > 0)
            {
                _context.OrderStatuses.AddRange(newEntities);
                await _context.SaveChangesAsync();
            }

            result.Inserted = newEntities.Count;
            result.Rejected = 0; // Se asume que no hay rechazos en statuses quemados

            lookup.StatusMap = await _context.OrderStatuses
                .ToDictionaryAsync(
                    static s => s.StatusName,
                    static s => s.StatusId,
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

