using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SistemaVentas.Interfaces;
using SistemaVentas.Models;

namespace SistemaVentas.Extraction;

public class SalesOrderDatabaseExtractor : IExtractor<SalesOrderExtractionDto>
{
    private readonly string _connectionString;
    private readonly ILogger<SalesOrderDatabaseExtractor> _logger;

    public SalesOrderDatabaseExtractor(IConfiguration config, ILogger<SalesOrderDatabaseExtractor> logger)
    {
        var connectionString = config.GetConnectionString("DefaultConnection");
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("La cadena de conexión 'DefaultConnection' no está configurada en appsettings.json.");
        }

        _connectionString = connectionString;
        _logger = logger;
    }

    public async Task<IEnumerable<SalesOrderExtractionDto>> ExtractAsync(CancellationToken cancellationToken)
    {
        var results = new List<SalesOrderExtractionDto>();
        var seenDetailIds = new HashSet<int>();

        try
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            var query = @"
                SELECT 
                    o.OrderID,
                    od.OrderDetailID,
                    o.CustomerID,
                    od.ProductID,
                    o.StatusID,
                    o.OrderDate,
                    od.Quantity,
                    od.TotalPrice
                FROM Sales.Orders o
                INNER JOIN Sales.OrderDetails od ON o.OrderID = od.OrderID";

            using var cmd = new SqlCommand(query, conn);
            using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var orderDetailId = reader.GetInt32(1);
                if (!seenDetailIds.Add(orderDetailId))
                {
                    continue;
                }

                results.Add(new SalesOrderExtractionDto
                {
                    OrderId = reader.GetInt32(0),
                    OrderDetailId = orderDetailId,
                    CustomerId = reader.GetInt32(2),
                    ProductId = reader.GetInt32(3),
                    StatusId = reader.GetInt32(4),
                    OrderDate = reader.GetDateTime(5),
                    Quantity = reader.GetInt32(6),
                    TotalPrice = reader.GetDecimal(7)
                });
            }

            _logger.LogInformation("SalesOrderDatabaseExtractor: {Count} registros de ventas extraídos exitosamente de OLTP.", results.Count);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al extraer datos de ventas desde la base de datos OLTP.");
            return Enumerable.Empty<SalesOrderExtractionDto>();
        }
    }
}
