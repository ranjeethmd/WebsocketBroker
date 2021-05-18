using System;
using System.Linq;
using System.Net.Sockets;
using WebsocketBroker.Abstractions;
using WebsocketBroker.Abstractions.POCO;

namespace WebsocketBroker.Core.Default
{
    public class RandomSelectionStratergy : IClientSelectionStratergy
    {
        private ISubscriptionManager _subscriptionManager;
        private Random _random = new  Random();

        public RandomSelectionStratergy(ISubscriptionManager subscriptionManager)
          
        {
            _subscriptionManager = subscriptionManager;
           
        }
        public TcpClient SelectClient(Group group)
        {
            
            var clients = _subscriptionManager.GetGroupClients(group);

            switch (clients.Count)
            {
                case 0:
                    return null;
                case 1:
                    return clients.ElementAt(0);
                default:
                    return clients.ElementAt(_random.Next(0, clients.Count - 1));
            }
        }
    }
}
