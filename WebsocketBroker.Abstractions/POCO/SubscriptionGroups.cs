using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace WebsocketBroker.Abstractions.POCO
{
    public class SubscriptionGroups: ConcurrentDictionary<GroupName, SynchronizedCollection<TcpClient>>
    {
    }
}
