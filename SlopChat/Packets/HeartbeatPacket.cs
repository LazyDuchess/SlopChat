using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlopChat.Packets
{
    internal class HeartbeatPacket : Packet
    {
        public const string kGUID = "SlopChat-Heartbeat";
        public override string GUID => kGUID;
        private const byte Version = 1;
        public ChatController.NetworkStates NetworkState = ChatController.NetworkStates.None;
        public uint HostId = uint.MaxValue;
        public DateTime HostStartTime;
        public string Status = "";

        public override void Write(BinaryWriter writer)
        {
            writer.Write(Version);
            writer.Write((int)NetworkState);
            writer.Write(HostId);
            writer.Write(HostStartTime.ToBinary());
            writer.Write(Status);
        }

        public override void Read(BinaryReader reader)
        {
            var version = reader.ReadByte();
            NetworkState = (ChatController.NetworkStates)reader.ReadInt32();
            HostId = reader.ReadUInt32();
            HostStartTime = DateTime.FromBinary(reader.ReadInt64());
            if (version > 0)
            {
                Status = reader.ReadString();
            }
        }
    }
}
