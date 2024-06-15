using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlopChat
{
    public class ChatConfig
    {
        public int MaxMessages = 10;
        public int MaxStatusCharacters = 16;
        public int MaxCharacters = 200;
        public bool PhoneOutWhileTyping = true;
        public TMPFilter.Criteria ChatCriteria = new TMPFilter.Criteria(
            [
                "b",
                "color",
                "i",
                "mark",
                "sprite",
                "s",
                "sub",
                "sup",
                "u",
                ]
            , TMPFilter.Criteria.Kinds.Whitelist
            );
        public bool FilterProfanity = true;
    }
}
