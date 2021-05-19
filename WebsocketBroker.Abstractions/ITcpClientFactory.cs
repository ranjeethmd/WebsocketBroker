using System.Net.Sockets;

namespace WebsocketBroker.Abstractions
{
    public interface ITcpClientFactory
    {
        ITcpClient GetClient(TcpClient client);
    }
}
