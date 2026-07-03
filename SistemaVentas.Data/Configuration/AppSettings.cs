namespace SistemaVentas.Configuration;

public static class AppSettings
{
    public static string ConnectionString { get; set; } = 
        @"Data Source=DESKTOP-CROAITG\SQLEXPRESS01;Initial Catalog=SalesAnalyticsDB;Integrated Security=True;Trust Server Certificate=True";

    public static string CsvDirectory { get; set; } = 
        @"C:\Adrian\ITLA_Materias ETC\FrancisElec1\Archivo CSV Análisis de Ventas-20260603";

    public static string CustomersFile   => Path.Combine(CsvDirectory, "customers.csv");
    public static string ProductsFile    => Path.Combine(CsvDirectory, "products.csv");
    public static string OrdersFile      => Path.Combine(CsvDirectory, "orders.csv");
    public static string OrderDetailsFile => Path.Combine(CsvDirectory, "order_details.csv");
}
