using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using WebsocketBroker.Abstractions;

namespace WebsocketBroker.Core.Default
{
    public class SubscriptionClient:ITcpMonitoredClient
    {
        private readonly TcpClient _client;
        private readonly ITcpStreamManager _tcpStreamMaanager;
        private Task _activityTask;
        private Task _dataTask;
       
        public SubscriptionClient(TcpClient client, ITcpStreamManager tcpStreamManager)
        {
            _client = client;
            _tcpStreamMaanager = tcpStreamManager;
        }

        public void Disconnect()
        {
            _client.Close();
            _tcpStreamMaanager.RemoveClient(this);
        }

        public async Task<byte[]> GetDataAync(CancellationToken cancellationToken)
        {
            var stream = _client.GetStream();

            //Set 30 sec timeout for data read;
            stream.ReadTimeout = 30000;

            var data = new byte[_client.Available];
            await stream.ReadAsync(data, 0, _client.Available, cancellationToken).ConfigureAwait(false);

            return data;
        }

        public bool IsDataAvailable()
        {
            var stream = _client.GetStream();

            if (stream.DataAvailable && _client.Available > 3)
            {
                return true;

            }

            return false;
        }

        public void MonitorActivity(CancellationToken token)
        {
            if (_activityTask != null) return;

            _activityTask = Task.Run(async () => {

                while (!token.IsCancellationRequested)
                {
                    var date = _tcpStreamMaanager.GetLastActivityDate(this);
                    var socket = _client.Client;

                    if (socket.Poll(1000, SelectMode.SelectRead) && socket.Available == 0)
                    {
                        _tcpStreamMaanager.RemoveClient(this);
                    }

                    await Task.Delay(TimeSpan.FromMinutes(5)).ConfigureAwait(false);
                }
            });
        }

        public void MonitorForData(CancellationToken token)
        {
            if (_dataTask != null) return;

            _dataTask = Task.Run(async () =>{
                while (!token.IsCancellationRequested)
                {
                    if(!IsDataAvailable())
                    {
                        await Task.Delay(1000).ConfigureAwait(false);
                        continue;
                    }

                    try
                    {

                         var data = await GetDataAync(token).ConfigureAwait(false);

                        _tcpStreamMaanager.AddDataClient(this);

                        _tcpStreamMaanager.UpdateClientRecordTime(this);

                    }
                    catch (Exception ex)
                    when (ex is System.IO.IOException || ex is ObjectDisposedException || ex is InvalidOperationException)
                    {
                        _tcpStreamMaanager.RemoveClient(this);


                    }
                }
            });
        }

        public async Task<bool> SendAsync(byte[] data, CancellationToken cancellationToken)
        {
            try
            {
                await _client.GetStream().WriteAsync(data, 0, data.Length, cancellationToken).ConfigureAwait(false);

                _tcpStreamMaanager.UpdateClientRecordTime(this);

                return true;
            }
            catch (Exception ex)
            when (ex is System.IO.IOException || ex is ObjectDisposedException || ex is InvalidOperationException)
            {
                _tcpStreamMaanager.RemoveClient(this);

                return false;
            }
        }
    }
}
