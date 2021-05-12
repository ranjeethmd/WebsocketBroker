using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using WebsocketBroker.Abstractions;


namespace WebsocketBroker.Core.Default
{
    public class TcpServer : IServer
    {
        private readonly IPAddress _ip;
        private readonly int _port;
        private readonly ILogger<TcpServer> _logger;
        public TcpServer(ILogger<TcpServer> logger, IPAddress ip, int port)
        {
            _ip = ip;
            _port = port;
            _logger = logger;
        }

        // TODO: Make this function Idempotent
        public async IAsyncEnumerable<TcpClient> StartAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var server = new TcpListener(_ip, _port);
            server.Start();

            _logger.LogInformation($"Server started on ip {_ip} and port {_port}");
            while (cancellationToken.IsCancellationRequested)
            { 
                var client = await server.AcceptTcpClientAsync().ConfigureAwait(false);
                _logger.LogInformation("A client connected.");

                yield return client;
            }
        }
    }
}
