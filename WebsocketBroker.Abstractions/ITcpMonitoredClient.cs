using System.Threading;

namespace WebsocketBroker.Abstractions
{

    /// <summary>
    /// This interface helps two things.
    /// Helps manage indvidual ITcpClient
    /// </summary>
    public interface ITcpMonitoredClient: ITcpClient
    {
        void MonitorForData(CancellationToken token);
        void MonitorActivity(CancellationToken token);

    }

    
}
