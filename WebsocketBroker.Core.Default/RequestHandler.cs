using Microsoft.Extensions.Logging;
using System;
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

        private static Channel<EndpointRecord> PublisherStream { get; } = Channel.CreateUnbounded<EndpointRecord>();
        private static Channel<EndpointRecord> ConsumerStream { get; } = Channel.CreateUnbounded<EndpointRecord>();

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
            return Task.Run(()=> {
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

        public ChannelReader<EndpointRecord> GetPublisherStream()
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
                var split = method.ToUpperInvariant().Split('/');

                if (method.StartsWith("/publish",StringComparison.InvariantCultureIgnoreCase))
                {
                   

                    if(split.Length == 2)
                    {
                        _subscriptionManager.AddSubscription(record.Client, new SubscriptionRecord(split[1],Subscription.Publisher), new Abstractions.POCO.Group(split[2]));
                    }
                    else
                    {
                        _logger.LogError($"Unknown method {method}");
                        _tcpClientManager.RemoveClient(record.Client);
                    }
                }
                else if(method.StartsWith("/consume", StringComparison.InvariantCultureIgnoreCase))
                {                    

                    if (split.Length == 3)
                    {
                        _subscriptionManager.AddSubscription(record.Client, new SubscriptionRecord(split[1], Subscription.Consumer), new Abstractions.POCO.Group(split[2]));
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
                var data = _frameHandler.ReadFrame(bytes);
                var group = _subscriptionManager.GetSubscriptionGroup(record.Client);

                // TODO:Some one might send direct data frame on raw tcp. Currently since the subscription will not be setup it will fail. Build try catch to close connection

                if (subcriptionInfo.Subscription == Subscription.Publisher)
                {
                    await PublisherStream.Writer.WriteAsync(new EndpointRecord(subcriptionInfo.Endpoint, group, data), cancellationToken).ConfigureAwait(false);
                }

                // TODO:Some one might send direct data frame on raw tcp. Currently since the subscription will not be setup it will fail. Build try catch to close connection

                if (subcriptionInfo.Subscription == Subscription.Consumer)
                {
                    await ConsumerStream.Writer.WriteAsync(new EndpointRecord(subcriptionInfo.Endpoint, group, data), cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    _tcpClientManager.RemoveClient(record.Client);
                }

            }           
        }

        public ChannelReader<EndpointRecord> GetConsumerStream()
        {
            return ConsumerStream.Reader;
        }
    }
}
