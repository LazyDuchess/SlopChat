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
        public List<ChatHistory.Entry> Entries;
        private const byte Version = 0;

        public override void Write(BinaryWriter writer)
        {
            writer.Write(Version);
            writer.Write(Entries.Count);
            foreach(var entry in Entries)
            {
                writer.Write(entry.PlayerId);
                writer.Write(entry.PlayerName);
                writer.Write(entry.Message);
            }
        }

        public override void Read(BinaryReader reader)
        {
            var version = reader.ReadByte();
            var entries = reader.ReadInt32();
            Entries = [];
            if (entries > SlopChatPlugin.Instance.ChatConfig.MaxMessages)
                entries = SlopChatPlugin.Instance.ChatConfig.MaxMessages;
            for (var i = 0; i < entries; i++)
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
                Entries.Add(entry);
            }
        }
    }
}
