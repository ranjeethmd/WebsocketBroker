using System;

namespace WebsocketBroker.Abstractions.Exceptions
{
    public class FrameException:ApplicationException
    {
        public FrameException(string message):base(message)
        {
            
        }
    }
}
