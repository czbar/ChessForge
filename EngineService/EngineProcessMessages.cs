using System;
using System.Collections.Generic;

namespace EngineService
{
    public partial class EngineProcess
    {
        /// <summary>
        /// This is V2 of the message processing loop.
        /// Runs an infinite loop waiting and reading messages from the engine.
        /// Invokes either the message handler for a single message EngineMessage?.Invoke()
        /// or a list of messages EngineInfoMessages?.Invoke().
        /// (The previous version was only doing the former thus being less peformant). 
        /// The message handler EngineMessage is EngineMessageProcessor.EngineMessageReceived()
        /// while for EngineInfoMessages it is EngineMessageProcessor.EngineInfoMessagesReceived().
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void ReadEngineMessagesV2()
        {
            _isMessageRxLoopRunning = true;

            List<string> messages = new List<string>();

            lock (_lockEngineMessage)
            {
                // bail out if the stream reader is null
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

                        // There is an important trick to do with how ReadLine is implemented in C#.
                        // The first read must be performed without checking Peek()
                        // as we may get -1 while ReadLine will actually return a message.
                        // We need therefore to ensure that we read at least the first message or risk being stuck.
                        string msg = _strmReader.ReadLine();

                        // special case for the null message.
                        if (msg == null)
                        {
                            if (ProcessNullMessage())
                            {
                                // if returned true, it means the engine is not ready,
                                // so we need to exit the loop.
                                break;
                            }
                        }
                        else
                        {
                            _badMessageCount = 0;

                            // we are not looking for anything in messages that contain "currmove"
                            // so ignore them 
                            if (!msg.Contains(UciCommands.ENG_CURRMOVE))
                            {
                                messages.Add(msg);
                            }

                            // now we will read all messages that are available,
                            // checking Peek() to avoid blocking when all was read 
                            while (_strmReader.Peek() >= 0)
                            {
                                msg = _strmReader.ReadLine();
                                if (msg == null)
                                {
                                    if (ProcessNullMessage())
                                    {
                                        // this should never happen here so just being defensive.
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
        /// </summary>
        /// <param name="rawMessages"></param>
        private void ProcessMessagesList(List<string> rawMessages)
        {
            int index = 0;

            // the flag to set if we find that there is no INFO message with multipv==1,
            // and need to process the messages the "old way", one by one.
            bool noBulkProcessing = false;

            while (index < rawMessages.Count)
            {
                string rawMsg = rawMessages[index];
                EngineLog.Message(rawMsg);
                if (rawMsg.StartsWith(UciCommands.ENG_READY_OK))
                {
                    index++;
                    HandleReadyOk();
                }
                else
                {
                    // insert prefixes needed for the message processing
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
                                EngineLog.Message("INFO messages sequence count: " + rawMessages.Count);
                                List<string> infoMessages = GetInfoMessagesSequence(rawMessages, ref index, ref noBulkProcessing);

                                // the above call will set index to the last processed message
                                if (infoMessages != null)
                                {
                                    index++;
                                    EngineLog.Message("INFO BULK count: " + infoMessages.Count);
                                    EngineInfoMessages?.Invoke(infoMessages);
                                }
                                else
                                {
                                    index++;
                                    EngineMessage?.Invoke(message);
                                }
                            }
                        }
                        else
                        {
                            index++;
                            EngineMessage?.Invoke(message);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Extracts a sequence of consecutive INFO messages starting with multipv 1
        /// from the supplied list.
        /// </summary>
        /// <param name="rawMessages"></param>
        /// <param name="index"></param>
        /// <param name="noBulkProcessing"></param>
        /// <returns></returns>
        private List<string> GetInfoMessagesSequence(List<string> rawMessages, ref int index, ref bool noBulkProcessing)
        {
            int startIndex = index;
            int lastMultiPv_1_Index = -1;

            for (int i = index; i < rawMessages.Count; i++)
            {
                if (!rawMessages[i].StartsWith(UciCommands.ENG_INFO))
                {
                    break;
                }
                index = i;

                if (rawMessages[i].Contains(UciCommands.ENG_MULTIPV_1))
                {
                    lastMultiPv_1_Index = i;
                }
            }

            if (lastMultiPv_1_Index < 0)
            {
                // reset index that will be returned to the caller, as we won't be bulk processing
                index = startIndex;
                noBulkProcessing = true;
                return null;
            }
            else
            {
                // build a list of all consecutive INFO messages starting with multipv 1
                List<string> infoMessages = new List<string>();
                for (int i = lastMultiPv_1_Index; i <= index; i++)
                {
                    string message = InsertIdPrefixes(rawMessages[i]);
                    infoMessages.Add(message);
                    EngineLog.Message("INFO messages list item: " + rawMessages[i]);
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
