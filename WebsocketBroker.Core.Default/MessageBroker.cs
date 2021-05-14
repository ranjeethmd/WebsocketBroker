using ebsocketBroker.Core.Default.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public MessageBroker(IRequestHandler requestHandler,
            IResponseHandler responseHandler,
            IFrameHandler frameHandler,
            ITopicFactory topicFactory)
        {
            _requestHandler = requestHandler;
            _responseHandler = responseHandler;            
            _topicFactory = topicFactory;
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {                   

                    foreach (var content in _requestHandler.GetRequest(cancellationToken))
                    {
                        var topic = _topicFactory.GetTopic("Test");
                        topic.CreatePartition();
                        topic.AppendData(content);

                        

                        //if (isHandShake)
                        //{
                        //    _ = _responseHandler.SendHeaderResponse(context.Record, content, cancellationToken);
                        //}
                        //else
                        //{
                        //    var frame = _frameHandler.CreateFrame(content);
                        //    _ = _responseHandler.SendResponse(context.Record, frame, cancellationToken);
                        //}
                    }                   
                }

            });
        }
    }
}
