using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;

namespace WebsocketBroker.Abstractions.POCO
{
    public class SubscriptionGroups: ConcurrentDictionary<GroupName, SynchronizedCollection<TcpClient>>
    {
    }
}
