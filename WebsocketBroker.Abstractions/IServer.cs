
using System.Collections.Generic;
using System.Threading;
using System.Net.Sockets;

namespace WebsocketBroker.Abstractions
{
    public interface IServer
    {
        IAsyncEnumerable<TcpClient> StartAsync(CancellationToken cancellationToken);
    }
}
