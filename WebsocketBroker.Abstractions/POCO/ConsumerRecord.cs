namespace WebsocketBroker.Abstractions.POCO
{
    public record ConsumerRecord(string Endpoint, GroupName Groups,byte[] data);
    
}
