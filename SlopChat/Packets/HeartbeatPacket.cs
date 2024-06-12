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
        private const byte Version = 0;
        public ChatController.NetworkStates NetworkState = ChatController.NetworkStates.None;
        public uint HostId = uint.MaxValue;

        public override void Write(BinaryWriter writer)
        {
            writer.Write(Version);
            writer.Write((int)NetworkState);
            writer.Write(HostId);
        }

        public override void Read(BinaryReader reader)
        {
            var version = reader.ReadByte();
            NetworkState = (ChatController.NetworkStates)reader.ReadInt32();
            HostId = reader.ReadUInt32();
        }
    }
}
