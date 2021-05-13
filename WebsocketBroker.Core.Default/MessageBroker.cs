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
        private readonly IFrameHandler _frameHandler;
        public MessageBroker(IRequestHandler requestHandler,
            IResponseHandler responseHandler,
            IFrameHandler frameHandler)
        {
            _requestHandler = requestHandler;
            _responseHandler = responseHandler;
            _frameHandler = frameHandler;
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var tasks = new List<Task>();

                    foreach (var context in _requestHandler.GetContext(cancellationToken))
                    {
                        var content = _frameHandler.ReadFrame(context.Content, out bool isHandShake);

                        if (isHandShake)
                        {
                            tasks.Add(_responseHandler.SendHeaderResponse(context.Record, content,cancellationToken));
                        }
                        else
                        {
                           
                            tasks.Add(_responseHandler.SendResponse(context.Record, content,cancellationToken));
                        }
                    }

                    await Task.WhenAll(tasks).ConfigureAwait(false);
                }
            });
        }
    }
}
