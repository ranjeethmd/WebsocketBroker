using System;
using System.Threading.Channels;

namespace WebsocketBroker.Abstractions
{
    public interface ITcpClientManager
    {
        void AddClient(ITcpClient client);

        void RemoveClient(ITcpClient client);

        void UpdateClientRecordTime(ITcpClient record);

        DateTimeOffset GetLastActivityDate(ITcpClient client);

        void NotifyOnDelete(Action<ITcpClient> action);

        ChannelReader<ITcpClient> GetClientStream();
    }
}
