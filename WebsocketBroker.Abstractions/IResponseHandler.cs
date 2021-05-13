using System.Threading;
using System.Threading.Tasks;
using WebsocketBroker.Abstractions.POCO;

namespace WebsocketBroker.Abstractions
{
    public interface IResponseHandler
    {
        Task SendHeaderResponse(ClientRecord clientRecord, byte[] swkaSha1, CancellationToken token);

        Task SendResponse(ClientRecord clientRecord, byte[] data,CancellationToken token);
    }
}
