using ebsocketBroker.Core.Default.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using WebsocketBroker.Abstractions;
using WebsocketBroker.Abstractions.POCO;

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

        private Task _publishTask;
        private Task _consumerTask;

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
            if (_publishTask == null)
            {
                _publishTask = Task.Run(async () =>
               {
                   var reader = _requestHandler.GetPublisherStream();

                   while (!cancellationToken.IsCancellationRequested)
                   {
                       await _publisherSlim.WaitAsync(cancellationToken).ConfigureAwait(false);
                       var context = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);

                       _ = Task.Run(async () =>
                       {

                           try
                           {
                               var topic = _topicFactory.GetTopic(context.Endpoint);
                               topic.CreatePartition();
                               topic.AppendData(context.Data);

                               await _responseHandler.SendResponseAsync(context.Group, AckConstants.ACCEPT, cancellationToken).ConfigureAwait(false);
                           }
                           catch (Exception ex)
                           {
                               _logger.LogError(ex, $"Error while processing data for Topic {context.Endpoint}");

                               await _responseHandler.SendResponseAsync(context.Group, AckConstants.REJECT, cancellationToken).ConfigureAwait(false);
                           }
                           finally
                           {
                               _publisherSlim.Release();
                           }


                       });
                   }

               });
            }

            if (_consumerTask == null)
            {
                _consumerTask = Task.Run(async () =>
                {
                    var reader = _requestHandler.GetConsumerStream();

                    while (!cancellationToken.IsCancellationRequested)
                    {
                        await _consumerSlim.WaitAsync(cancellationToken).ConfigureAwait(false);
                        var context = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);

                        _ = Task.Run(async () =>
                        {

                            try
                            {
                                var topic = _topicFactory.GetTopic(context.Endpoint);
                                topic.CreatePartition();

                                var offset = BitConverter.ToInt64(context.Data);

                                var data = topic.ReadData(offset);

                                await _responseHandler.SendResponseAsync(context.Group, data, cancellationToken).ConfigureAwait(false);
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
            }

            return Task.WhenAll(_publishTask, _consumerTask);
        }
    }
}
