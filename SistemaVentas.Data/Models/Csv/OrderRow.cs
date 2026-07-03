using CsvHelper.Configuration.Attributes;

namespace SistemaVentas.Models.Csv;

public class OrderRow
{
    [Name("OrderID")]    public string OrderId { get; set; }
    [Name("CustomerID")] public string CustomerId { get; set; }
    [Name("OrderDate")]  public string OrderDate { get; set; }
    [Name("Status")]     public string Status { get; set; }
}
