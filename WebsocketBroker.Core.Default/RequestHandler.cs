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
        private readonly ITcpClientManager _tcpManager;
        private readonly IFrameHandler _frameHandler;
        private readonly ILogger<RequestHandler> _logger;
        private readonly ISubscriptionManager _subscriptionManager;
        private readonly Regex _match = new Regex("GET (.*?) HTTP/1.1");
        private readonly SemaphoreSlim _requestSlim = new SemaphoreSlim(Environment.ProcessorCount);


        private static Channel<EndpointRecord> PublisherStream { get; } = Channel.CreateUnbounded<EndpointRecord>();
        private static Channel<EndpointRecord> ConsumerStream { get; } = Channel.CreateUnbounded<EndpointRecord>();

        public RequestHandler(ITcpClientManager tcpManager,
            IFrameHandler frameHandler,
            ISubscriptionManager subscriptionManager,
            ILogger<RequestHandler> logger)
        {
            _tcpManager = tcpManager;
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
                    var reader = _tcpManager.GetClientStream();
                    var client = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);

                    await _requestSlim.WaitAsync().ConfigureAwait(false);

                    _ = Task.Run(async () => {
                        await ProcessContentAsync(client, cancellationToken).ConfigureAwait(false);
                    });                    
                    
                }
            });            
        }

        public ChannelReader<EndpointRecord> GetPublisherStream()
        {
            return PublisherStream.Reader;
        }

        private async Task ProcessContentAsync(ITcpClient client,CancellationToken cancellationToken)
        {
            var bytes = await client.GetDataAync(cancellationToken).ConfigureAwait(false);

            var content = Encoding.UTF8.GetString(bytes);

            if (Regex.IsMatch(content, "^GET", RegexOptions.IgnoreCase))
            {
                _logger.LogInformation("=====Handshaking from client=====\n{0}", content);

                var method = _match.Match(content).Groups[1].ToString().Trim();
                var split = method.ToUpperInvariant().Split('/');

                if (method.StartsWith("/publish",StringComparison.InvariantCultureIgnoreCase))
                {
                   

                    if(split.Length == 3)
                    {
                        _subscriptionManager.AddSubscription(client, new SubscriptionRecord(split[1],Subscription.Publisher), new Abstractions.POCO.Group(split[2]));
                    }
                    else
                    {
                        _logger.LogError($"Unknown method {method}");
                        client.Disconnect();
                    }
                }
                else if(method.StartsWith("/consume", StringComparison.InvariantCultureIgnoreCase))
                {                    

                    if (split.Length == 3)
                    {
                        _subscriptionManager.AddSubscription(client, new SubscriptionRecord(split[1], Subscription.Consumer), new Abstractions.POCO.Group(split[2]));
                    }
                    else
                    {
                        _logger.LogError($"Unknown method {method}");
                        client.Disconnect();
                    }
                }
                else
                {
                    _logger.LogError($"Unknown method {method}");
                    client.Disconnect();                   
                }               
            }

            else
            {
                var subcriptionInfo = _subscriptionManager.GetSubscription(client);

                if(subcriptionInfo == null)
                {
                    client.Disconnect();
                    return;
                }

                var data = _frameHandler.ReadFrame(bytes);
                var group = _subscriptionManager.GetSubscriptionGroup(client);

                

                if (subcriptionInfo.Subscription == Subscription.Publisher)
                {
                    await PublisherStream.Writer.WriteAsync(new EndpointRecord(subcriptionInfo.Endpoint, group, data), cancellationToken).ConfigureAwait(false);
                }                

                if (subcriptionInfo.Subscription == Subscription.Consumer)
                {
                    await ConsumerStream.Writer.WriteAsync(new EndpointRecord(subcriptionInfo.Endpoint, group, data), cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    client.Disconnect();
                }

            }           
        }

        public ChannelReader<EndpointRecord> GetConsumerStream()
        {
            return ConsumerStream.Reader;
        }
    }
}
