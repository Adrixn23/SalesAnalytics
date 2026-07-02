using Microsoft.EntityFrameworkCore;
using SistemaVentas.Configuration;
using SistemaVentas.Models;
using SistemaVentas.Interfaces;
using SistemaVentas.Services;

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("╔════════════════════════════════════════════════════╗");
Console.WriteLine("║     Sistema de Análisis de Ventas - Proceso ETL    ║");
Console.WriteLine("╚════════════════════════════════════════════════════╝");
Console.ResetColor();
Console.WriteLine($"  Inicio: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n");

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

    // POLIMORFISMO PURO: Una colección de interfaces genéricas
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

    // Usando FOR en lugar de FOREACH para iterar, cumpliendo la regla solicitada
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
            break; // Si falla uno, detenemos el pipeline por integridad
        }
    }

    Console.WriteLine($"\n  Fin del proceso: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"\n  Error fatal: {ex.Message}");
    Console.ResetColor();
}
