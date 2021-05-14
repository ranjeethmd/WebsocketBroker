using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using WebsocketBroker.Abstractions;
using WebsocketBroker.Abstractions.Exceptions;

namespace WebsocketBroker.Core.Default
{
    public class FrameHandler : IFrameHandler
    {
        public byte[] CreateFrame(byte[] payload)
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
                return memoryStream.ToArray();               
            }
        }

        public byte[] ReadFrame(byte[] frameData)
        {
            bool fin = (frameData[0] & 0b10000000) != 0; //checks whether message is send in frames.
            bool mask = (frameData[1] & 0b10000000) != 0; // must be true, "All messages from the client to the server have this bit set"

            int opcode = frameData[0] & 0b00001111; // expecting 1 - text message
            ulong msglen = (ulong)frameData[1] - 128; // & 0111 1111
            ulong offset = 2;

            if (msglen == 126)
            {
                // was ToUInt16(frameData, offset) but the result is incorrect
                msglen = BitConverter.ToUInt16(new byte[] { frameData[3], frameData[2] }, 0);
                offset = 4;
            }
            else if (msglen == 127)
            {
                msglen = BitConverter.ToUInt64(new byte[] { frameData[9], frameData[8], frameData[7], frameData[6], frameData[5], frameData[4], frameData[3], frameData[2] }, 0);
                offset = 10;
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

                for (ulong i = 0; i < msglen; ++i)
                    decoded[i] = (byte)(frameData[offset + i] ^ masks[i % 4]);

                return decoded;
            }
        }
    }
}
