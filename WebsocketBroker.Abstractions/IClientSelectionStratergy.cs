using WebsocketBroker.Abstractions.POCO;

namespace WebsocketBroker.Abstractions
{
    public interface IClientSelectionStratergy
    {
        ITcpClient SelectClient(Group group);
    }
}
