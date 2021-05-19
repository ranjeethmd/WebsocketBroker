using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using WebsocketBroker.Abstractions;

namespace WebsocketBroker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServer _server;
        private readonly ITcpStreamManager _tcpClientManager;
        private readonly IRequestHandler _requestHandler;
        private readonly IResponseHandler _responseHandler;
        private readonly IBrokerManager _brokerManager;
        private readonly ITcpClientFactory _tcpClientFactory;



        public Worker(
            ILogger<Worker> logger,
            IServer server, 
            ITcpStreamManager tcpClientManager,
            ITcpClientFactory tcpClientFactory,
            IRequestHandler requestHandler,
            IResponseHandler responseHandler,
            IBrokerManager brokerManager)
        {
            _logger = logger;
            _server = server;
            _tcpClientManager = tcpClientManager;
            _requestHandler = requestHandler;
            _responseHandler = responseHandler;
            _brokerManager = brokerManager;
            _tcpClientFactory = tcpClientFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                var requestProcessing = _requestHandler.BeginPorcessAsync(stoppingToken);
                var responseHandler = _requestHandler.BeginPorcessAsync(stoppingToken);
                var brokerTask = _brokerManager.StartAsync(stoppingToken);

                await foreach (var client in _server.StartAsync(stoppingToken))
                {
                    _tcpClientManager.AddClient(_tcpClientFactory.GetClient(client));
                }

                await Task.WhenAll(requestProcessing, brokerTask).ConfigureAwait(false);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"Server failed with error.");
            }
        }

        
    }
}
