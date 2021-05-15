using WebsocketBroker.Abstractions;

namespace ebsocketBroker.Core.Default.Interfaces
{
    public interface ITopicFactory
    {
        ITopic GetTopic(string name);
    }
}
