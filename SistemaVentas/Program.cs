using Microsoft.Data.SqlClient;
using SistemaVentas.Configuration;

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("╔════════════════════════════════════════════════════╗");
Console.WriteLine("║     Sistema de Análisis de Ventas - Proceso ETL    ║");
Console.WriteLine("╚════════════════════════════════════════════════════╝");
Console.ResetColor();
Console.WriteLine($"  Inicio: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n");

try
{
    using var conn = new SqlConnection(AppSettings.ConnectionString);
    conn.Open();

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("  ✓ Conexión establecida con SalesAnalyticsDB");
    Console.ResetColor();
    Console.WriteLine($"  ✓ Directorio CSV: {AppSettings.CsvDirectory}\n");
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"  [ERROR DE CONEXIÓN] {ex.Message}");
    Console.ResetColor();
}
