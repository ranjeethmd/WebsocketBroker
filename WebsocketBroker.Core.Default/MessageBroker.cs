using ebsocketBroker.Core.Default.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using WebsocketBroker.Abstractions;

namespace WebsocketBroker.Core.Default
{
    public class MessageBroker : IBrokerManager
    {
        private readonly IRequestHandler _requestHandler;
        private readonly IResponseHandler _responseHandler;        
        private readonly ITopicFactory _topicFactory;
        private readonly ILogger<MessageBroker> _logger;
        private readonly SemaphoreSlim _publisherSlim = new SemaphoreSlim(Environment.ProcessorCount);
        private readonly SemaphoreSlim _consumerSlim = new SemaphoreSlim(Environment.ProcessorCount);

        public MessageBroker(IRequestHandler requestHandler,
            IResponseHandler responseHandler, 
            ILogger<MessageBroker> logger,
            ITopicFactory topicFactory)
        {
            _requestHandler = requestHandler;
            _responseHandler = responseHandler;            
            _topicFactory = topicFactory;
            _logger = logger;
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            var publisher =  Task.Run(async () =>
            {
                var reader = _requestHandler.GetPublisherStream();

                while (!cancellationToken.IsCancellationRequested)
                {
                    var context = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                    await _publisherSlim.WaitAsync().ConfigureAwait(false);

                    _ = Task.Run(() => {

                        try
                        {
                            var topic = _topicFactory.GetTopic(context.Endpoint);
                            topic.CreatePartition();
                            topic.AppendData(context.Content);
                        }
                        catch(Exception ex)
                        {
                            _logger.LogError(ex, $"Error while processing data for Topic {context.Endpoint}");
                        }
                        finally
                        {
                            _publisherSlim.Release();
                        }
                    
                    
                    });
                }

            });

            var subscriber = Task.Run(async () =>
            {
                var reader = _requestHandler.GetConsumerStream();

                while (!cancellationToken.IsCancellationRequested)
                {
                    var context = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                    await _consumerSlim.WaitAsync().ConfigureAwait(false);

                    _ = Task.Run(() => {

                        try
                        {
                            var topic = _topicFactory.GetTopic(context.Endpoint);
                            topic.CreatePartition();
                            //topic.AppendData(context.Content);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Error while processing data for Topic {context.Endpoint}");
                        }
                        finally
                        {
                            _consumerSlim.Release();
                        }


                    });
                }

            });

            return Task.WhenAll(publisher, subscriber);
        }
    }
}
