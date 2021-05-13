using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading.Tasks;

namespace WebsocketBroker.Core.IO
{
    public class Topic
    {
        private readonly string _name,_location;
        private readonly Dictionary<string, MemoryMappedFile> _refs = new Dictionary<string, MemoryMappedFile>();
        public Topic(string name, string location)
        {
            _name = name;
            _location = location;

            Reload();
        }

        private string CurrentFile { get; set; }
        private long CurrentFileIndex { get; set; }
        private ulong CurrentOffset { get; set; }

        public void  CreatePartition()
        {
            var date = DateTimeOffset.UtcNow.Date.ToString("ddMMyyyy");
            CurrentFile = $"T-{_name}-{date}-{CurrentFileIndex}";

            var topicPath = Path.Combine(_location, $"T-{_name}");
            
            Directory.CreateDirectory(topicPath);

            _refs.Add(CurrentFile ,MemoryMappedFile.CreateFromFile(Path.Combine(topicPath,$"P-{date}-{CurrentFileIndex}.data"), FileMode.OpenOrCreate, CurrentFile,1000000));

        }

        private void Reload()
        {

        }

        public void AppendData(byte[] data)
        {
            var offset = (long)CurrentOffset;
            using (MemoryMappedViewAccessor accessor = _refs[CurrentFile].CreateViewAccessor())
            {
                accessor.Write(offset, (ulong)data.Length);
                accessor.WriteArray(offset + 8, data, 0, data.Length);
            }

            CurrentOffset = (ulong) (offset + 8 + data.Length);
        }

        public byte[] ReadData(long offset)
        {
            using (MemoryMappedViewAccessor accessor = _refs[CurrentFile].CreateViewAccessor())
            {
                
                long pointer = 0;

                for(int i =0; i <= offset; i++)
                {
                    if (i > 0)
                    {
                        pointer += (long)accessor.ReadUInt64(pointer) + 8;                        
                    }
                }                

                var size = accessor.ReadUInt64(pointer);
                byte[] data = new byte[size];
                accessor.ReadArray(pointer + 8, data, 0, data.Length);

                return data;
            }
            

        }

        public void RemoveTill(long offset)
        {
            using (MemoryMappedViewAccessor accessor = _refs[CurrentFile].CreateViewAccessor())
            {

                long pointer = 0;

                for (int i = 0; i <= offset; i++)
                {
                    if (i > 0)
                    {
                        pointer += (long)accessor.ReadUInt64(pointer) + 8;
                    }

                    var size = accessor.ReadUInt64(pointer);
                    byte[] data = new byte[size];
                    accessor.WriteArray(pointer + 8, data, 0, data.Length);
                }               

            }

        }

    }
}
