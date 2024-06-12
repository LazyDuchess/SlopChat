using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SlopChat
{
    public class ChatAssets
    {
        public AssetBundle Bundle { get; private set; }
        public ChatAssets(string path)
        {
            Bundle = AssetBundle.LoadFromFile(Path.Combine(path, "slopchat"));
        }
    }
}
