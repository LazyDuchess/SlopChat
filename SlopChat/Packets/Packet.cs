using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlopChat.Packets
{
    public abstract class Packet
    {
        public abstract string GUID
        {
            get;
        }
        public uint PlayerId = uint.MaxValue;

        public virtual void Write(BinaryWriter writer)
        {

        }

        public virtual void Read(BinaryReader reader)
        {

        }
    }
}
