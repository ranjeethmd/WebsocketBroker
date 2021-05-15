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
        private readonly ConcurrentDictionary<SubscriptionRecord, SubscriptionGroups> _subscriptions2Client;
        private record SubscriptionRecordGroup(SubscriptionRecord Record, GroupName Group = null);
       

        public SubscriptionManager(ITcpClientManager tcpClientManager)
        {
            _client2Subscription = new ConcurrentDictionary<TcpClient, SubscriptionRecordGroup>();
            _subscriptions2Client = new ConcurrentDictionary<SubscriptionRecord, SubscriptionGroups>();
            tcpClientManager.NotifyOnDelete(RemoveClient);
        }

        public void AddConsumer(TcpClient tcpClient, SubscriptionRecord subscription, GroupName group) 
        {
            _= _client2Subscription.GetOrAdd(tcpClient, new SubscriptionRecordGroup(subscription,group));

            var groups = _subscriptions2Client.GetOrAdd(subscription, new SubscriptionGroups());

            var clients= groups.GetOrAdd(group, new SynchronizedCollection<TcpClient>());

            clients.Add(tcpClient);
        }

        public void AddPublisher(TcpClient tcpClient, SubscriptionRecord subscription)
        {
            _ = _client2Subscription.GetOrAdd(tcpClient, new SubscriptionRecordGroup(subscription));
        }

        public SubscriptionGroups GetConsumers(SubscriptionRecord subscription)
        {
            return _subscriptions2Client[subscription];
        }

        public GroupName GetConsumerGroup(TcpClient client)
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
    }
}
