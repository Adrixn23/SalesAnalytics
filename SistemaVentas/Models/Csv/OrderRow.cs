namespace SistemaVentas.Models.Csv;

internal sealed record OrderRow(
    int      OrderId,
    int      CustomerId,
    DateOnly OrderDate,
    string   Status);
