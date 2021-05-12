using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WebsocketBroker.Abstractions.POCO;

namespace WebsocketBroker.Abstractions
{
    public interface IRequestHandler
    {
        Task BeginPorcessAsync(CancellationToken cancellationToken);

        IEnumerable<ContextRecord> GetContext(CancellationToken cancellationToken);
    }
}
