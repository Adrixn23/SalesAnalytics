using System;

namespace SistemaVentas.Models;

public class SalesOrderExtractionDto
{
    public int OrderId { get; set; }
    public int OrderDetailId { get; set; }
    public int CustomerId { get; set; }
    public int ProductId { get; set; }
    public int StatusId { get; set; }
    public DateTime OrderDate { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
}
