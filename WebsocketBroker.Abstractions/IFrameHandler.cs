namespace WebsocketBroker.Abstractions
{
    public interface IFrameHandler
    {
        byte[] ReadFrame(byte[] frameData, out bool isHandShake);

        byte[] CreateFrame(byte[] data);
    }
}
