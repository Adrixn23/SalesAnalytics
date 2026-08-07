using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SistemaVentas.Interfaces;

public interface IExtractor<T>
{
    Task<IEnumerable<T>> ExtractAsync(CancellationToken cancellationToken);
}
