using SlopCrew.API;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Bindings;

namespace SlopChat.Packets
{
    public class ChatHistoryPacket : Packet
    {
        public const string kGUID = "SlopChat-ChatHistory";
        public override string GUID => kGUID;
        public int Index = 0;
        public ChatHistory.Entry Entry = null;
        private const byte Version = 0;

        public override void Write(BinaryWriter writer)
        {
            writer.Write(Version);
            writer.Write(Index);
            writer.Write(Entry != null);
            if (Entry != null)
            {
                writer.Write(Entry.PlayerId);
                writer.Write(Entry.PlayerName);
                writer.Write(Entry.Message);
            }
        }

        public override void Read(BinaryReader reader)
        {
            var version = reader.ReadByte();
            Index = reader.ReadInt32();
            var isValid = reader.ReadBoolean();
            if (isValid)
            {
                var entry = new ChatHistory.Entry();
                entry.PlayerId = reader.ReadUInt32();
                entry.PlayerName = reader.ReadString();
                entry.Message = reader.ReadString();
                var slopApi = APIManager.API;
                if (slopApi.PlayerIDExists(entry.PlayerId) == true)
                {
                    entry.PlayerName = slopApi.GetPlayerName(entry.PlayerId);
                }
                entry.Sanitize();
                Entry = entry;
            }
        }
    }
}
