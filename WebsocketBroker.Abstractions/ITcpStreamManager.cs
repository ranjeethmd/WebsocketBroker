namespace WebsocketBroker.Abstractions
{
    public interface ITcpStreamManager:ITcpClientManager
    {
        void AddDataClient(ITcpClient ITcpClient);
    }
}
