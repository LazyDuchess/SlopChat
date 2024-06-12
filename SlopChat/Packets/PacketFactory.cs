using SlopCrew.API;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SlopChat.Packets
{
    public static class PacketFactory
    {
        public static void SendPacket(Packet packet, ISlopCrewAPI slopAPI)
        {
            var data = SerializePacket(packet);
            slopAPI.SendCustomPacket(packet.GUID, data);
        }

        public static byte[] SerializePacket(Packet packet)
        {
            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);
            packet.Write(writer);
            writer.Flush();
            var data = stream.ToArray();
            writer.Close();
            stream.Close();
            return data;
        }

        public static T DeserializePacket<T>(string guid, byte[] data, uint playerId) where T : Packet
        {
            return DeserializePacket(guid, data, playerId) as T;
        }

        public static Packet DeserializePacket(string guid, byte[] data, uint playerId)
        {
            var stream = new MemoryStream(data);
            var reader = new BinaryReader(stream);
            Packet packet = null;
            switch (guid)
            {
                case HeartbeatPacket.kGUID:
                    packet = new HeartbeatPacket();
                    break;

                case MessagePacket.kGUID:
                    packet = new MessagePacket();
                    break;

                case ChatHistoryPacket.kGUID:
                    packet = new ChatHistoryPacket();
                    break;
            }
            if (packet != null)
            {
                packet.PlayerId = playerId;
                packet.Read(reader);
            }
            reader.Close();
            stream.Close();
            return packet;
        }
    }
}
