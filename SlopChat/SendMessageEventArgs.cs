using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlopChat
{
    public class SendMessageEventArgs
    {
        public string Message;
        public bool Cancel = false;

        public SendMessageEventArgs(string message)
        {
            Message = message;
        }
    }
}
