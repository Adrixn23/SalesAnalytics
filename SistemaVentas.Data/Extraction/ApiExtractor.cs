using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SistemaVentas.Interfaces;
using SistemaVentas.Models;

namespace SistemaVentas.Extraction;

public class ApiExtractor : IExtractor<CommentDto>
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiExtractor> _logger;

    public ApiExtractor(IHttpClientFactory factory, ILogger<ApiExtractor> logger)
    {
        _httpClient = factory.CreateClient("VentasApiExterna");
        _logger = logger;
    }

    public async Task<IEnumerable<CommentDto>> ExtractAsync(CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var data = await _httpClient.GetFromJsonAsync<List<CommentDto>>("api/comments", cancellationToken)
                   ?? new List<CommentDto>();

        var stagingDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "staging");
        if (!Directory.Exists(stagingDir))
        {
            Directory.CreateDirectory(stagingDir);
        }

        var outputPath = Path.Combine(stagingDir, "comments.json");
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(outputPath, json, cancellationToken);

        sw.Stop();
        _logger.LogInformation("ApiExtractor: {Count} comentarios obtenidos en {Ms} ms", data.Count, sw.ElapsedMilliseconds);
        return data;
    }
}
