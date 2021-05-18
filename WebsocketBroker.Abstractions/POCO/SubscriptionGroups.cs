using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;

namespace WebsocketBroker.Abstractions.POCO
{

    /// <summary>
    /// Holds mapping between group and clients
    /// </summary>
    public class ClientGroup: ConcurrentDictionary<Group, SynchronizedCollection<TcpClient>>
    {
    }
}
