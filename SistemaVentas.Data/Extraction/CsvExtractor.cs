using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using SistemaVentas.Interfaces;

namespace SistemaVentas.Extraction;

public class CsvExtractor<T> : IExtractor<T>
{
    private readonly string _filePath;
    private readonly ILogger<CsvExtractor<T>> _logger;

    public CsvExtractor(string filePath, ILogger<CsvExtractor<T>> logger)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<T>> ExtractAsync(CancellationToken cancellationToken)
    {
        var records = new List<T>();

        try
        {
            if (!File.Exists(_filePath))
            {
                _logger.LogWarning("El archivo CSV no existe en la ruta especificada: {FilePath}", _filePath);
                return records;
            }

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null,
                HeaderValidated = null
            };

            using var reader = new StreamReader(_filePath);
            using var csv = new CsvReader(reader, config);

            await foreach (var record in csv.GetRecordsAsync<T>())
            {
                cancellationToken.ThrowIfCancellationRequested();
                records.Add(record);
            }

            _logger.LogInformation("CsvExtractor<{Type}>: {Count} registros extraídos correctamente de {FilePath}", typeof(T).Name, records.Count, _filePath);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Operación de extracción para {FilePath} fue cancelada.", _filePath);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al extraer datos desde el archivo CSV: {FilePath}", _filePath);
        }

        return records;
    }
}
