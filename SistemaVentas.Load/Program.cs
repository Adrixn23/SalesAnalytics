using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SistemaVentas.Configuration;
using SistemaVentas.Extraction;
using SistemaVentas.Interfaces;
using SistemaVentas.Models;
using SistemaVentas.Models.Csv;
using SistemaVentas.Services;

namespace SistemaVentas.Load;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                               ?? throw new InvalidOperationException("DefaultConnection connection string not found.");
        AppSettings.ConnectionString = connectionString;
        AppSettings.CsvDirectory = builder.Configuration.GetSection("EtlSettings")["CsvDirectory"] 
                                   ?? throw new InvalidOperationException("CSV directory not found.");

        builder.Services.AddHttpClient("VentasApiExterna", client =>
        {
            client.BaseAddress = new Uri(builder.Configuration["ExternalApi:BaseUrl"] ?? "https://localhost:7050/");
        });

        builder.Services.AddDbContext<SalesAnalyticsDBContext>(options =>
            options.UseSqlServer(connectionString));

        builder.Services.AddTransient<IExtractor<CustomerRow>>(sp =>
            new CsvExtractor<CustomerRow>(System.IO.Path.Combine(AppSettings.CsvDirectory, "customers.csv"), sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<CsvExtractor<CustomerRow>>>()));
        builder.Services.AddTransient<IExtractor<ProductRow>>(sp =>
            new CsvExtractor<ProductRow>(System.IO.Path.Combine(AppSettings.CsvDirectory, "products.csv"), sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<CsvExtractor<ProductRow>>>()));
        builder.Services.AddTransient<IExtractor<OrderRow>>(sp =>
            new CsvExtractor<OrderRow>(System.IO.Path.Combine(AppSettings.CsvDirectory, "orders.csv"), sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<CsvExtractor<OrderRow>>>()));
        builder.Services.AddTransient<IExtractor<OrderDetailRow>>(sp =>
            new CsvExtractor<OrderDetailRow>(System.IO.Path.Combine(AppSettings.CsvDirectory, "order_details.csv"), sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<CsvExtractor<OrderDetailRow>>>()));
        builder.Services.AddTransient<IExtractor<ReviewDto>, DatabaseExtractor>();
        builder.Services.AddTransient<IExtractor<CommentDto>, ApiExtractor>();

        builder.Services.AddScoped<IEtlService, CountryService>();
        builder.Services.AddScoped<IEtlService, CityService>();
        builder.Services.AddScoped<IEtlService, CategoryService>();
        builder.Services.AddScoped<IEtlService, ProductService>();
        builder.Services.AddScoped<IEtlService, OrderStatusService>();
        builder.Services.AddScoped<IEtlService, CustomerService>();
        builder.Services.AddScoped<IEtlService, OrderService>();
        builder.Services.AddScoped<IEtlService, OrderDetailService>();

        builder.Services.AddHostedService<EtlWorker>();

        var host = builder.Build();
        await host.RunAsync();
    }
}
