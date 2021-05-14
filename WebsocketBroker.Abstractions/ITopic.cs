using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebsocketBroker.Abstractions
{
    public interface ITopic
    {
        void AppendData(byte[] data);

        void CreatePartition();

        byte[] ReadData(long offset);
    }
}
