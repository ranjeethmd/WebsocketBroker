using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using WebsocketBroker.Abstractions;

namespace WebsocketBroker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServer _server;
        private readonly ITcpClientManager _tcpClientManager;
        private readonly IConnectionManagement _connectionStratergy;
        private readonly IRequestHandler _requestHandler;
        private readonly IBrokerManager _brokerManager;



        public Worker(
            ILogger<Worker> logger,
            IServer server, 
            ITcpClientManager tcpClientManager,
            IConnectionManagement connectionStratergy,
            IRequestHandler requestHandler,
            IBrokerManager brokerManager)
        {
            _logger = logger;
            _server = server;
            _tcpClientManager = tcpClientManager;
            _connectionStratergy = connectionStratergy;
            _requestHandler = requestHandler;
            _brokerManager = brokerManager;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {          

            var monitorConnections = _connectionStratergy.MonitorAsync(stoppingToken);
            var requestProcessing = _requestHandler.BeginPorcessAsync(stoppingToken);
            var brokerTask = _brokerManager.StartAsync(stoppingToken);

            await foreach(var client in _server.StartAsync(stoppingToken))
            {
               _tcpClientManager.AddClient(client);
            }

            await Task.WhenAll(monitorConnections,requestProcessing).ConfigureAwait(false);
        }

        
    }
}
