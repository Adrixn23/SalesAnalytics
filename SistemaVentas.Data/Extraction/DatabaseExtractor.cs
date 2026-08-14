using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SistemaVentas.Interfaces;
using SistemaVentas.Models;

namespace SistemaVentas.Extraction;

public class DatabaseExtractor : IExtractor<ReviewDto>
{
    private readonly string _connectionString;
    private readonly ILogger<DatabaseExtractor> _logger;

    public DatabaseExtractor(IConfiguration config, ILogger<DatabaseExtractor> logger)
    {
        var connectionString = config.GetConnectionString("VentasHistoricoExternoDB");
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("La cadena de conexión 'VentasHistoricoExternoDB' no está configurada en appsettings.json.");
        }

        _connectionString = connectionString;
        _logger = logger;
    }

    public async Task<IEnumerable<ReviewDto>> ExtractAsync(CancellationToken cancellationToken)
    {
        var results = new List<ReviewDto>();
        var seenIds = new HashSet<int>();

        try
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            using var cmd = new SqlCommand("SELECT ReviewID, CustomerEmail, ProductName, Rating, CommentText, ReviewDate FROM dbo.Reviews", conn);
            using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var reviewId = reader.GetInt32(0);
                if (!seenIds.Add(reviewId))
                {
                    continue;
                }

                results.Add(new ReviewDto
                {
                    ReviewId = reviewId,
                    CustomerEmail = reader.GetString(1),
                    ProductName = reader.GetString(2),
                    Rating = reader.GetInt32(3),
                    CommentText = reader.GetString(4),
                    ReviewDate = reader.GetDateTime(5)
                });
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al extraer datos desde la Base de Datos.");
            return Enumerable.Empty<ReviewDto>();
        }
    }
}
