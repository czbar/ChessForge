using System;
using System.Collections.Generic;

namespace EngineService
{
    public partial class EngineProcess
    {
        /// <summary>
        /// Runs an infinite loop waiting and reading messages from the engine.
        /// to check on messages from the engine.
        /// Invokes the message handler to process the info.
        /// The message handler is EngineMessageProcessor.EngineMessageReceived(string)
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
#pragma warning disable IDE0051 // Remove unused private members
        private void ReadEngineMessagesV2()
#pragma warning restore IDE0051 // Remove unused private members
        {
            _isMessageRxLoopRunning = true;

            List<string> messages = new List<string>();

            lock (_lockEngineMessage)
            {
                if (_strmReader == null)
                {
                    _isMessageRxLoopRunning = false;
                    return;
                }

                try
                {
                    while (_strmReader != null)
                    {
                        messages.Clear();

                        // the first read must be without checking Peek() so we don't get block when in fact we cab get a message
                        string msg = _strmReader.ReadLine();

                        // special case for the null message.
                        if (msg == null)
                        {
                            if (ProcessNullMessage())
                            {
                                // if the engine is not ready, we need to exit the loop
                                // otherwise we will continue to read messages
                                break;
                            }
                        }
                        else
                        {
                            _badMessageCount = 0;

                            // we will not process messages that contain "currmove"
                            if (!msg.Contains(UciCommands.ENG_CURRMOVE))
                            {
                                messages.Add(msg);
                            }

                            // now we will read all messages that are available, checking Peek() to avoid blocking when all was read 
                            while (_strmReader.Peek() >= 0)
                            {
                                msg = _strmReader.ReadLine();
                                if (msg == null)
                                {
                                    if (ProcessNullMessage())
                                    {
                                        break;
                                    }
                                }
                                else if (!msg.Contains(UciCommands.ENG_CURRMOVE))
                                {
                                    messages.Add(msg);
                                }
                            }

                            ProcessMessagesList(messages);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _isMessageRxLoopRunning = false;

                    EngineLog.Message("ERROR: ReadEngineMessages():" + ex.Message);
                    throw new Exception("ReadEngineMessages():" + ex.Message);
                };
            }

            _isMessageRxLoopRunning = false;
        }

        /// <summary>
        /// Processes a list of messages from the engine.
        /// If there are consecutive INFO messages the last sequence starting with multipv==1
        /// is only processed.
        /// The BEST MOVE message and any other message types are processed individually.
        /// Messages that contain "currmove" are ignored.
        /// </summary>
        /// <param name="messages"></param>
        private void ProcessMessagesList(List<string> messages)
        {
            int index = 0;
            while (index < messages.Count)
            {
                bool noBulkProcessing = false;

                string rawMsg = messages[index];
                EngineLog.Message(rawMsg);
                if (rawMsg.StartsWith(UciCommands.ENG_READY_OK))
                {
                    index++;
                    HandleReadyOk();
                }
                else
                {
                    string message = InsertIdPrefixes(rawMsg);
                    if (message.Contains(UciCommands.ENG_BEST_MOVE))
                    {
                        index++;
                        _activeEvaluationMode = GoFenCommand.EvaluationMode.NONE;

                        if (!HandleBestMove())
                        {
                            message = InsertBestMoveDelayedPrefix(message);
                        }

                        if (!_ignoreNextBestMove)
                        {
                            EngineMessage?.Invoke(message);
                        }
                    }
                    else
                    {
                        if (rawMsg.StartsWith(UciCommands.ENG_INFO))
                        {
                            // if the following messages are INFOS, process them together
                            // there could be multiple sets of INFO messages from multipv 1 to multipv max
                            // If there is no INFO message with multipv==1, we will process the messages the "old way".
                            if (noBulkProcessing)
                            {
                                index++;
                                EngineMessage?.Invoke(message);
                            }
                            else
                            {
                                List<string> infoMessages = GetInfoMessagesSequence(messages, ref index, ref noBulkProcessing);
                                // the above call will set index to the last processed message
                                index++;
                                EngineInfoMessages?.Invoke(infoMessages);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Extracts a sequence of consecutive INFO messages starting with multipv 1
        /// from the supplied list.
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="index"></param>
        /// <param name="noBulkProcessing"></param>
        /// <returns></returns>
        private List<string> GetInfoMessagesSequence(List<string> messages, ref int index, ref bool noBulkProcessing)
        {
            int lastMultiPv_1_Index = -1;

            for (int i = index; i < messages.Count; i++)
            {
                if (!messages[i].Contains(UciCommands.ENG_INFO))
                {
                    break;
                }
                index = i;

                if (messages[i].Contains(UciCommands.ENG_MULTIPV_1))
                {
                    lastMultiPv_1_Index = i;
                }
            }

            if (lastMultiPv_1_Index < 0)
            {
                noBulkProcessing = true;
                return null;
            }
            else
            {
                // build a list of all consecutive INFO messages starting with multipv 1
                List<string> infoMessages = new List<string>();
                for (int i = lastMultiPv_1_Index; i <= index; i++)
                {
                    infoMessages.Add(messages[i]);
                    EngineLog.Message("INFO messages list item: " + messages[i]);
                }
                return infoMessages;
            }
        }

        /// <summary>
        /// Processes a null message.
        /// A null message can be received during initialization or when there is an engine error.
        /// if the former, we need to exit the loop and allow regular processing,
        /// if the latter, we need to handle error situation
        /// </summary>
        /// <returns></returns>
        private bool ProcessNullMessage()
        {
            EngineLog.Message("NULL message received");

            bool forceBreak = false;

            _badMessageCount++;
            if (_isEngineReady)
            {
                EngineMessage?.Invoke(null);
            }
            else
            {
                forceBreak = true;
            }

            return forceBreak;
        }
    }
}
