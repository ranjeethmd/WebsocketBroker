namespace WebsocketBroker.Abstractions
{
    public interface ITopic
    {
        void AppendData(byte[] data);

        void CreatePartition();

        byte[] ReadData(long offset);
    }
}
