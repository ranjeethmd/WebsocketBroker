using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebsocketBroker.Abstractions;

namespace ebsocketBroker.Core.Default.Interfaces
{
    public interface ITopicFactory
    {
        ITopic GetTopic(string name);
    }
}
