using System.Threading;
using System.Threading.Tasks;
using WebsocketBroker.Abstractions.POCO;

namespace WebsocketBroker.Abstractions
{
    public interface IResponseHandler
    {
        Task StartAsync(CancellationToken token);
        Task SendHeaderResponseAsync(ITcpClient ITcpClient, string header, CancellationToken token);

        Task SendResponseAsync(Group group, byte[] data,CancellationToken token);
    }
}
