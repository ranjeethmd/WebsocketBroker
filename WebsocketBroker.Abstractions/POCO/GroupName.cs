namespace WebsocketBroker.Abstractions.POCO
{
    public class GroupName
    {
        public GroupName(string name, long offset = 0)
        {
            Name = name;
            Offset = offset;
        }

        public string Name { get; }
        public long Offset { get; set; }
        
    }
}
