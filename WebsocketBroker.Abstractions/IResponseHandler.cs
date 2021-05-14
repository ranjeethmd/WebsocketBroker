using System.Threading;
using System.Threading.Tasks;
using WebsocketBroker.Abstractions.POCO;

namespace WebsocketBroker.Abstractions
{
    public interface IResponseHandler
    {
        Task SendHeaderResponse(ClientRecord clientRecord, string header, CancellationToken token);

        Task SendResponse(ClientRecord clientRecord, byte[] data,CancellationToken token);
    }
}
