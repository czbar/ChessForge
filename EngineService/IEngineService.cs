using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EngineService
{
    public interface IEngineService
    {
        bool StartEngine();
        void StopEngine();
        void SendCommand(string command);
        event Action<string> EngineMessage;
    }
}
