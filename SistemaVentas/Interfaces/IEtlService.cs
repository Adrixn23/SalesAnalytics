using SistemaVentas.Models;
using SistemaVentas.Result;

namespace SistemaVentas.Services;

internal interface IEtlService
{
    Task<OperationResult> LoadAsync(LookupContext lookup);
}
