using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using WebsocketBroker.Abstractions;
using WebsocketBroker.Abstractions.POCO;

namespace WebsocketBroker.Core.Default
{
    public class RequestHandler : IRequestHandler
    {
        private readonly ITcpClientManager _tcpClientManager;
        private readonly IFrameHandler _frameHandler;
        private readonly ILogger<RequestHandler> _logger;
        private readonly ISubscriptionManager _subscriptionManager;
        private readonly Regex _match = new Regex("GET (.*?) HTTP/1.1");

        private static Channel<PublisherRecord> PublisherStream { get; } = Channel.CreateUnbounded<PublisherRecord>();
        private static Channel<ConsumerRecord> ConsumerStream { get; } = Channel.CreateUnbounded<ConsumerRecord>();

        public RequestHandler(ITcpClientManager tcpClientManager,
            IFrameHandler frameHandler,
            ISubscriptionManager subscriptionManager,
            ILogger<RequestHandler> logger)
        {
            _tcpClientManager = tcpClientManager;
            _frameHandler = frameHandler;
            _logger = logger;
            _subscriptionManager = subscriptionManager;
        }

        // TODO: Make the functon Idempotent
        public  Task BeginPorcessAsync(CancellationToken cancellationToken)
        {
            return Task.Run(async ()=> {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var records = _tcpClientManager.GetClientsWithData();                   
                    

                    foreach(var record in records)
                    {
                        _ = ProcessContentAsync(record, cancellationToken);                      
                        
                    }
                }
            });            
        }

        public ChannelReader<PublisherRecord> GetPublisherStream()
        {
            return PublisherStream.Reader;
        }

        private async Task ProcessContentAsync(ClientRecord record,CancellationToken cancellationToken)
        {
            byte[] bytes = new byte[record.Client.Available];
            
            await record.Stream.ReadAsync(bytes, 0, record.Client.Available, cancellationToken).ConfigureAwait(false);
            _tcpClientManager.UpdateClientRecordTime(record.Client);

            var content = Encoding.UTF8.GetString(bytes);

            if (Regex.IsMatch(content, "^GET", RegexOptions.IgnoreCase))
            {
                _logger.LogInformation("=====Handshaking from client=====\n{0}", content);

                var method = _match.Match(content).Groups[1].ToString().Trim();

                if(method.StartsWith("/publish",StringComparison.InvariantCultureIgnoreCase))
                {
                    var split = method.ToUpperInvariant().Split('/');

                    if(split.Length == 2)
                    {
                        _subscriptionManager.AddPublisher(record.Client, new SubscriptionRecord(split[1],Subscription.Publisher));
                    }
                    else
                    {
                        _logger.LogError($"Unknown method {method}");
                        _tcpClientManager.RemoveClient(record.Client);
                    }
                }
                else if(method.StartsWith("/consume", StringComparison.InvariantCultureIgnoreCase))
                {
                    var split = method.ToUpperInvariant().Split('/');

                    if (split.Length == 3)
                    {
                        _subscriptionManager.AddConsumer(record.Client, new SubscriptionRecord(split[1], Subscription.Consumer), new GroupName(split[2]));
                    }
                    else
                    {
                        _logger.LogError($"Unknown method {method}");
                        _tcpClientManager.RemoveClient(record.Client);
                    }
                }
                else
                {
                    _logger.LogError($"Unknown method {method}");
                    _tcpClientManager.RemoveClient(record.Client);                    
                }               
            }

            else
            {
                var subcriptionInfo = _subscriptionManager.GetSubscription(record.Client);

                if (subcriptionInfo.Subscription == Subscription.Publisher)
                {
                    var data = _frameHandler.ReadFrame(bytes);
                    await PublisherStream.Writer.WriteAsync(new PublisherRecord(subcriptionInfo.Endpoint, data), cancellationToken).ConfigureAwait(false);
                }

                if (subcriptionInfo.Subscription == Subscription.Consumer)
                {
                    var data = _frameHandler.ReadFrame(bytes);                   
                    var group = _subscriptionManager.GetConsumerGroup(record.Client);
                    await ConsumerStream.Writer.WriteAsync(new ConsumerRecord(subcriptionInfo.Endpoint,group, data), cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    _tcpClientManager.RemoveClient(record.Client);
                }

            }           
        }

        public ChannelReader<ConsumerRecord> GetConsumerStream()
        {
            return ConsumerStream.Reader;
        }
    }
}
