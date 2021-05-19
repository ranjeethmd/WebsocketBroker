using System.Collections.Concurrent;
using System.Collections.Generic;

namespace WebsocketBroker.Abstractions.POCO
{

    /// <summary>
    /// Holds mapping between group and clients
    /// </summary>
    public class GroupClients: ConcurrentDictionary<Group, SynchronizedCollection<ITcpClient>>
    {
    }
}
