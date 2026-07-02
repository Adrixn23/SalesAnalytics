using CsvHelper.Configuration.Attributes;

namespace SistemaVentas.Models.Csv;

public class CustomerRow
{
    [Name("CustomerID")] public string CustomerId { get; set; }
    [Name("FirstName")]  public string FirstName { get; set; }
    [Name("LastName")]   public string LastName { get; set; }
    [Name("Email")]      public string Email { get; set; }
    [Name("Phone")]      public string Phone { get; set; }
    [Name("City")]       public string City { get; set; }
    [Name("Country")]    public string Country { get; set; }
}
