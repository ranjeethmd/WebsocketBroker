using System;
using System.IO;
using System.Text;
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
        public async Task SendHeaderResponse(ClientRecord record, byte[] swkaSha1, CancellationToken token)
        {
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
            using (MemoryStream memoryStream = new MemoryStream())
            {
                byte finBitSetAsByte = 0x80; //Final Frame
                byte byte1 = (byte)(finBitSetAsByte | 2); //Binary Data
                memoryStream.WriteByte(byte1);

                
                byte maskBitSetAsByte = (byte)0x00; //Server Mask

                if (payload.Length < 126)
                {
                    byte byte2 = (byte)(maskBitSetAsByte | (byte)payload.Length);
                    memoryStream.WriteByte(byte2);
                }
                else if (payload.Length <= ushort.MaxValue)
                {
                    byte byte2 = (byte)(maskBitSetAsByte | 126);
                    memoryStream.WriteByte(byte2);
                    byte[] data = BitConverter.GetBytes((ushort)payload.Length);
                   

                    memoryStream.Write(data, 0, data.Length);
                }
                else
                {
                    byte byte2 = (byte)(maskBitSetAsByte | 127);
                    memoryStream.WriteByte(byte2);
                    byte[] data = BitConverter.GetBytes((ulong)payload.Length);
                    memoryStream.Write(data, 0, data.Length);
                }

                memoryStream.Write(payload, 0, payload.Length);
                byte[] buffer = memoryStream.ToArray();
                await  record.Stream.WriteAsync(buffer, 0, buffer.Length, token).ConfigureAwait(false);

                _tcpClientManager.UpdateClientRecordTime(record);
            }
        }
    }
}
