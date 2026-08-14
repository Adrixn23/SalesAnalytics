using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SistemaVentas.Extraction;
using SistemaVentas.Interfaces;
using SistemaVentas.Models;

namespace SistemaVentas.Analytics.Load;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddTransient<IExtractor<SalesOrderExtractionDto>, SalesOrderDatabaseExtractor>();
        builder.Services.AddTransient<AnalyticsLoadService>();

        var host = builder.Build();

        var loader = host.Services.GetRequiredService<AnalyticsLoadService>();
        await loader.ExecuteLoadAsync();
    }
}
