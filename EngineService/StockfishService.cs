using System;
using System.IO;
using System.Diagnostics;
using System.Timers;
using System.Diagnostics.Tracing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EngineService
{
    public class StockfishService : IEngineService
    {
        public bool IsEngineRunning = false;
        public bool IsEngineReady = false;

        private StreamReader strmReader;
        private StreamWriter strmWriter;
        private Process engineProcess;

        public event Action<string> EngineMessage;

        private bool _debugMode;

        private Timer MessagePollTimer = new System.Timers.Timer();


        public StockfishService(bool debugMode)
        {
            _debugMode = debugMode;
            if (_debugMode)
            {
                EngineLog.SetDebugMode();
            }
        }

        public bool StartEngine()
        {
            //FileInfo engine = new FileInfo(Path.Combine(Environment.CurrentDirectory, "stockfish_8_x64.exe"));
            FileInfo engine = new FileInfo(Path.Combine(Environment.CurrentDirectory, "stockfish_15_x64_avx2.exe"));
            if (engine.Exists && engine.Extension == ".exe")
            {
                try
                {
                    engineProcess = new Process();
                    engineProcess.StartInfo.FileName = engine.FullName;
                    engineProcess.StartInfo.UseShellExecute = false;
                    engineProcess.StartInfo.RedirectStandardInput = true;
                    engineProcess.StartInfo.RedirectStandardOutput = true;
                    engineProcess.StartInfo.RedirectStandardError = true;
                    engineProcess.StartInfo.CreateNoWindow = true;

                    engineProcess.Start();

                    strmWriter = engineProcess.StandardInput;
                    strmReader = engineProcess.StandardOutput;

                    CreateTimer();
                    // start the message polling timer
                    // it will be stopped when ok is received
                    StartMessagePollTimer();

                    strmWriter.WriteLine(UciCommands.ENG_UCI);
                    strmWriter.WriteLine(UciCommands.ENG_ISREADY);
                    strmWriter.WriteLine(UciCommands.ENG_UCI_NEW_GAME);

                    IsEngineRunning = true;

                    return true;
                }
                catch
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public void StopEngine()
        {
            if (engineProcess != null && !engineProcess.HasExited)
            {
                //engineListener.Dispose();
                strmReader.Close();
                strmWriter.Close();
            }

            IsEngineRunning = false;
            IsEngineReady = false;
        }

        /// <summary>
        /// Sends a command to the engine by writing to its standard
        /// stream.
        /// </summary>
        /// <param name="command"></param>
        public void SendCommand(string command)
        {
            if (strmWriter != null && command != UciCommands.ENG_UCI)
            {
                EngineLog.Message("Cmd: " + command);
                strmWriter.WriteLine(command);
            }
        }


        /// <summary>
        /// Creates a timer to poll for engine messages.
        /// </summary>
        private void CreateTimer()
        {
            MessagePollTimer.Enabled = false;
            MessagePollTimer.Interval = 50;
            MessagePollTimer.Elapsed += new ElapsedEventHandler(ReadEngineMessages);
        }

        public void StartMessagePollTimer()
        {
            MessagePollTimer.Enabled = true;
        }
        public void StopMessagePollTimer()
        {
            MessagePollTimer.Enabled = false;
        }

        public static object EngineLock = new object();

        /// <summary>
        /// Called periodically to check on messages from the engine.
        /// Invokes the message handler to process the info.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void ReadEngineMessages(object source, ElapsedEventArgs e)
        {
            lock (EngineLock)
            {
                try
                {
                    var message = strmReader.ReadLine();
                    if (message != null)
                    {
                        EngineLog.Message(message);
                        if (message.StartsWith(UciCommands.ENG_READY_OK))
                        {
                            IsEngineReady = true;
                            StopMessagePollTimer();
                        }
                        else
                        {
                            EngineMessage?.Invoke(message);
                        }
                    }
                    else
                    {
                        EngineLog.Message("NULL");
                    }
                }
                catch
                {
                    throw new Exception("ReadEngineMessages()");
                };
            }
        }

    }
}

