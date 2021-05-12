using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WebsocketBroker.Abstractions;
using WebsocketBroker.Abstractions.POCO;

namespace WebsocketBroker.Core.Default
{
    public class RequestHandler : IRequestHandler
    {
        private readonly ITcpClientManager _tcpClientManager;
        private static BlockingCollection<ContextRecord> RequestStream { get; } = new BlockingCollection<ContextRecord>();

        public RequestHandler(ITcpClientManager tcpClientManager)
        {
            _tcpClientManager = tcpClientManager;
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
                    }

                    await Task.WhenAll(tasks.Keys).ConfigureAwait(false);

                    Parallel.ForEach(tasks.Values, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, context => {
                        RequestStream.Add(context);                    
                    });
                }
            });            
        }

        public IEnumerable<ContextRecord> GetContext(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if(RequestStream.TryTake(out ContextRecord context, 60000))
                {
                    yield return context;
                }                               
            }
        }
    }
}
