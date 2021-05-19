using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;
using WebsocketBroker.Abstractions;


namespace WebsocketBroker.Core.Default
{
    public class TcpClientManager : ITcpStreamManager
    {
        private readonly ConcurrentDictionary<ITcpClient,DateTimeOffset> _clients = new ConcurrentDictionary<ITcpClient, DateTimeOffset>();
        private readonly List<Action<ITcpClient>> _notificationList = new List<Action<ITcpClient>>();
        private readonly Channel<ITcpClient> _dataClientStream = Channel.CreateUnbounded<ITcpClient>();

        public void AddClient(ITcpClient client)
        {
            
            _clients.GetOrAdd(client, DateTimeOffset.UtcNow);
        }

        public void UpdateClientRecordTime(ITcpClient client)
        {
            if (_clients.TryGetValue(client, out DateTimeOffset current))
            {
                _clients.TryUpdate(client, DateTimeOffset.UtcNow,current);
            }
        }

       

        public void RemoveClient(ITcpClient client)
        {
            _clients.Remove(client, out _);
            _notificationList.ForEach(action => {
                _ = Task.Run(() => action(client));                           
            });
        }

        public void NotifyOnDelete(Action<ITcpClient> action)
        {
            _notificationList.Add(action);
        }

        public ChannelReader<ITcpClient> GetClientStream()
        {
            return _dataClientStream.Reader;
        }

        void ITcpStreamManager.AddDataClient(ITcpClient tcpClient)
        {
           _ = _dataClientStream.Writer.WriteAsync(tcpClient);
        }

        public DateTimeOffset GetLastActivityDate(ITcpClient client)
        {
            return _clients[client];
        }
    }
}
