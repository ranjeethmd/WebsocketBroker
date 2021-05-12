using Microsoft.Extensions.Logging;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using WebsocketBroker.Abstractions;

namespace WebsocketBroker.Core.Default
{
    public class PollingConnectionCheckStratergy : IConnectionManagement
    {
        private readonly ITcpClientManager _tcpClientManager;
        private readonly ILogger<PollingConnectionCheckStratergy> _logger;
        public PollingConnectionCheckStratergy(ITcpClientManager tcpClientManager,ILogger<PollingConnectionCheckStratergy> logger)
        {
            _tcpClientManager = tcpClientManager;
        }

        // TODO: Make this function idempotent
        public Task MonitorAsync(CancellationToken cancellationToken)
        {
            return Task.Run(async ()=> {

                while (!cancellationToken.IsCancellationRequested)
                {
                    var records = _tcpClientManager.GetStagnentClients(TimeSpan.FromMinutes(5));

                    Parallel.ForEach(records
                        , new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount}
                        ,record => {

                            var socket = record.Client.Client;

                            try
                            {
                                if (socket.Poll(1000, SelectMode.SelectRead) && socket.Available == 0)
                                {
                                    _tcpClientManager.RemoveClient(record);
                                }
                            }
                            catch(Exception ex)
                            {
                                _logger.LogError(ex, "Error while cleaning socket.");
                            }
                        });

                    await Task.Delay(TimeSpan.FromMinutes(1)).ConfigureAwait(false);
                } 
            });
        }
    }
}
