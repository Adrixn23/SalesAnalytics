namespace SistemaVentas.Configuration;

public static class AppSettings
{
    internal static readonly string ConnectionString =
        @"Data Source=DESKTOP-CROAITG\SQLEXPRESS01;Initial Catalog=SalesAnalyticsDB;Integrated Security=True;Trust Server Certificate=True";

    internal static readonly string CsvDirectory =
        @"C:\Adrian\ITLA_Materias ETC\FrancisElec1\Archivo CSV Análisis de Ventas-20260603";

    internal static string CustomersFile   => Path.Combine(CsvDirectory, "customers.csv");
    internal static string ProductsFile    => Path.Combine(CsvDirectory, "products.csv");
    internal static string OrdersFile      => Path.Combine(CsvDirectory, "orders.csv");
    internal static string OrderDetailsFile => Path.Combine(CsvDirectory, "order_details.csv");
}
