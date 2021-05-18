using System;

namespace WebsocketBroker.Abstractions.POCO
{
    public class AckConstants
    {
        public static byte[] ACCEPT = BitConverter.GetBytes(true);
        public static byte[] REJECT = BitConverter.GetBytes(false);
    }
}
