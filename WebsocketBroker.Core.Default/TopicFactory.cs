using ebsocketBroker.Core.Default.Interfaces;
using System;
using System.Collections.Concurrent;
using System.IO;
using WebsocketBroker.Abstractions;
using WebsocketBroker.Core.IO;

namespace ebsocketBroker.Core.Default.Services
{
    public class TopicFactory : ITopicFactory
    {
        private readonly static ConcurrentDictionary<string, ITopic> _cache = new ConcurrentDictionary<string, ITopic>();
        private readonly string _location;

        public TopicFactory(string location)
        {
            var path =  location;
            path = Environment.ExpandEnvironmentVariables(path);
            _location = Path.GetFullPath(path);
        }
        public ITopic GetTopic(string name)
        {
            return _cache.GetOrAdd(name, new Topic(name, _location));
        }
    }
}
