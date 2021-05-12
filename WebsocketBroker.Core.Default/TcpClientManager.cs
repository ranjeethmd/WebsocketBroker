using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using WebsocketBroker.Abstractions;
using WebsocketBroker.Abstractions.POCO;

namespace WebsocketBroker.Core.Default
{
    public class TcpClientManager : ITcpClientManager
    {
        private readonly SynchronizedCollection<ClientRecord> _clients = new SynchronizedCollection<ClientRecord>();

        public void AddClient(TcpClient client)
        {
            _clients.Add(new(client,client.GetStream(),DateTimeOffset.UtcNow));
        }

        public IEnumerable<ClientRecord> GetClientsWithData()
        {
            return _clients.AsParallel().Where(t => t.Stream.DataAvailable && t.Client.Available > 3);
        }

        public IEnumerable<ClientRecord> GetStagnentClients(TimeSpan timeSpan)
        {
            return _clients.AsParallel().Where(r => DateTimeOffset.UtcNow - r.LastAccessed >= timeSpan).ToArray();
        }

        public void RemoveClient(ClientRecord record)
        {
            record.Client.Close();
            _clients.Remove(record);
        }
    }
}
