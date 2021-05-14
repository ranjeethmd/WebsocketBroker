using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WebsocketBroker.Abstractions;
using WebsocketBroker.Abstractions.POCO;

namespace WebsocketBroker.Core.Default
{
    public class ResponseHandler : IResponseHandler
    {
        private readonly ITcpClientManager _tcpClientManager;

        public ResponseHandler(ITcpClientManager tcpClientManager)
        {
            _tcpClientManager = tcpClientManager;
        }
        public async Task SendHeaderResponse(ClientRecord record, string header, CancellationToken token)
        {
            string swk = Regex.Match(header, "Sec-WebSocket-Key: (.*)").Groups[1].Value.Trim();
            string swka = swk + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
            var swkaSha1 = System.Security.Cryptography.SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(swka));

            string swkaSha1Base64 = Convert.ToBase64String(swkaSha1);

            // HTTP/1.1 defines the sequence CR LF as the end-of-line marker
            byte[] response = Encoding.UTF8.GetBytes(
                "HTTP/1.1 101 Switching Protocols\r\n" +
                "Connection: Upgrade\r\n" +
                "Upgrade: websocket\r\n" +
                "Sec-WebSocket-Accept: " + swkaSha1Base64 + "\r\n\r\n");

            await record.Stream.WriteAsync(response, 0, response.Length,token).ConfigureAwait(false);

            _tcpClientManager.UpdateClientRecordTime(record);
        }

        public async Task SendResponse(ClientRecord record, byte[] payload, CancellationToken token)
        {
            await record.Stream.WriteAsync(payload, 0, payload.Length, token).ConfigureAwait(false);

            _tcpClientManager.UpdateClientRecordTime(record);
        }
    }
}
