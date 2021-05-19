using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using WebsocketBroker.Abstractions;
using WebsocketBroker.Abstractions.POCO;

namespace WebsocketBroker.Core.Default
{
    public class SubscriptionManager :  ISubscriptionManager
    {
        private readonly ConcurrentDictionary<ITcpClient, SubscriptionRecordGroup> _client2Subscription;

        private readonly ConcurrentDictionary<SubscriptionRecord, GroupClients> _subscriptions2Client;

        private readonly ConcurrentDictionary<ITcpClient, SynchronizedCollection<ITcpClient>> _client2Groups;
     
        private record SubscriptionRecordGroup(SubscriptionRecord Record, Group Group);
       

        public SubscriptionManager(ITcpClientManager tcpClientManager)
        {
            _client2Subscription = new ConcurrentDictionary<ITcpClient, SubscriptionRecordGroup>();
            _subscriptions2Client = new ConcurrentDictionary<SubscriptionRecord, GroupClients>();
            tcpClientManager.NotifyOnDelete(RemoveClient);
        }

        public void AddSubscription(ITcpClient client, SubscriptionRecord subscription, Group group) 
        {
            _= _client2Subscription.GetOrAdd(client, new SubscriptionRecordGroup(subscription,group));

            var groups = _subscriptions2Client.GetOrAdd(subscription, new GroupClients());

            var clients= groups.GetOrAdd(group, new SynchronizedCollection<ITcpClient>());

            clients.Add(client);

            _client2Groups.GetOrAdd(client, clients);
        }

       

        public GroupClients GetClientGroup(SubscriptionRecord subscription)
        {
            return _subscriptions2Client[subscription];
        }

        public Group GetSubscriptionGroup(ITcpClient client)
        {
            return _client2Subscription[client].Group;
        }

        public SubscriptionRecord GetSubscription(ITcpClient client)
        {
            return _client2Subscription[client].Record;
        }

        private void RemoveClient(ITcpClient tcpClient)
        {
            _client2Groups[tcpClient].Remove(tcpClient);

            _client2Subscription.Remove(tcpClient, out SubscriptionRecordGroup _ );
        }

        public ICollection<ITcpClient> GetGroupClients(Group group)
        {
            var subscriptionrGroup =  _client2Subscription.Values.Where(x => x.Group == group).First();
            return _subscriptions2Client[subscriptionrGroup.Record][group];
        }
    }
}
