using Microsoft.EntityFrameworkCore;
using SistemaVentas.Configuration;
using SistemaVentas.Interfaces;
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
            using var reader = new System.IO.StreamReader(AppSettings.ProductsFile);
            using var csv = new CsvHelper.CsvReader(reader, System.Globalization.CultureInfo.InvariantCulture);
            var allRows = csv.GetRecords<Models.Csv.ProductRow>().ToList();
            
            result.Processed = allRows.Count;

            var existingIds = await _context.Products
                .Select(static p => p.ProductId)
                .ToHashSetAsync();

            var validEntities = allRows
                .Select(static f => new 
                {
                    ParsedId = int.TryParse(f.ProductId, out int id),
                    Id = id,
                    Name = f.ProductName?.Trim(),
                    CategoryName = f.Category?.Trim(),
                    ParsedPrice = decimal.TryParse(f.Price, out decimal price),
                    Price = price,
                    ParsedStock = int.TryParse(f.Stock, out int stock),
                    Stock = stock
                })
                .Where(x => x.ParsedId && !existingIds.Contains(x.Id))
                .Where(x => x.CategoryName != null && lookup.CategoryMap.ContainsKey(x.CategoryName))
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
                await _context.Database.OpenConnectionAsync();
                try
                {
                    await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Catalog.Products ON");
                    _context.Products.AddRange(validEntities);
                    await _context.SaveChangesAsync();
                    await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Catalog.Products OFF");
                }
                finally
                {
                    await _context.Database.CloseConnectionAsync();
                }
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
            result.Message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
        }

        return result;
    }
}
