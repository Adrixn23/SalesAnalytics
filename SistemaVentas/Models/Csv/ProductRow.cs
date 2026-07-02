namespace SistemaVentas.Models.Csv;

internal sealed record ProductRow(
    int     ProductId,
    string  ProductName,
    string  Category,
    decimal Price,
    int     Stock);
