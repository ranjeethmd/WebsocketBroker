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

        void RemoveClient(ClientRecord client);

        void UpdateClientRecordTime(ClientRecord record);

        IEnumerable<ClientRecord> GetStagnentClients(TimeSpan timeSpan);
    }
}
