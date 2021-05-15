namespace WebsocketBroker.Core.IO.POCO
{
    class TopicContext
    {
        public string Id { get; set; }
        public string CurrentFile { get; set; }
        public string CurrentLedgerFile { get; set; }
        public long CurrentLedgerRotation { get; set; }      
       
    }
}
