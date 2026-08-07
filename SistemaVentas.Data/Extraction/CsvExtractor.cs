using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Configuration;
using SistemaVentas.Interfaces;
using SistemaVentas.Models.Csv;
namespace SistemaVentas.Extraction;
public class CsvExtractor<T> : IExtractor<T>
{
    private readonly string _csvDirectory;

    public CsvExtractor(IConfiguration configuration)
    {
        _csvDirectory = configuration.GetSection("EtlSettings")["CsvDirectory"] 
                        ?? throw new InvalidOperationException("CSV directory not configured.");
    }
    private string GetPathForType()
    {
        var type = typeof(T);
        if (type == typeof(CustomerRow)) return Path.Combine(_csvDirectory, "customers.csv");
        if (type == typeof(ProductRow)) return Path.Combine(_csvDirectory, "products.csv");
        if (type == typeof(OrderRow)) return Path.Combine(_csvDirectory, "orders.csv");
        if (type == typeof(OrderDetailRow)) return Path.Combine(_csvDirectory, "order_details.csv");
        throw new InvalidOperationException($"Type not supported: {type.Name}");
    }
    public async Task<IEnumerable<T>> ExtractAsync(CancellationToken cancellationToken)
    {
        var filePath = GetPathForType();
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            HeaderValidated = null
        };
     
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, config);
        var records = new List<T>();
        await foreach (var record in csv.GetRecordsAsync<T>())
        {
            cancellationToken.ThrowIfCancellationRequested();
            records.Add(record);
        }
        return records;
    }
}
