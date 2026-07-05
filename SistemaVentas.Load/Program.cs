using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SistemaVentas.Configuration;
using SistemaVentas.Models;
using SistemaVentas.Interfaces;
using SistemaVentas.Services;
Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("╔════════════════════════════════════════════════════╗");
Console.WriteLine("║     Sistema de Análisis de Ventas - Proceso ETL    ║");
Console.WriteLine("╚════════════════════════════════════════════════════╝");
Console.ResetColor();

var stopwatch = Stopwatch.StartNew();
Console.WriteLine($"  Inicio: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n");


var config = new ConfigurationBuilder()
    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

AppSettings.ConnectionString = config.GetConnectionString("DefaultConnection")!;
AppSettings.CsvDirectory = config.GetSection("EtlSettings")["CsvDirectory"]!;

var optionsBuilder = new DbContextOptionsBuilder<SalesAnalyticsDBContext>();
optionsBuilder.UseSqlServer(AppSettings.ConnectionString);
using var context = new SalesAnalyticsDBContext(optionsBuilder.Options);

try
{
    if (await context.Database.CanConnectAsync())
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("  ✓ Conexión establecida con la base de datos.");
        Console.ResetColor();
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("  ✗ No se pudo conectar a la base de datos.");
        Console.ResetColor();
        return;
    }

    var lookup = new LookupContext();


    IEtlService[] etlServices = 
    {
        new CountryService(context),
        new CityService(context),
        new CategoryService(context),
        new ProductService(context),
        new OrderStatusService(context),
        new CustomerService(context),
        new OrderService(context),
        new OrderDetailService(context)
    };


    for (int i = 0; i < etlServices.Length; i++)
    {
        var service = etlServices[i];
        var serviceName = service.GetType().Name.Replace("Service", "");
        
        Console.Write($"  → Cargando {serviceName,-15} ");
        
        var result = await service.LoadAsync(lookup);
        
        if (result.Success)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[OK]  Procesados: {result.Processed,6} | Insertados: {result.Inserted,6} | Rechazados: {result.Rejected,6}");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR] {result.Message}");
            Console.ResetColor();
            break;
        }
    }

    stopwatch.Stop();
    Console.WriteLine($"\n  Fin del proceso: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"  Tiempo total de ejecución: {stopwatch.Elapsed.TotalSeconds:F2} segundos");
    Console.ResetColor();
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"\n  Error fatal: {ex.Message}");
    Console.ResetColor();
}
