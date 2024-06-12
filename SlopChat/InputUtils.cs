using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlopChat
{
    public static class InputUtils
    {
        public static int InputBlockers { get; private set; } = 0;
        public static bool InputBlocked
        {
            get
            {
                return InputBlockers > 0;
            }
        }
        public static void PushInputBlocker()
        {
            InputBlockers++;
        }

        public static void PopInputBlocker()
        {
            InputBlockers--;
        }
    }
}
