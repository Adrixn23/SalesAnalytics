#nullable disable


namespace SistemaVentas.Models;

public partial class Product
{
    public int ProductId { get; set; }

    public string ProductName { get; set; }

    public int CategoryId { get; set; }

    public decimal Price { get; set; }

    public int Stock { get; set; }
}
