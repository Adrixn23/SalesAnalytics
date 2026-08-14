using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SistemaVentas.Interfaces;
using SistemaVentas.Models;

namespace SistemaVentas.Analytics.Load;

public class AnalyticsLoadService
{
    private readonly string _oltpConnectionString;
    private readonly string _olapConnectionString;
    private readonly IExtractor<SalesOrderExtractionDto> _salesExtractor;
    private readonly ILogger<AnalyticsLoadService> _logger;

    public AnalyticsLoadService(
        IConfiguration config,
        IExtractor<SalesOrderExtractionDto> salesExtractor,
        ILogger<AnalyticsLoadService> logger)
    {
        _oltpConnectionString = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection connection string not found.");
        _olapConnectionString = config.GetConnectionString("AnalyticsConnection")
            ?? config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("AnalyticsConnection connection string not found.");
        _salesExtractor = salesExtractor ?? throw new ArgumentNullException(nameof(salesExtractor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ExecuteLoadAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Iniciando proceso de carga limpia del Data Warehouse (OLAP)...");

        try
        {
            await CleanTablesAsync(cancellationToken);
            await LoadDimensionsAsync(cancellationToken);
            await LoadFactSalesAsync(cancellationToken);

            _logger.LogInformation("Proceso de carga del Data Warehouse finalizado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante la ejecución de la carga del Data Warehouse.");
            throw;
        }
    }

    private async Task CleanTablesAsync(CancellationToken cancellationToken)
    {
        using var conn = new SqlConnection(_olapConnectionString);
        await conn.OpenAsync(cancellationToken);

        using (var cmd = new SqlCommand("IF OBJECT_ID('Fact.Sales', 'U') IS NOT NULL TRUNCATE TABLE Fact.Sales", conn))
        {
            await cmd.ExecuteNonQueryAsync(cancellationToken);
            _logger.LogInformation("Tabla Fact.Sales limpiada exitosamente (TRUNCATE).");
        }

        using (var cmd = new SqlCommand("DELETE FROM Dim.Customer; DELETE FROM Dim.Product; DELETE FROM Dim.Seller; DELETE FROM Dim.Status; DELETE FROM Dim.Date;", conn))
        {
            await cmd.ExecuteNonQueryAsync(cancellationToken);
            _logger.LogInformation("Tablas de dimensiones limpiadas exitosamente.");
        }
    }

    private async Task LoadDimensionsAsync(CancellationToken cancellationToken)
    {
        using var dwConn = new SqlConnection(_olapConnectionString);
        await dwConn.OpenAsync(cancellationToken);

        using (var cmd = new SqlCommand("SET IDENTITY_INSERT Dim.Seller ON; INSERT INTO Dim.Seller (SellerKey, SellerID, SellerName, SellerRegion) VALUES (-1, -1, 'Desconocido', 'No disponible'); SET IDENTITY_INSERT Dim.Seller OFF;", dwConn))
        {
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        using (var cmd = new SqlCommand("INSERT INTO Dim.Status (StatusID, StatusName) SELECT StatusID, StatusName FROM Sales.OrderStatus", dwConn))
        {
            int count = await cmd.ExecuteNonQueryAsync(cancellationToken);
            _logger.LogInformation("Dim.Status: {Count} filas cargadas.", count);
        }

        using (var cmd = new SqlCommand("INSERT INTO Dim.Product (ProductID, ProductName, CategoryName, StandardPrice) SELECT p.ProductID, p.ProductName, c.CategoryName, p.Price FROM Catalog.Products p INNER JOIN Catalog.Categories c ON p.CategoryID = c.CategoryID", dwConn))
        {
            int count = await cmd.ExecuteNonQueryAsync(cancellationToken);
            _logger.LogInformation("Dim.Product: {Count} filas cargadas.", count);
        }

        using (var cmd = new SqlCommand("INSERT INTO Dim.Customer (CustomerID, FullName, Email, CityName, RegionName, CountryName, Segment) SELECT cu.CustomerID, cu.FirstName + ' ' + cu.LastName, cu.Email, ci.CityName, 'No disponible', co.CountryName, 'No disponible' FROM People.Customers cu INNER JOIN Geo.Cities ci ON cu.CityID = ci.CityID INNER JOIN Geo.Countries co ON ci.CountryID = co.CountryID", dwConn))
        {
            int count = await cmd.ExecuteNonQueryAsync(cancellationToken);
            _logger.LogInformation("Dim.Customer: {Count} filas cargadas.", count);
        }

        using (var cmd = new SqlCommand(@"
            DECLARE @StartDate DATE = '2023-01-01';
            DECLARE @EndDate DATE = '2025-12-31';
            WITH DateCTE AS (
                SELECT @StartDate AS DateValue
                UNION ALL
                SELECT DATEADD(day, 1, DateValue)
                FROM DateCTE
                WHERE DateValue < @EndDate
            )
            INSERT INTO Dim.Date (DateKey, FullDate, Year, Month, MonthName, Quarter)
            SELECT 
                CAST(FORMAT(DateValue, 'yyyyMMdd') AS INT),
                DateValue,
                YEAR(DateValue),
                MONTH(DateValue),
                FORMAT(DateValue, 'MMMM', 'es-ES'),
                DATEPART(quarter, DateValue)
            FROM DateCTE
            OPTION (MAXRECURSION 0);", dwConn))
        {
            int count = await cmd.ExecuteNonQueryAsync(cancellationToken);
            _logger.LogInformation("Dim.Date: {Count} filas cargadas.", count);
        }
    }

    private async Task LoadFactSalesAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Extrayendo ventas desde la base de datos OLTP...");
        var extractedSales = (await _salesExtractor.ExtractAsync(cancellationToken)).ToList();

        if (extractedSales.Count == 0)
        {
            _logger.LogWarning("No se encontraron registros de ventas para cargar.");
            return;
        }

        _logger.LogInformation("Cargando mapeo de llaves desde el Data Warehouse (OLAP)...");
        using var dwConn = new SqlConnection(_olapConnectionString);
        await dwConn.OpenAsync(cancellationToken);

        var customerLookup = new Dictionary<int, int>();
        using (var cmd = new SqlCommand("SELECT CustomerID, CustomerKey FROM Dim.Customer", dwConn))
        using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                customerLookup[reader.GetInt32(0)] = reader.GetInt32(1);
            }
        }

        var productLookup = new Dictionary<int, int>();
        using (var cmd = new SqlCommand("SELECT ProductID, ProductKey FROM Dim.Product", dwConn))
        using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                productLookup[reader.GetInt32(0)] = reader.GetInt32(1);
            }
        }

        var statusLookup = new Dictionary<int, int>();
        using (var cmd = new SqlCommand("SELECT StatusID, StatusKey FROM Dim.Status", dwConn))
        using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                statusLookup[reader.GetInt32(0)] = reader.GetInt32(1);
            }
        }

        _logger.LogInformation("Insertando registros en Fact.Sales...");

        var dataTable = new DataTable();
        dataTable.Columns.Add("OrderID", typeof(int));
        dataTable.Columns.Add("OrderDetailID", typeof(int));
        dataTable.Columns.Add("CustomerKey", typeof(int));
        dataTable.Columns.Add("ProductKey", typeof(int));
        dataTable.Columns.Add("SellerKey", typeof(int));
        dataTable.Columns.Add("StatusKey", typeof(int));
        dataTable.Columns.Add("DateKey", typeof(int));
        dataTable.Columns.Add("Quantity", typeof(int));
        dataTable.Columns.Add("UnitPrice", typeof(decimal));
        dataTable.Columns.Add("Total", typeof(decimal));

        foreach (var sale in extractedSales)
        {
            if (!customerLookup.TryGetValue(sale.CustomerId, out int customerKey)) continue;
            if (!productLookup.TryGetValue(sale.ProductId, out int productKey)) continue;
            if (!statusLookup.TryGetValue(sale.StatusId, out int statusKey)) continue;

            int dateKey = int.Parse(sale.OrderDate.ToString("yyyyMMdd"));
            decimal unitPrice = sale.Quantity > 0 ? sale.TotalPrice / sale.Quantity : 0m;

            dataTable.Rows.Add(
                sale.OrderId,
                sale.OrderDetailId,
                customerKey,
                productKey,
                -1,
                statusKey,
                dateKey,
                sale.Quantity,
                unitPrice,
                sale.TotalPrice
            );
        }

        using var bulkCopy = new SqlBulkCopy(dwConn)
        {
            DestinationTableName = "Fact.Sales",
            BatchSize = 5000,
            BulkCopyTimeout = 120
        };

        bulkCopy.ColumnMappings.Add("OrderID", "OrderID");
        bulkCopy.ColumnMappings.Add("OrderDetailID", "OrderDetailID");
        bulkCopy.ColumnMappings.Add("CustomerKey", "CustomerKey");
        bulkCopy.ColumnMappings.Add("ProductKey", "ProductKey");
        bulkCopy.ColumnMappings.Add("SellerKey", "SellerKey");
        bulkCopy.ColumnMappings.Add("StatusKey", "StatusKey");
        bulkCopy.ColumnMappings.Add("DateKey", "DateKey");
        bulkCopy.ColumnMappings.Add("Quantity", "Quantity");
        bulkCopy.ColumnMappings.Add("UnitPrice", "UnitPrice");
        bulkCopy.ColumnMappings.Add("Total", "Total");

        await bulkCopy.WriteToServerAsync(dataTable, cancellationToken);
        _logger.LogInformation("Fact.Sales: {Count} filas insertadas correctamente mediante SqlBulkCopy.", dataTable.Rows.Count);
    }
}
