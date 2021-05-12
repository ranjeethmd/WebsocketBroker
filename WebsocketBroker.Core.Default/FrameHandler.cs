using Microsoft.Extensions.Logging;
using System;
using System.Text;
using System.Text.RegularExpressions;
using WebsocketBroker.Abstractions;
using WebsocketBroker.Abstractions.Exceptions;

namespace WebsocketBroker.Core.Default
{
    public class FrameHandler : IFrameHandler
    {
        private readonly ILogger<FrameHandler> _logger;
        public FrameHandler(ILogger<FrameHandler> logger)
        {
            _logger = logger;
        }
        public byte[] CreateFrame(byte[] data)
        {
            throw new NotImplementedException();
        }

        public byte[] ReadFrame(byte[] frameData, out bool isHandShake)
        {
            var content = Encoding.UTF8.GetString(frameData);

            if (Regex.IsMatch(content, "^GET", RegexOptions.IgnoreCase))
            {
                _logger.LogInformation("=====Handshaking from client=====\n{0}", content);
                
                string swk = Regex.Match(content, "Sec-WebSocket-Key: (.*)").Groups[1].Value.Trim();
                string swka = swk + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
                isHandShake = true;
                return System.Security.Cryptography.SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(swka));   
            }
            else
            {
                isHandShake = false;

                bool fin = (frameData[0] & 0b10000000) != 0; //checks whether message is send in frames.
                bool mask = (frameData[1] & 0b10000000) != 0; // must be true, "All messages from the client to the server have this bit set"

                int opcode = frameData[0] & 0b00001111; // expecting 1 - text message
                int msglen = frameData[1] - 128; // & 0111 1111
                int offset = 2;

                if (msglen == 126)
                {
                    // was ToUInt16(frameData, offset) but the result is incorrect
                    msglen = BitConverter.ToUInt16(new byte[] { frameData[3], frameData[2] }, 0);
                    offset = 4;
                }
                else if (msglen == 127)
                {
                    throw new FrameException("TODO: msglen == 127, needs qword to store msglen");                    
                }

                if (msglen == 0)
                {
                    throw new FrameException("mask bit not set");
                }
                else 
                {
                    byte[] decoded = new byte[msglen];
                    byte[] masks = new byte[4] { frameData[offset], frameData[offset + 1], frameData[offset + 2], frameData[offset + 3] };
                    offset += 4;

                    for (int i = 0; i < msglen; ++i)
                        decoded[i] = (byte)(frameData[offset + i] ^ masks[i % 4]);

                    return decoded;  

                }
            }
        }
    }
}
