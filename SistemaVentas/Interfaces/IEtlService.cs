using SistemaVentas.Models;
using SistemaVentas.Result;
using System.Threading.Tasks;

namespace SistemaVentas.Interfaces;

public interface IEtlService
{
    Task<OperationResult> LoadAsync(LookupContext lookup);
}
