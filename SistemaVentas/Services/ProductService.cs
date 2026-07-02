using Microsoft.EntityFrameworkCore;
using SistemaVentas.Configuration;
using SistemaVentas.Helpers;
using SistemaVentas.Models;
using SistemaVentas.Result;

namespace SistemaVentas.Services;

public sealed class ProductService : IEtlService
{
    private readonly SalesAnalyticsDBContext _context;

    public ProductService(SalesAnalyticsDBContext context) => _context = context;

    public async Task<OperationResult> LoadAsync(LookupContext lookup)
    {
        var result = new OperationResult();

        try
        {
            var allRows = CsvParser.ReadFile(AppSettings.ProductsFile, 5).ToList();
            result.Processed = allRows.Count;

            var existingIds = await _context.Products
                .Select(static p => p.ProductId)
                .ToHashSetAsync();

            var validEntities = allRows
                .Select(static f => new 
                {
                    ParsedId = int.TryParse(f[0], out int id),
                    Id = id,
                    Name = f[1].Trim(),
                    CategoryName = f[2].Trim(),
                    ParsedPrice = decimal.TryParse(f[3], out decimal price),
                    Price = price,
                    ParsedStock = int.TryParse(f[4], out int stock),
                    Stock = stock
                })
                .Where(x => x.ParsedId && !existingIds.Contains(x.Id))
                .Where(x => lookup.CategoryMap.ContainsKey(x.CategoryName))
                .Select(x => new Product
                {
                    ProductId = x.Id,
                    ProductName = x.Name,
                    CategoryId = lookup.CategoryMap[x.CategoryName],
                    Price = x.ParsedPrice ? x.Price : 0m,
                    Stock = x.ParsedStock ? x.Stock : 0
                })
                .ToList();

            if (validEntities.Count > 0)
            {
                _context.Products.AddRange(validEntities);
                await _context.SaveChangesAsync();
            }

            result.Inserted = validEntities.Count;
            result.Rejected = result.Processed - result.Inserted - existingIds.Count;

            lookup.ProductIds = await _context.Products
                .Select(static p => p.ProductId)
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
