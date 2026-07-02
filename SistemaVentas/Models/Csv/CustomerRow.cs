namespace SistemaVentas.Models.Csv;

internal sealed record CustomerRow(
    int    CustomerId,
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    string City,
    string Country);
