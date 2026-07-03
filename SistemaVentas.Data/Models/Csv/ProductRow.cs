using CsvHelper.Configuration.Attributes;

namespace SistemaVentas.Models.Csv;

public class ProductRow
{
    [Name("ProductID")]   public string ProductId { get; set; }
    [Name("ProductName")] public string ProductName { get; set; }
    [Name("Category")]    public string Category { get; set; }
    [Name("Price")]       public string Price { get; set; }
    [Name("Stock")]       public string Stock { get; set; }
}
