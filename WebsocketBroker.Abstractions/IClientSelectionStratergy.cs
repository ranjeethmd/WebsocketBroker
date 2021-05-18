using System.Net.Sockets;
using WebsocketBroker.Abstractions.POCO;

namespace WebsocketBroker.Abstractions
{
    public interface IClientSelectionStratergy
    {
        TcpClient SelectClient(Group group);
    }
}
