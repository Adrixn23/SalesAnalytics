using System;

namespace SistemaVentas.Models;

public class CommentDto
{
    public int CommentId { get; set; }
    public string CustomerEmail { get; set; }
    public string CommentText { get; set; }
    public DateTime CommentDate { get; set; }
}
