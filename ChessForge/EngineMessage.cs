using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessForge
{
    public class EngineMessage
    {
        public EngineMessage(MessageType type, string messageText)
        {
            Type = type;
            MessageText = messageText;
        }

        public enum MessageType
        {
            UNKNOWN,
            INFO,
            BEST_MOVE
        };

        public MessageType Type;

        public string MessageText;
    }
}
