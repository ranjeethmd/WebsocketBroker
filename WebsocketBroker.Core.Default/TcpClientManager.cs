using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using WebsocketBroker.Abstractions;
using WebsocketBroker.Abstractions.POCO;

namespace WebsocketBroker.Core.Default
{
    public class TcpClientManager : ITcpClientManager
    {
        private readonly ConcurrentDictionary<TcpClient,DateTimeOffset> _clients = new ConcurrentDictionary<TcpClient, DateTimeOffset>();

        public void AddClient(TcpClient client)
        {
            
            _clients.GetOrAdd(client, DateTimeOffset.UtcNow);
        }

        public void UpdateClientRecordTime(ClientRecord record)
        {
            if (_clients.TryGetValue(record.Client, out DateTimeOffset current))
            {
                _clients.TryUpdate(record.Client, DateTimeOffset.UtcNow,current);
            }
        }

        public IEnumerable<ClientRecord> GetClientsWithData()
        {
            var toBeProcessed = new ConcurrentBag<ClientRecord>();

            Parallel.ForEach(_clients.Keys, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, client => {
                
                var stream = client.GetStream();

                if (stream.DataAvailable && client.Available > 3)
                {
                    toBeProcessed.Add(new ClientRecord(client, stream));
                }
            });

            return toBeProcessed;
        }

        public IEnumerable<ClientRecord> GetStagnentClients(TimeSpan timeSpan)
        {
            return _clients.AsParallel().Where(kv => DateTimeOffset.UtcNow - kv.Value >= timeSpan).Select(kv => new ClientRecord(kv.Key, kv.Key.GetStream())).ToArray();
        }

        public void RemoveClient(ClientRecord record)
        {
            record.Client.Close();

            _clients.Remove(record.Client, out DateTimeOffset _);
        }
    }
}
