namespace SistemaVentas.Result;

public sealed class OperationResult
{
    public bool   Success   { get; set; } = true;
    public string Message   { get; set; } = string.Empty;
    public int    Processed { get; set; }
    public int    Inserted  { get; set; }
    public int    Rejected  { get; set; }
}
