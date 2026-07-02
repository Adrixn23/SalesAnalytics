using CsvHelper.Configuration.Attributes;

namespace SistemaVentas.Models.Csv;

public class OrderDetailRow
{
    [Name("OrderID")]    public string OrderId { get; set; }
    [Name("ProductID")]  public string ProductId { get; set; }
    [Name("Quantity")]   public string Quantity { get; set; }
    [Name("TotalPrice")] public string TotalPrice { get; set; }
}
