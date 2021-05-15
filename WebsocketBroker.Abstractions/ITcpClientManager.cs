using System;
using System.Collections.Generic;
using System.Net.Sockets;
using WebsocketBroker.Abstractions.POCO;

namespace WebsocketBroker.Abstractions
{
    public interface ITcpClientManager
    {
        void AddClient(TcpClient client);

        IEnumerable<ClientRecord> GetClientsWithData();

        void RemoveClient(TcpClient client);

        void UpdateClientRecordTime(TcpClient record);

        IEnumerable<TcpClient> GetStagnentClients(TimeSpan timeSpan);

        void NotifyOnDelete(Action<TcpClient> action);
    }
}
