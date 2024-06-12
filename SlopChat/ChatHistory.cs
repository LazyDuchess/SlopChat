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
        }

        public List<Entry> Entries;
        private TextMeshProUGUI _label;
        private ChatConfig _config;

        private void Awake()
        {
            _config = SlopChatPlugin.Instance.ChatConfig;
            Entries = [];
            _label = GetComponent<TextMeshProUGUI>();
            UpdateLabel();
        }

        public void UpdateLabel()
        {
            var newText = "";
            for(var i = 0; i < Entries.Count; i++)
            {
                var entry = Entries[i];
                newText += $"<color=yellow>{entry.PlayerName}<color=white> : {entry.Message}";
                if (i != Entries.Count - 1)
                    newText += "\n";
            }
            _label.text = newText;
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
    }
}
