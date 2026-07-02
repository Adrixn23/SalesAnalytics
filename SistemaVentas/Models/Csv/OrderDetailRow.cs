namespace SistemaVentas.Models.Csv;

internal sealed record OrderDetailRow(
    int     OrderId,
    int     ProductId,
    int     Quantity,
    decimal TotalPrice);
