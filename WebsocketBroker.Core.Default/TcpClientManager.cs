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
        private readonly List<Action<TcpClient>> _notificationList = new List<Action<TcpClient>>();

        public void AddClient(TcpClient client)
        {
            
            _clients.GetOrAdd(client, DateTimeOffset.UtcNow);
        }

        public void UpdateClientRecordTime(TcpClient client)
        {
            if (_clients.TryGetValue(client, out DateTimeOffset current))
            {
                _clients.TryUpdate(client, DateTimeOffset.UtcNow,current);
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

        public IEnumerable<TcpClient> GetStagnentClients(TimeSpan timeSpan)
        {
            return _clients.AsParallel().Where(kv => DateTimeOffset.UtcNow - kv.Value >= timeSpan).Select(kv => kv.Key).ToArray();
        }

        public void RemoveClient(TcpClient client)
        {
            client.Close();
            _clients.Remove(client, out _);
            _notificationList.ForEach(action => {
                _ = Task.Run(() => action(client));                           
            });
        }

        public void NotifyOnDelete(Action<TcpClient> action)
        {
            _notificationList.Add(action);
        }
    }
}
