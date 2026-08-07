using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
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
        _connectionString = config.GetConnectionString("VentasHistoricoExternoDB")
            ?? "Server=localhost\\SQLEXPRESS;Database=VentasHistoricoExternoDB;Integrated Security=True;TrustServerCertificate=True;";
        _logger = logger;
    }

    public async Task<IEnumerable<ReviewDto>> ExtractAsync(CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var results = new List<ReviewDto>();

        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        using var cmd = new SqlCommand("SELECT ReviewID, CustomerEmail, ProductName, Rating, CommentText, ReviewDate FROM dbo.Reviews", conn);
        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new ReviewDto
            {
                ReviewId = reader.GetInt32(0),
                CustomerEmail = reader.GetString(1),
                ProductName = reader.GetString(2),
                Rating = reader.GetInt32(3),
                CommentText = reader.GetString(4),
                ReviewDate = reader.GetDateTime(5)
            });
        }

        var stagingDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "staging");
        if (!Directory.Exists(stagingDir))
        {
            Directory.CreateDirectory(stagingDir);
        }

        var outputPath = Path.Combine(stagingDir, "reviews.json");
        var json = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(outputPath, json, cancellationToken);

        sw.Stop();
        _logger.LogInformation("DatabaseExtractor: {Count} reseñas extraidas en {Ms} ms", results.Count, sw.ElapsedMilliseconds);
        return results;
    }
}
