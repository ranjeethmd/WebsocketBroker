﻿using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using WebsocketBroker.Abstractions.POCO;

namespace WebsocketBroker.Abstractions
{
    public interface IRequestHandler
    {
        Task BeginPorcessAsync(CancellationToken cancellationToken);
        ChannelReader<EndpointRecord> GetPublisherStream();

        ChannelReader<EndpointRecord> GetConsumerStream();
    }
}
