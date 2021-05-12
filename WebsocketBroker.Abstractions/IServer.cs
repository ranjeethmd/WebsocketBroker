
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;

namespace WebsocketBroker.Abstractions
{
    public interface IServer
    {
        IAsyncEnumerable<TcpClient> StartAsync(CancellationToken cancellationToken);
    }
}
