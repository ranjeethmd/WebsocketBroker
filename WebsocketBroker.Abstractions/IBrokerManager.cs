using System.Threading;
using System.Threading.Tasks;

namespace WebsocketBroker.Abstractions
{
    public interface IBrokerManager
    {
        Task StartAsync(CancellationToken cancellationToken);
    }
}
