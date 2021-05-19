using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using WebsocketBroker.Abstractions;
using Group = WebsocketBroker.Abstractions.POCO.Group;

namespace WebsocketBroker.Core.Default
{
    public class ResponseHandler : IResponseHandler
    {       
       
        private readonly IClientSelectionStratergy _clientSelection;
        private readonly IFrameHandler _frameHandler;
        private readonly SemaphoreSlim _headerSlim = new SemaphoreSlim(Environment.ProcessorCount);
        private readonly SemaphoreSlim _dataSlim = new SemaphoreSlim(Environment.ProcessorCount);

        private readonly Channel<HeaderRecord> _headerStream = Channel.CreateUnbounded<HeaderRecord>();
        private readonly Channel<DataRecord> _dataStream = Channel.CreateUnbounded<DataRecord>();

        private Task _headerTask;
        private Task _dataTask;

        private record HeaderRecord(ITcpClient Client, string Header);
        private record DataRecord(Group Group, byte[] Payload);

        public ResponseHandler(            
            IClientSelectionStratergy clientSelection,
            IFrameHandler frameHandler)
        {
            _clientSelection = clientSelection;            
            _frameHandler = frameHandler;            
        }

        private async Task ProcessHeaderAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await _headerSlim.WaitAsync(token).ConfigureAwait(false);

                var content = await _headerStream.Reader.ReadAsync(token).ConfigureAwait(false);

                _ = Task.Run(async () =>
                {
                    try
                    {
                        string swk = Regex.Match(content.Header, "Sec-WebSocket-Key: (.*)").Groups[1].Value.Trim();
                        string swka = swk + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
                        var swkaSha1 = System.Security.Cryptography.SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(swka));

                        string swkaSha1Base64 = Convert.ToBase64String(swkaSha1);

                        // HTTP/1.1 defines the sequence CR LF as the end-of-line marker
                        byte[] response = Encoding.UTF8.GetBytes(
                            "HTTP/1.1 101 Switching Protocols\r\n" +
                            "Connection: Upgrade\r\n" +
                            "Upgrade: websocket\r\n" +
                            "Sec-WebSocket-Accept: " + swkaSha1Base64 + "\r\n\r\n");


                        await content.Client.SendAsync(response, token).ConfigureAwait(false);
                    }
                    finally
                    {
                        _headerSlim.Release();
                    }

                });
            }
        }

        private async Task ProcessDataAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await _dataSlim.WaitAsync(token).ConfigureAwait(false);

                var content = await _dataStream.Reader.ReadAsync(token).ConfigureAwait(false);

                _ = Task.Run(async () =>
                {
                    var client = _clientSelection.SelectClient(content.Group);

                    if (client == null) return;

                    try
                    {
                        var data = _frameHandler.CreateFrame(content.Payload);
                        await client.SendAsync(data, token).ConfigureAwait(false);
                    }
                    finally
                    {
                        _headerSlim.Release();
                    }

                });
            }
        }
        public async Task SendHeaderResponseAsync(ITcpClient client, string header, CancellationToken token)
        {
            await _headerStream.Writer.WriteAsync(new HeaderRecord(client, header), token).ConfigureAwait(false);
        }


        public async Task SendResponseAsync(Group group, byte[] payload, CancellationToken token)
        {            
            await _dataStream.Writer.WriteAsync(new DataRecord(group, payload), token).ConfigureAwait(false);
        }

        public async Task StartAsync(CancellationToken token)
        {
            _headerTask = Task.Run(async () => await ProcessHeaderAsync(token));
            _dataTask = Task.Run(async () => await ProcessDataAsync(token));

            await Task.WhenAll(_headerTask, _dataTask).ConfigureAwait(false);
        }


    }
}
