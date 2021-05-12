using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
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



        public Worker(ILogger<Worker> logger,IServer server, ITcpClientManager tcpClientManager,IConnectionManagement connectionStratergy)
        {
            _logger = logger;
            _server = server;
            _tcpClientManager = tcpClientManager;
            _connectionStratergy = connectionStratergy;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {          

            var monitorConnections = _connectionStratergy.MonitorAsync(stoppingToken);

            await foreach(var client in _server.StartAsync(stoppingToken))
            {
               _tcpClientManager.AddClient(client);
            }

            await Task.WhenAll(monitorConnections).ConfigureAwait(false);
        }

        public async Task Write(Stream stream,int opCode, byte[] payload, CancellationToken stoppingToken, bool isLastFrame)
        {
            var _isClient = false;
            // best to write everything to a memory stream before we push it onto the wire
            // not really necessary but I like it this way
            using (MemoryStream memoryStream = new MemoryStream())
            {
                byte finBitSetAsByte = isLastFrame ? (byte)0x80 : (byte)0x00;
                byte byte1 = (byte)(finBitSetAsByte | (byte)opCode);
                memoryStream.WriteByte(byte1);

                // NB, set the mask flag if we are constructing a client frame
                byte maskBitSetAsByte = _isClient ? (byte)0x80 : (byte)0x00;

                // depending on the size of the length we want to write it as a byte, ushort or ulong
                if (payload.Length < 126)
                {
                    byte byte2 = (byte)(maskBitSetAsByte | (byte)payload.Length);
                    memoryStream.WriteByte(byte2);
                }
                else if (payload.Length <= ushort.MaxValue)
                {
                    byte byte2 = (byte)(maskBitSetAsByte | 126);
                    memoryStream.WriteByte(byte2);
                    //BinaryReaderWriter.WriteUShort((ushort)payload.Length, memoryStream, false);
                    byte[] data = BitConverter.GetBytes((ulong)payload.Length);
                    memoryStream.Write(data, 0, data.Length);
                }
                else
                {
                    byte byte2 = (byte)(maskBitSetAsByte | 127);
                    memoryStream.WriteByte(byte2);
                    byte[] data = BitConverter.GetBytes((ulong)payload.Length);
                    memoryStream.Write(data, 0, data.Length);
                }

                // if we are creating a client frame then we MUST mack the payload as per the spec
                //if (_isClient)
                //{
                //    byte[] maskKey = new byte[WebSocketFrameCommon.MaskKeyLength];
                //    _random.NextBytes(maskKey);
                //    memoryStream.Write(maskKey, 0, maskKey.Length);

                //    // mask the payload
                //    WebSocketFrameCommon.ToggleMask(maskKey, payload);
                //}

                memoryStream.Write(payload, 0, payload.Length);
                byte[] buffer = memoryStream.ToArray();
                await stream.WriteAsync(buffer, 0, buffer.Length,stoppingToken);
            }
        }
    }
}
