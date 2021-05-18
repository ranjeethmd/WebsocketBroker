using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using WebsocketBroker.Abstractions.POCO;

namespace WebsocketBroker.Abstractions
{
    public interface IResponseHandler
    {
        Task SendHeaderResponse(TcpClient tcpClient, string header, CancellationToken token);

        Task SendResponse(Group group, byte[] data,CancellationToken token);
    }
}
