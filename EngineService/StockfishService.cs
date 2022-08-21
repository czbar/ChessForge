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
    /// <summary>
    /// This class starts the engine service and provides a means of
    /// communicating with it.
    /// It is built as a DLL so that it can be easily tested independent of the main program.
    /// </summary>
    public class EngineProcess : IEngineService
    {
        // true if the engine is running ready to accept requets
        public bool IsEngineRunning = false;

        // true if we have received "readyok" from the engine 
        public bool IsEngineReady = false;

        // reads engine process's STDOUT 
        private StreamReader strmReader;

        // writes to engine process's STDIN 
        private StreamWriter strmWriter;

        // the engine service process
        private Process engineProcess;

        /// <summary>
        /// Action invoked by ReadEngineMessage().
        /// It is defined in EngineMessageProcessor as EngineMessageReceived().
        /// </summary>
        public event Action<string> EngineMessage;

        // if true, engine's messages will be logged
        private bool _debugMode;

        // the timer polling for engine's messages
        private Timer MessagePollTimer = new Timer();


        /// <summary>
        /// Creates the Engine Service object.
        /// </summary>
        /// <param name="debugMode"></param>
        public EngineProcess(bool debugMode, string appPath)
        {
            _debugMode = debugMode;
            if (_debugMode)
            {
                EngineLog.SetDebugMode(appPath);
            }
        }

        /// <summary>
        /// Starts the engine process.
        /// Initializes all relevant objects and variables.
        /// </summary>
        /// <returns></returns>
        public bool StartEngine(string enginePath)
        {
            try
            {
                EngineLog.Message("Starting engine: " + enginePath);
                engineProcess = new Process();
                engineProcess.StartInfo.FileName = enginePath;
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

                EngineLog.Message("Engine running.");

                return true;
            }
            catch (Exception ex)
            {
                EngineLog.Message("Failed to start engine: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Stops the engine process.
        /// </summary>
        public void StopEngine()
        {
            if (engineProcess != null && !engineProcess.HasExited)
            {
                strmReader.Close();
                strmWriter.Close();
                engineProcess.Close();
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
        /// Starts the message poll timer.
        /// </summary>
        public void StartMessagePollTimer()
        {
            MessagePollTimer.Enabled = true;
        }

        /// <summary>
        /// Stops the message poll timer
        /// </summary>
        public void StopMessagePollTimer()
        {
            if (MessagePollTimer != null)
            {
                MessagePollTimer.Enabled = false;
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

        /// <summary>
        /// A lock object to use when reading engine messages
        /// </summary>
        private static object EngineLock = new object();

        // helper counter for ReadEngineMessages
        private int counter = 0;

        /// <summary>
        /// Called periodically in response to MessagePollTimer's elapse event
        /// to check on messages from the engine.
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
                    string message;
                    while ((message = strmReader.ReadLine()) != null)
                    {
                        if (message != null && !message.Contains("currmove"))
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

                        // escape every now then if it gets to tight so that GUI updates can happen
                        counter++;
                        if (counter % 10 == 0)
                        {
                            counter = 0;
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("ReadEngineMessages():" + ex.Message);
                };
            }
        }

    }
}

