using System.Threading;
using System.Threading.Tasks;

namespace WebsocketBroker.Abstractions
{
    public interface IConnectionManagement
    {
        Task MonitorAsync(CancellationToken cancellationToken);
    }
}
