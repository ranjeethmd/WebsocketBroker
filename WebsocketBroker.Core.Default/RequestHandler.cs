using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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
        private readonly Regex _match = new Regex("GET (.*?) HTTP/1.1");

        private static ConcurrentQueue<ContextRecord> RequestStream { get; } = new ConcurrentQueue<ContextRecord>();

        public RequestHandler(ITcpClientManager tcpClientManager,
            IFrameHandler frameHandler,
            ILogger<RequestHandler> logger)
        {
            _tcpClientManager = tcpClientManager;
            _frameHandler = frameHandler;
            _logger = logger;
        }

        // TODO: Make the functon Idempotent
        public  Task BeginPorcessAsync(CancellationToken cancellationToken)
        {
            return Task.Run(async ()=> {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var records = _tcpClientManager.GetClientsWithData();

                    var tasks = new Dictionary<Task,ContextRecord>();

                    foreach(var record in records)
                    {
                        byte[] bytes = new byte[record.Client.Available];
                        tasks.Add(record.Stream.ReadAsync(bytes, 0, record.Client.Available, cancellationToken),new(record,bytes));
                        _tcpClientManager.UpdateClientRecordTime(record);
                    }

                    await Task.WhenAll(tasks.Keys).ConfigureAwait(false);

                    Parallel.ForEach(tasks.Values, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, context => {
                        RequestStream.Enqueue(context);                    
                    });
                }
            });            
        }

        public IEnumerable<byte[]> GetRequest(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if(RequestStream.TryDequeue(out ContextRecord context))
                {  
                    var content = Encoding.UTF8.GetString(context.Content);

                    if (Regex.IsMatch(content, "^GET", RegexOptions.IgnoreCase))
                    {
                        _logger.LogInformation("=====Handshaking from client=====\n{0}", content);

                        var method = _match.Match(content).Groups[1].ToString().ToLower();

                        switch (method)
                        {
                            case "/publish":
                                break;
                            case "/consume":
                                break;
                            default:
                                _logger.LogError($"Unknown method {method}");
                                _tcpClientManager.RemoveClient(context.Record);
                                break;
                        }     
                    }

                    else
                    {
                        var data = _frameHandler.ReadFrame(context.Content);
                        yield return data;
                    }
                }
                else
                {
                    break;
                }
            }
        }
    }
}
