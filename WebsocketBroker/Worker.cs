using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
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



        public Worker(ILogger<Worker> logger,IServer server, ITcpClientManager tcpClientManager)
        {
            _logger = logger;
            _server = server;
            _tcpClientManager = tcpClientManager;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                string ip = "127.0.0.1";
                int port = 80;
                var server = new TcpListener(IPAddress.Parse(ip), port);

                server.Start();

                _logger.LogInformation($"Server started on ip {ip} and port {port}");

                TcpClient client = await server.AcceptTcpClientAsync().ConfigureAwait(false);
                _logger.LogInformation("A client connected.");


                NetworkStream stream = client.GetStream();                

                while (true)
                {
                    if (stream.DataAvailable && client.Available > 3)
                    {
                        byte[] bytes = new byte[client.Available];
                        await stream.ReadAsync(bytes, 0, client.Available,stoppingToken);

                        var content = Encoding.UTF8.GetString(bytes);

                        if (Regex.IsMatch(content, "^GET", RegexOptions.IgnoreCase))
                        {
                            _logger.LogInformation("=====Handshaking from client=====\n{0}", content);

                            // 1. Obtain the value of the "Sec-WebSocket-Key" request header without any leading or trailing whitespace
                            // 2. Concatenate it with "258EAFA5-E914-47DA-95CA-C5AB0DC85B11" (a special GUID specified by RFC 6455)
                            // 3. Compute SHA-1 and Base64 hash of the new value
                            // 4. Write the hash back as the value of "Sec-WebSocket-Accept" response header in an HTTP response
                            string swk = Regex.Match(content, "Sec-WebSocket-Key: (.*)").Groups[1].Value.Trim();
                            string swka = swk + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
                            byte[] swkaSha1 = System.Security.Cryptography.SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(swka));
                            string swkaSha1Base64 = Convert.ToBase64String(swkaSha1);

                            // HTTP/1.1 defines the sequence CR LF as the end-of-line marker
                            byte[] response = Encoding.UTF8.GetBytes(
                                "HTTP/1.1 101 Switching Protocols\r\n" +
                                "Connection: Upgrade\r\n" +
                                "Upgrade: websocket\r\n" +
                                "Sec-WebSocket-Accept: " + swkaSha1Base64 + "\r\n\r\n");

                            await stream.WriteAsync(response, 0, response.Length);
                        }
                        else
                        {
                            bool fin = (bytes[0] & 0b10000000) != 0; //checks whether message is send in frames.
                            bool mask = (bytes[1] & 0b10000000) != 0; // must be true, "All messages from the client to the server have this bit set"

                            int opcode = bytes[0] & 0b00001111; // expecting 1 - text message
                            int msglen = bytes[1] - 128; // & 0111 1111
                            int offset = 2;


                            if (msglen == 126)
                            {
                                // was ToUInt16(bytes, offset) but the result is incorrect
                                msglen = BitConverter.ToUInt16(new byte[] { bytes[3], bytes[2] }, 0);
                                offset = 4;
                            }
                            else if (msglen == 127)
                            {
                                _logger.LogError("TODO: msglen == 127, needs qword to store msglen");
                                // i don't really know the byte order, please edit this
                                // msglen = BitConverter.ToUInt64(new byte[] { bytes[5], bytes[4], bytes[3], bytes[2], bytes[9], bytes[8], bytes[7], bytes[6] }, 0);
                                // offset = 10;
                            }

                            else if (mask)
                            {
                                byte[] decoded = new byte[msglen];
                                byte[] masks = new byte[4] { bytes[offset], bytes[offset + 1], bytes[offset + 2], bytes[offset + 3] };
                                offset += 4;

                                for (int i = 0; i < msglen; ++i)
                                    decoded[i] = (byte)(bytes[offset + i] ^ masks[i % 4]);

                                string text = Encoding.UTF8.GetString(decoded);
                                _logger.LogInformation("{0}", text);

                                byte[] response = Encoding.UTF8.GetBytes("This works");


                                await Write(stream, 2, response, stoppingToken, true);

                            }
                            else
                                _logger.LogError("mask bit not set");
                        }
                    }
                }

            }

           await foreach(var client in _server.StartAsync(stoppingToken))
           {
               _tcpClientManager.AddClient(client);
           }
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
