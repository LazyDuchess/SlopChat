using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlopChat.Packets
{
    public class MessagePacket : Packet
    {
        public const string kGUID = "SlopChat-Message";
        public override string GUID => kGUID;
        public string Message = "";
        private const byte Version = 0;

        public override void Write(BinaryWriter writer)
        {
            writer.Write(Version);
            writer.Write(Message);
        }

        public override void Read(BinaryReader reader)
        {
            var version = reader.ReadByte();
            Message = reader.ReadString();
        }
    }
}
