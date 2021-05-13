using System;
using System.Net.Sockets;


namespace WebsocketBroker.Abstractions.POCO
{
    public record ClientRecord(TcpClient Client, NetworkStream Stream);    
}
