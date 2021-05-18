namespace WebsocketBroker.Abstractions.POCO
{
    public class Group
    {
        public Group(string name)
        {
            Name = name.ToUpperInvariant();
        }

        public string Name { get; }
      
        
    }
}
