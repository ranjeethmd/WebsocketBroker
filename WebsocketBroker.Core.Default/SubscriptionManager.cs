using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using WebsocketBroker.Abstractions;
using WebsocketBroker.Abstractions.POCO;

namespace WebsocketBroker.Core.Default
{
    public class SubscriptionManager :  ISubscriptionManager
    {
        private readonly ConcurrentDictionary<TcpClient, SubscriptionRecordGroup> _client2Subscription;

        private readonly ConcurrentDictionary<SubscriptionRecord, ClientGroup> _subscriptions2Client;
     
        private record SubscriptionRecordGroup(SubscriptionRecord Record, Group Group);
       

        public SubscriptionManager(ITcpClientManager tcpClientManager)
        {
            _client2Subscription = new ConcurrentDictionary<TcpClient, SubscriptionRecordGroup>();
            _subscriptions2Client = new ConcurrentDictionary<SubscriptionRecord, ClientGroup>();
            tcpClientManager.NotifyOnDelete(RemoveClient);
        }

        public void AddSubscription(TcpClient tcpClient, SubscriptionRecord subscription, Group group) 
        {
            _= _client2Subscription.GetOrAdd(tcpClient, new SubscriptionRecordGroup(subscription,group));

            var groups = _subscriptions2Client.GetOrAdd(subscription, new ClientGroup());

            var clients= groups.GetOrAdd(group, new SynchronizedCollection<TcpClient>());

            clients.Add(tcpClient);
        }

       

        public ClientGroup GetClientGroup(SubscriptionRecord subscription)
        {
            return _subscriptions2Client[subscription];
        }

        public Group GetSubscriptionGroup(TcpClient client)
        {
            return _client2Subscription[client].Group;
        }

        public SubscriptionRecord GetSubscription(TcpClient client)
        {
            return _client2Subscription[client].Record;
        }

        private void RemoveClient(TcpClient tcpClient)
        {
            _subscriptions2Client.AsParallel().SelectMany(kv => kv.Value)
                .Select(kv => kv.Value).Where(clients => clients.Contains(tcpClient))
                .ForAll(clients => clients.Remove(tcpClient));

            _client2Subscription.Remove(tcpClient, out SubscriptionRecordGroup _ );
        }

        public ICollection<TcpClient> GetGroupClients(Group group)
        {
            throw new System.NotImplementedException();
        }
    }
}
