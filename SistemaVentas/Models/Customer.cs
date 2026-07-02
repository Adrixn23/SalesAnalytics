#nullable disable
using System;
using System.Collections.Generic;

namespace SistemaVentas.Models;

public partial class Customer
{
    public int CustomerId { get; set; }

    public string FirstName { get; set; }

    public string LastName { get; set; }

    public string Email { get; set; }

    public string Phone { get; set; }

    public int CityId { get; set; }
}