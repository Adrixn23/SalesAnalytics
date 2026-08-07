using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace SistemaVentas.Analytics.Load;

public class Program
{
    private static readonly string ConnectionString = "Data Source=DESKTOP-CROAITG\\SQLEXPRESS01;Initial Catalog=SalesAnalyticsDB;Integrated Security=True;TrustServerCertificate=True";

    public static async Task Main(string[] args)
    {
        Console.WriteLine("Iniciando carga de dimensiones a SalesAnalyticsDB_Analitica...");

        int customerCount = 0;
        int productCount = 0;
        int sellerCount = 0;
        int statusCount = 0;
        int dateCount = 0;
        int salesCount = 0;

        using (var conn = new SqlConnection(ConnectionString))
        {
            await conn.OpenAsync();

            using (var cmd = new SqlCommand("IF OBJECT_ID('Fact.Sales', 'U') IS NOT NULL DELETE FROM Fact.Sales", conn))
                await cmd.ExecuteNonQueryAsync();

            using (var cmd = new SqlCommand("DELETE FROM Dim.Customer; DELETE FROM Dim.Product; DELETE FROM Dim.Seller; DELETE FROM Dim.Status; DELETE FROM Dim.Date;", conn))
                await cmd.ExecuteNonQueryAsync();

            using (var cmd = new SqlCommand("SET IDENTITY_INSERT Dim.Seller ON; INSERT INTO Dim.Seller (SellerKey, SellerID, SellerName, SellerRegion) VALUES (-1, -1, 'Desconocido', 'No disponible'); SET IDENTITY_INSERT Dim.Seller OFF;", conn))
                sellerCount = await cmd.ExecuteNonQueryAsync();

            using (var cmd = new SqlCommand("INSERT INTO Dim.Status (StatusID, StatusName) SELECT StatusID, StatusName FROM Sales.OrderStatus", conn))
                statusCount = await cmd.ExecuteNonQueryAsync();

            Console.WriteLine($"Dim.Status: {statusCount} filas insertadas");

            using (var cmd = new SqlCommand("INSERT INTO Dim.Product (ProductID, ProductName, CategoryName, StandardPrice) SELECT p.ProductID, p.ProductName, c.CategoryName, p.Price FROM Catalog.Products p INNER JOIN Catalog.Categories c ON p.CategoryID = c.CategoryID", conn))
                productCount = await cmd.ExecuteNonQueryAsync();

            Console.WriteLine($"Dim.Product: {productCount} filas insertadas/actualizadas");

            using (var cmd = new SqlCommand("INSERT INTO Dim.Customer (CustomerID, FullName, Email, CityName, RegionName, CountryName, Segment) SELECT cu.CustomerID, cu.FirstName + ' ' + cu.LastName, cu.Email, ci.CityName, 'No disponible', co.CountryName, 'No disponible' FROM People.Customers cu INNER JOIN Geo.Cities ci ON cu.CityID = ci.CityID INNER JOIN Geo.Countries co ON ci.CountryID = co.CountryID", conn))
                customerCount = await cmd.ExecuteNonQueryAsync();

            Console.WriteLine($"Dim.Customer: {customerCount} filas insertadas/actualizadas");

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
                OPTION (MAXRECURSION 0);", conn))
            {
                dateCount = await cmd.ExecuteNonQueryAsync();
            }

            Console.WriteLine($"Dim.Fecha: {dateCount} filas insertadas");

            using (var cmd = new SqlCommand(@"
                INSERT INTO Fact.Sales (OrderID, OrderDetailID, CustomerKey, ProductKey, SellerKey, StatusKey, DateKey, Quantity, UnitPrice, Total)
                SELECT 
                    o.OrderID,
                    od.OrderDetailID,
                    dc.CustomerKey,
                    dp.ProductKey,
                    -1,
                    ds.StatusKey,
                    CAST(FORMAT(o.OrderDate, 'yyyyMMdd') AS INT),
                    od.Quantity,
                    od.TotalPrice / od.Quantity,
                    od.TotalPrice
                FROM Sales.Orders o
                INNER JOIN Sales.OrderDetails od ON o.OrderID = od.OrderID
                INNER JOIN People.Customers c ON o.CustomerID = c.CustomerID
                INNER JOIN Dim.Customer dc ON c.CustomerID = dc.CustomerID
                INNER JOIN Catalog.Products p ON od.ProductID = p.ProductID
                INNER JOIN Dim.Product dp ON p.ProductID = dp.ProductID
                INNER JOIN Sales.OrderStatus s ON o.StatusID = s.StatusID
                INNER JOIN Dim.Status ds ON s.StatusID = ds.StatusID;", conn))
            {
                salesCount = await cmd.ExecuteNonQueryAsync();
            }
        }

        Console.WriteLine();
        Console.WriteLine("============= RESUMEN DE CARGA =============");
        Console.WriteLine($"Dim.Customer:       {customerCount}");
        Console.WriteLine($"Dim.Product:        {productCount}");
        Console.WriteLine($"Dim.Seller:         {sellerCount}");
        Console.WriteLine($"Dim.Status:         {statusCount}");
        Console.WriteLine($"Dim.Date:           {dateCount}");
        Console.WriteLine($"Fact.Sales:         {salesCount}");
        Console.WriteLine("=============================================");
        Console.WriteLine("Proceso finalizado correctamente.");
    }
}
