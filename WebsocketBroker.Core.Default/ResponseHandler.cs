using System;
using System.Net.Sockets;
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
        private readonly TcpClientManager _tcpClientManager;
        private readonly IFrameHandler _frameHandler;
        private readonly SemaphoreSlim _headerSlim = new SemaphoreSlim(Environment.ProcessorCount);
        private readonly SemaphoreSlim _dataSlim = new SemaphoreSlim(Environment.ProcessorCount);

        private readonly Channel<HeaderRecord> _headerStream = Channel.CreateUnbounded<HeaderRecord>();
        private readonly Channel<DataRecord> _dataStream = Channel.CreateUnbounded<DataRecord>();


        private record HeaderRecord(TcpClient Client, string Header);
        private record DataRecord(Group Group, byte[] Payload);

        public ResponseHandler(
            TcpClientManager tcpClientManager,
            IClientSelectionStratergy clientSelection,
            IFrameHandler frameHandler)
        {
            _clientSelection = clientSelection;
            _tcpClientManager = tcpClientManager;
            _frameHandler = frameHandler;
        }
        public async Task SendHeaderResponse(TcpClient client, string header, CancellationToken token)
        {

            await _headerStream.Writer.WriteAsync(new HeaderRecord(client, header), token).ConfigureAwait(false);

            _ = Task.Run(async ()=> {

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


                            await content.Client.GetStream().WriteAsync(response, 0, response.Length, token).ConfigureAwait(false);

                            _tcpClientManager.UpdateClientRecordTime(client);

                        }
                        catch(Exception ex)
                        when(ex is System.IO.IOException || ex is ObjectDisposedException || ex is InvalidOperationException)
                        {
                            _tcpClientManager.RemoveClient(content.Client);
                        }
                        finally
                        {
                            _headerSlim.Release();
                        }                   
                    
                    });
                }              
            });            
        }


        public async Task SendResponse(Group group, byte[] payload, CancellationToken token)
        {
            
            await _dataStream.Writer.WriteAsync(new DataRecord(group, payload), token).ConfigureAwait(false);

            _ = Task.Run(async () => {

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

                            await client.GetStream().WriteAsync(data, 0, data.Length, token).ConfigureAwait(false);

                            _tcpClientManager.UpdateClientRecordTime(client);



                        }
                        catch (Exception ex)
                        when (ex is System.IO.IOException || ex is ObjectDisposedException || ex is InvalidOperationException)
                        {
                            _tcpClientManager.RemoveClient(client);
                        }
                        finally
                        {
                            _headerSlim.Release();
                        }

                    });
                }
            });
        }
    }
}
