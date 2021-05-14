namespace WebsocketBroker.Abstractions
{
    public interface IFrameHandler
    {
        byte[] ReadFrame(byte[] frameData);

        byte[] CreateFrame(byte[] data);
    }
}
