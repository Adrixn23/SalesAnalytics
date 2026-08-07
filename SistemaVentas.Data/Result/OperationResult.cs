namespace SistemaVentas.Result
{
    public class OperationResult
    {
        public OperationResult()
        {
            this.Success = true;
        }

        public bool Success { get; set; }
        public string? Message { get; set; }


        public int Processed { get; set; }
        public int Inserted { get; set; }
        public int Rejected { get; set; }
    }
}
