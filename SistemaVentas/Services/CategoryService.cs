using Microsoft.EntityFrameworkCore;
using SistemaVentas.Configuration;
using SistemaVentas.Helpers;
using SistemaVentas.Models;
using SistemaVentas.Result;

namespace SistemaVentas.Services;

public sealed class CategoryService : IEtlService
{
    private readonly SalesAnalyticsDBContext _context;

    public CategoryService(SalesAnalyticsDBContext context) => _context = context;

    public async Task<OperationResult> LoadAsync(LookupContext lookup)
    {
        var result = new OperationResult();

        try
        {
            var allRows = CsvParser.ReadFile(AppSettings.ProductsFile, 5).ToList();

            var validNames = allRows
                .Select(static f => f[2].Trim())
                .Where(static name => !string.IsNullOrWhiteSpace(name))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            result.Processed = validNames.Count;
            result.Rejected  = allRows.Count(static f => string.IsNullOrWhiteSpace(f[2].Trim()));

            var existingNames = (await _context.Categories
                .Select(static c => c.CategoryName)
                .ToListAsync())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var newEntities = validNames
                .Where(name => !existingNames.Contains(name))
                .Select(static name => new Category { CategoryName = name })
                .ToList();

            if (newEntities.Count > 0)
            {
                _context.Categories.AddRange(newEntities);
                await _context.SaveChangesAsync();
            }

            result.Inserted = newEntities.Count;

            lookup.CategoryMap = await _context.Categories
                .ToDictionaryAsync(
                    static c => c.CategoryName,
                    static c => c.CategoryId,
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
