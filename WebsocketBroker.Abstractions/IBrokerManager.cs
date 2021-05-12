using System.Threading.Tasks;

namespace WebsocketBroker.Abstractions
{
    public interface IBrokerManager
    {
        Task StartAsync();
    }
}
