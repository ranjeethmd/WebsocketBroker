using System;
using System.Net.Sockets;
using WebsocketBroker.Abstractions;

namespace WebsocketBroker.Core.Default
{
    public class TcpClientFactory : ITcpClientFactory
    {
        private readonly IServiceProvider _provider;

        public TcpClientFactory(IServiceProvider provider)
        {
            _provider = provider;
        }

        public ITcpClient GetClient(TcpClient client)
        {
            var streamManager =  (ITcpStreamManager) _provider.GetService(typeof(ITcpStreamManager));
            return new SubscriptionClient(client, streamManager);
        }
    }
}
