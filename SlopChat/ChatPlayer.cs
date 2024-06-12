using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlopChat
{
    public class ChatPlayer
    {
        public string Name;
        public uint Id;
        public ChatController.NetworkStates NetworkState;
        public uint HostId;
        public DateTime LastHeartBeat;
    }
}
