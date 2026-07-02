#nullable disable
using System;
using System.Collections.Generic;

namespace SistemaVentas.Models;

public partial class Order
{
    public int OrderId { get; set; }

    public int CustomerId { get; set; }

    public DateOnly OrderDate { get; set; }

    public int StatusId { get; set; }
}