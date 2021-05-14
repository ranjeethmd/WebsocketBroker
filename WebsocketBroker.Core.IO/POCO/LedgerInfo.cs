using System;


namespace WebsocketBroker.Core.IO.POCO
{
    class LedgerInfo
    {
        public long Id { get; set; }
        public long Position { get; set; }
        public long Length { get; set; }
        public DateTimeOffset CreateData { get; set; }
    }
}
