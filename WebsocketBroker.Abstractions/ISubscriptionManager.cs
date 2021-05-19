using System.Collections.Generic;
using WebsocketBroker.Abstractions.POCO;

namespace WebsocketBroker.Abstractions
{
    public interface ISubscriptionManager
    {
        void AddSubscription(ITcpClient tcpClient, SubscriptionRecord subscription, Group group);
        GroupClients GetClientGroup(SubscriptionRecord subscription);
        SubscriptionRecord GetSubscription(ITcpClient client);
        Group GetSubscriptionGroup(ITcpClient client);
        ICollection<ITcpClient> GetGroupClients(Group group);
    }
}
