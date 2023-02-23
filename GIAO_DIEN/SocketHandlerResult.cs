using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GIAO_DIEN
{
    public class SocketHandlerResult
    {
        public byte[] buffer { get; set; }
        public int lenght;

        public SocketHandlerResult(byte[] buffer, int lenght)
        {
            this.buffer = buffer;
            this.lenght = lenght;

        }

    }
}
