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
        public PollingConnectionCheckStratergy(ITcpClientManager tcpClientManager)
        {
            _tcpClientManager = tcpClientManager;
        }
        public Task MonitorAsync(CancellationToken cancellationToken)
        {
            return Task.Run(async ()=> {

                while (cancellationToken.IsCancellationRequested)
                {
                    var records = _tcpClientManager.GetStagnentClients(TimeSpan.FromMinutes(5));

                    Parallel.ForEach(records
                        , new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount}
                        ,record => {

                            var socket = record.Client.Client;

                            if (socket.Poll(1000, SelectMode.SelectRead) && socket.Available == 0)
                            {
                                _tcpClientManager.RemoveClient(record);
                            }
                        });

                    await Task.Delay(TimeSpan.FromMinutes(1)).ConfigureAwait(false);
                } 
            });
        }
    }
}
