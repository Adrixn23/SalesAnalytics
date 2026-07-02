#nullable disable
using System;
using System.Collections.Generic;

namespace SistemaVentas.Models;

public partial class Country
{
    public int CountryId { get; set; }

    public string CountryName { get; set; }

    public string Region { get; set; }
}
