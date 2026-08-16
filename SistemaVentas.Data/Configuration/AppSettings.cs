using System.IO;

namespace SistemaVentas.Configuration;

public static class AppSettings
{
    public static string ConnectionString { get; set; } = string.Empty;

    public static string CsvDirectory { get; set; } = string.Empty;

    public static string CustomersFile   => Path.Combine(CsvDirectory, "customers.csv");
    public static string ProductsFile    => Path.Combine(CsvDirectory, "products.csv");
    public static string OrdersFile      => Path.Combine(CsvDirectory, "orders.csv");
    public static string OrderDetailsFile => Path.Combine(CsvDirectory, "order_details.csv");
}
