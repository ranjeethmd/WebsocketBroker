using LiteDB;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using WebsocketBroker.Core.IO.POCO;
using WebsocketBroker.Abstractions;

namespace WebsocketBroker.Core.IO
{
    public class Topic:ITopic
    {
        private readonly string _name,_location;
        private readonly LiteDatabase _db;
        private readonly Dictionary<string, MemoryMappedFile> _refs = new Dictionary<string, MemoryMappedFile>();
        private readonly ManualResetEvent _reloading = new ManualResetEvent(false);
        private readonly int LEDGER_FILE_SIZE = 1000000000;
        private readonly ILiteCollection<TopicContext> _topicContext;
        private readonly ILiteCollection<LedgerInfo> _ledgerInfo;


        public Topic(string name, string location)
        {
            _name = name;
            _location = location;
            var topicPath = Path.Combine(_location, $"T-{_name}");
           
            _db = new LiteDatabase(Path.Combine(topicPath,$"T-{_name}.db"));

            _topicContext = _db.GetCollection<TopicContext>("context");
            _ledgerInfo = _db.GetCollection<LedgerInfo>("ledger");

           

            Reload();
        }       
        

        public void  CreatePartition()
        {
            _reloading.Reset();

            var date = DateTimeOffset.UtcNow.Date.ToString("ddMMyyyy");

            var context =_topicContext.Find(x => x.Id == _name,limit:1).FirstOrDefault();

            var topicPath = Path.Combine(_location, $"T-{_name}");

            if (context == null)
            {
                context = new TopicContext
                {
                    Id = _name,
                    CurrentFile = $"T-{_name}-{date}",
                    CurrentLedgerRotation = 1,
                    CurrentLedgerFile = Path.Combine(topicPath, $"L-{date}.dat")
                };                
            }

            Directory.CreateDirectory(topicPath);

            if(File.Exists(context.CurrentLedgerFile))
            {
                _refs.Add(context.CurrentFile, MemoryMappedFile.CreateFromFile(context.CurrentLedgerFile, FileMode.OpenOrCreate, context.CurrentFile));
            }
            else
            {
                _refs.Add(context.CurrentFile, MemoryMappedFile.CreateFromFile(context.CurrentLedgerFile, FileMode.OpenOrCreate, context.CurrentFile, LEDGER_FILE_SIZE));
                var fileContext = _db.GetCollection<FileContext>();
                fileContext.Upsert(new FileContext {Id = context.CurrentFile,Path = context.CurrentLedgerFile });
            }


            _topicContext.Upsert(context);

            _reloading.Set();
        }

        private void IncreasePartitionCapacity()
        {
            _reloading.Reset();

            var context = _topicContext.FindOne(x => x.Id == _name);

            var mmf = _refs[context.CurrentFile];

            mmf.Dispose();

            context.CurrentLedgerRotation = context.CurrentLedgerRotation + 1;

            _refs.Add(context.CurrentFile, MemoryMappedFile.CreateFromFile(context.CurrentLedgerFile, FileMode.OpenOrCreate, context.CurrentFile, context.CurrentLedgerRotation * LEDGER_FILE_SIZE));

            _topicContext.Update(context);

            _reloading.Set();
        }
        private void Reload()
        {

        }

        public void AppendData(byte[] data)
        {
            _reloading.WaitOne();

            var context = _topicContext.FindOne(x => x.Id == _name);

            var info = _ledgerInfo.FindOne(Query.All(Query.Descending));           

            var position = info?.Position + info?.Length  ?? 0;

            using (MemoryMappedViewAccessor accessor = _refs[context.CurrentFile].CreateViewAccessor())
            {               
                accessor.WriteArray(position,  data, 0, data.Length);
            }            

            _ledgerInfo.Insert(new LedgerInfo { Id = info?.Id + 1 ?? 1, Length = data.Length, Position =  position, CreateData = DateTimeOffset.UtcNow});            
        }

        public byte[] ReadData(long offset)
        {
            _reloading.WaitOne();

            var context = _topicContext.FindOne(x => x.Id == _name);

            var infos = _ledgerInfo.FindAll();

            var info = _ledgerInfo.FindOne(x => x.Id == offset);

            using (MemoryMappedViewAccessor accessor = _refs[context.CurrentFile].CreateViewAccessor())
            {
                var position = info.Position; 
                var size = info.Length;
                byte[] data = new byte[size];
                accessor.ReadArray(position, data, 0, data.Length);

                return data;
            }
            

        }

        public void RemoveTill(long offset)
        {
            _reloading.WaitOne();

            var context = _topicContext.FindOne(x => x.Id == _name);
            var infos = _ledgerInfo.Find(x => x.Id <= offset);

            using (MemoryMappedViewAccessor accessor = _refs[context.CurrentFile].CreateViewAccessor())
            {
                foreach (var info in infos)
                {
                    var data = new byte[info.Length];
                    accessor.WriteArray(info.Position, data, 0, data.Length);
                }               

            }

        }

    }
}
