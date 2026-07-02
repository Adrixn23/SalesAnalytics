using SistemaVentas.Models;
using SistemaVentas.Result;

namespace SistemaVentas.Services;

public interface IEtlService
{
    Task<OperationResult> LoadAsync(LookupContext lookup);
}
