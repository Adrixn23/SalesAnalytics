using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SistemaVentas.Interfaces;
using SistemaVentas.Models;
using SistemaVentas.Models.Csv;

namespace SistemaVentas.Load;

public class EtlWorker : BackgroundService
{
    private readonly IExtractor<CustomerRow> _customerExtractor;
    private readonly IExtractor<ProductRow> _productExtractor;
    private readonly IExtractor<OrderRow> _orderExtractor;
    private readonly IExtractor<OrderDetailRow> _orderDetailExtractor;
    private readonly IExtractor<ReviewDto> _dbExtractor;
    private readonly IExtractor<CommentDto> _apiExtractor;
    private readonly IServiceProvider _serviceProvider;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<EtlWorker> _logger;

    public EtlWorker(
        IExtractor<CustomerRow> customerExtractor,
        IExtractor<ProductRow> productExtractor,
        IExtractor<OrderRow> orderExtractor,
        IExtractor<OrderDetailRow> orderDetailExtractor,
        IExtractor<ReviewDto> dbExtractor,
        IExtractor<CommentDto> apiExtractor,
        IServiceProvider serviceProvider,
        IHostApplicationLifetime lifetime,
        ILogger<EtlWorker> logger)
    {
        _customerExtractor = customerExtractor;
        _productExtractor = productExtractor;
        _orderExtractor = orderExtractor;
        _orderDetailExtractor = orderDetailExtractor;
        _dbExtractor = dbExtractor;
        _apiExtractor = apiExtractor;
        _serviceProvider = serviceProvider;
        _lifetime = lifetime;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker iniciado en: {time}", DateTimeOffset.Now);

        try
        {
            var sw = Stopwatch.StartNew();

            IEnumerable<CustomerRow> customers = Array.Empty<CustomerRow>();
            IEnumerable<ProductRow> products = Array.Empty<ProductRow>();
            IEnumerable<OrderRow> orders = Array.Empty<OrderRow>();
            IEnumerable<OrderDetailRow> orderDetails = Array.Empty<OrderDetailRow>();
            IEnumerable<ReviewDto> reviews = Array.Empty<ReviewDto>();
            IEnumerable<CommentDto> comments = Array.Empty<CommentDto>();

            var tasks = new List<Task>
            {
                Task.Run(async () =>
                {
                    try
                    {
                        customers = await _customerExtractor.ExtractAsync(stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error extrayendo clientes desde CSV.");
                    }
                }, stoppingToken),

                Task.Run(async () =>
                {
                    try
                    {
                        products = await _productExtractor.ExtractAsync(stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error extrayendo productos desde CSV.");
                    }
                }, stoppingToken),

                Task.Run(async () =>
                {
                    try
                    {
                        orders = await _orderExtractor.ExtractAsync(stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error extrayendo órdenes desde CSV.");
                    }
                }, stoppingToken),

                Task.Run(async () =>
                {
                    try
                    {
                        orderDetails = await _orderDetailExtractor.ExtractAsync(stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error extrayendo detalles de órdenes desde CSV.");
                    }
                }, stoppingToken),

                Task.Run(async () =>
                {
                    try
                    {
                        reviews = await _dbExtractor.ExtractAsync(stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error extrayendo reseñas desde Base de Datos.");
                    }
                }, stoppingToken),

                Task.Run(async () =>
                {
                    try
                    {
                        comments = await _apiExtractor.ExtractAsync(stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error extrayendo comentarios desde la API.");
                    }
                }, stoppingToken)
            };

            await Task.WhenAll(tasks);
            sw.Stop();
            _logger.LogInformation("Fase de Extracción completada en {Elapsed} ms.", sw.ElapsedMilliseconds);

            _logger.LogInformation("Iniciando fase de Carga (Load) a Base de Datos Analítica...");
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<SalesAnalyticsDBContext>();
                if (!await context.Database.CanConnectAsync(stoppingToken))
                {
                    _logger.LogError("No se pudo conectar a la base de datos destino.");
                    return;
                }

                var lookup = new LookupContext();
                var services = scope.ServiceProvider.GetRequiredService<IEnumerable<IEtlService>>();

                foreach (var service in services)
                {
                    var serviceName = service.GetType().Name.Replace("Service", "");
                    _logger.LogInformation("Ejecutando servicio de carga: {ServiceName}", serviceName);

                    var result = await service.LoadAsync(lookup);
                    if (result.Success)
                    {
                        _logger.LogInformation("[OK] {ServiceName} -> Procesados: {Processed} | Insertados: {Inserted} | Rechazados: {Rejected}",
                            serviceName, result.Processed, result.Inserted, result.Rejected);
                    }
                    else
                    {
                        _logger.LogError("[ERROR] {ServiceName} -> {Message}", serviceName, result.Message);
                        break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fatal en la ejecución del proceso ETL.");
        }
        finally
        {
            _lifetime.StopApplication();
        }
    }
}
