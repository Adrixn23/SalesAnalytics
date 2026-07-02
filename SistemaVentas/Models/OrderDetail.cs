#nullable disable
using System;
using System.Collections.Generic;

namespace SistemaVentas.Models;

public partial class OrderDetail
{
    public int OrderDetailId { get; set; }

    public int OrderId { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public decimal TotalPrice { get; set; }
}
