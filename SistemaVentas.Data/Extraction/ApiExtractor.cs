using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
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
        try
        {
            var data = await _httpClient.GetFromJsonAsync<List<CommentDto>>("api/comments", cancellationToken);
            return data ?? Enumerable.Empty<CommentDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extrayendo datos desde la API REST.");
            return Enumerable.Empty<CommentDto>();
        }
    }
}
