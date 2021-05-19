using System.Threading;
using System.Threading.Tasks;

namespace WebsocketBroker.Abstractions
{

    /// <summary>
    /// This interface helps two things.
    /// Helps manage indvidual ITcpClient
    /// </summary>
    public interface ITcpClient
    {
        bool IsDataAvailable();

        Task<byte[]> GetDataAync(CancellationToken cancellationToken);

        Task<bool> SendAsync(byte[] data, CancellationToken cancellationToken);

        void Disconnect();

    }

    
}
