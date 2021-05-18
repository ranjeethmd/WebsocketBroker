using System.Collections.Generic;
using System.Net.Sockets;
using WebsocketBroker.Abstractions.POCO;

namespace WebsocketBroker.Abstractions
{
    public interface ISubscriptionManager
    {
        void AddSubscription(TcpClient tcpClient, SubscriptionRecord subscription, Group group);
        ClientGroup GetClientGroup(SubscriptionRecord subscription);
        SubscriptionRecord GetSubscription(TcpClient client);
        Group GetSubscriptionGroup(TcpClient client);
        ICollection<TcpClient> GetGroupClients(Group group);
    }
}
