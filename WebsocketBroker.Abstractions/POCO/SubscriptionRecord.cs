using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebsocketBroker.Abstractions.POCO
{
    public record SubscriptionRecord(string Endpoint, Subscription Subscription);
    
}
