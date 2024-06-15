using SlopCrew.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace SlopChat
{
    public class ChatHistory : MonoBehaviour
    {
        public class Entry
        {
            public string PlayerName;
            public uint PlayerId;
            public string Message;

            public void Sanitize()
            {
                PlayerName = SlopChatPlugin.Instance.SanitizeName(PlayerName);
                Message = SlopChatPlugin.Instance.SanitizeMessage(Message, ProfanityFilter.CensoredMessage);
            }
        }

        public List<Entry> Entries;
        private TextMeshProUGUI _label;
        private ChatConfig _config;

        private void Awake()
        {
            _config = SlopChatPlugin.Instance.ChatConfig;
            Entries = [];
            for(var i = 0; i < SlopChatPlugin.Instance.ChatConfig.MaxMessages; i++)
            {
                Entries.Add(null);
            }
            _label = GetComponent<TextMeshProUGUI>();
            UpdateLabel();
        }

        public bool UpdateLabel()
        {
            var newText = "";
            for(var i = 0; i < Entries.Count; i++)
            {
                var entry = Entries[i];
                if (entry == null) continue;
                var message = entry.Message;
                if (ChatController.MutedPlayers.Contains(TMPFilter.RemoveAllTags(entry.PlayerName)))
                    message = "Muted message.";
                else
                {
                    if (ChatController.Instance.ChatPlayersById.TryGetValue(entry.PlayerId, out var player)) {
                        if (player.Status != "")
                        {
                            newText += $"<color=yellow>[{player.Status}]</color> ";
                        }
                    }
                    else if (APIManager.API.PlayerIDExists(entry.PlayerId) == false && TMPFilter.RemoveAllTags(entry.PlayerName) == TMPFilter.RemoveAllTags(APIManager.API.PlayerName))
                    {
                        if (ChatController.Status != "")
                        {
                            newText += $"<color=yellow>[{ChatController.Status}]</color> ";
                        }
                    }
                }
                newText += $"<color=yellow>{entry.PlayerName}<color=white> : {message}";
                if (i != Entries.Count - 1)
                    newText += "\n";
            }
            if (newText != _label.text)
            {
                _label.text = newText;
                return true;
            }
            _label.text = newText;
            return false;
        }

        public void Append(Entry message)
        {
            Entries.Add(message);
            if (Entries.Count > _config.MaxMessages)
            {
                Entries.RemoveAt(0);
            }
            UpdateLabel();
        }

        public void Set(Entry message, int position)
        {
            Entries[position] = message;
        }
    }
}
