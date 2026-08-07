using System;

namespace SistemaVentas.Models;

public class ReviewDto
{
    public int ReviewId { get; set; }
    public string CustomerEmail { get; set; }
    public string ProductName { get; set; }
    public int Rating { get; set; }
    public string CommentText { get; set; }
    public DateTime ReviewDate { get; set; }
}
