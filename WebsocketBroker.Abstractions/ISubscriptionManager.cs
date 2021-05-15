using System.Net.Sockets;
using WebsocketBroker.Abstractions.POCO;

namespace WebsocketBroker.Abstractions
{
    public interface ISubscriptionManager
    {
        void AddPublisher(TcpClient tcpClient, SubscriptionRecord subscription);
        void AddConsumer(TcpClient tcpClient, SubscriptionRecord subscription, GroupName group);
        SubscriptionGroups GetConsumers(SubscriptionRecord subscription);
        SubscriptionRecord GetSubscription(TcpClient client);
        GroupName GetConsumerGroup(TcpClient client);
    }
}
