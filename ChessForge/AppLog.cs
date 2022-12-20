using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using GameTree;
using System.Windows;
using System.Runtime.InteropServices;
using System.Diagnostics;
using ChessPosition;

namespace ChessForge
{
    /// <summary>
    /// Logging class to be used in debug mode.
    /// </summary>
    public class AppLog
    {
        /// <summary>
        /// Lock object for accessing log the log
        /// data.
        /// </summary>
        public static object AppLogLock = new object();

        /// <summary>
        /// List of logged messages.
        /// </summary>
        private static List<string> Log = new List<string>();

        /// <summary>
        /// Logs a message adding a time stamp.
        /// </summary>
        /// <param name="msg"></param>
        [Conditional("DEBUG")]
        public static void Message(string msg)
        {
            if (Configuration.DebugLevel == 0)
                return;

            lock (AppLogLock)
            {
                string timeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "  ";
                Log.Add(timeStamp + msg);
            }
        }

        /// <summary>
        /// Logs TreeNode details including the board position.
        /// </summary>
        /// <param name="nd"></param>
        public static void TreeNodeDetails(TreeNode nd)
        {
            if (nd != null)
            {
                Message("");
            }

            Message("*** BEGIN TreeNode details:");
            if (nd == null)
            {
                Message("The TreeNode is null.");
                Message("*** END TreeNode:");
            }
            else
            {
                Message("NodeId=" + nd.NodeId.ToString());
                Message("LastMove=" + (nd.LastMoveAlgebraicNotationWithNag ?? ""));
                Message("ColorToMove=" + (nd.ColorToMove.ToString()));
                Message("MoveNumber=" + nd.MoveNumber.ToString());

                LogPosition(nd.Position);
                Message("*** END TreeNode:");
                Message("");
            }
        }

        /// <summary>
        /// Logs position in readable form.
        /// </summary>
        /// <param name="position"></param>
        public static void LogPosition(BoardPosition position)
        {
            List<string> list = DebugUtils.BuildStringForPosition(position);
            foreach (string item in list)
            {
                Message(item);
            }
        }

        /// <summary>
        /// Logs a message with text made of the passed
        /// text (typically a function name) and an Excpetion object.
        /// </summary>
        /// <param name="location">E.g. a function name</param>
        /// <param name="ex"></param>
        [Conditional("DEBUG")]
        public static void Message(string location, Exception ex)
        {
            if (Configuration.DebugLevel == 0 || ex == null)
                return;

            lock (AppLogLock)
            {
                string timeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "  ";
                Log.Add(timeStamp + "Exception in " + location + " " + ex.Message);
            }
        }

        /// <summary>
        /// Writes the logged messages out to a file.
        /// </summary>
        /// <param name="logFileDistnct"></param>
        [Conditional("DEBUG")]
        public static void Dump(string filePath)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string s in Log)
            {
                sb.Append(s + Environment.NewLine);
            }
            try
            {
                // this may fail if we try to write to the system folder e.g. because the app was invoked via menu association.
                File.WriteAllText(filePath, sb.ToString());
            }
            catch { };
            Log.Clear();
        }

        /// <summary>
        /// Writes out a VariationTree
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="tree"></param>
        [Conditional("DEBUG")]
        public static void DumpVariationTree(string filePath, VariationTree tree)
        {
            StringBuilder sb = new StringBuilder();

            if (tree == null)
            {
                sb.Append("VariationTree reference is null.");
            }
            else
            {
                for (int i = 0; i < tree.Nodes.Count; i++)
                {
                    TreeNode nd = tree.Nodes[i];
                    sb.Append("Node index = " + i.ToString() + Environment.NewLine);
                    sb.Append("Node Id = " + nd.NodeId.ToString() + Environment.NewLine);
                    sb.Append("Parent Node Id = " + (nd.Parent == null ? "-" : nd.Parent.NodeId.ToString()) + Environment.NewLine);
                    sb.Append("Move alg = " + nd.LastMoveAlgebraicNotationWithNag + Environment.NewLine);
                    sb.Append("EnPassant = " + nd.Position.EnPassantSquare.ToString() + Environment.NewLine);
                    sb.Append("InheritedEnPassant = " + nd.Position.InheritedEnPassantSquare.ToString() + Environment.NewLine);
                    if (nd.NodeId != 0)
                    {
                        sb.Append("Origin = " + nd.Position.LastMove.Origin.Xcoord.ToString() + " " + nd.Position.LastMove.Origin.Ycoord.ToString() + Environment.NewLine);
                        sb.Append("Destination = " + nd.Position.LastMove.Destination.Xcoord.ToString() + " " + nd.Position.LastMove.Destination.Ycoord.ToString() + Environment.NewLine);
                    }
                    sb.Append("Comment = " + (nd.Comment == null ? "" : nd.Comment) + Environment.NewLine);
                    sb.Append("IsNewTrainingMove = " + nd.IsNewTrainingMove.ToString());
                    sb.Append("Arrows = " + (nd.Arrows == null ? "" : nd.Arrows) + Environment.NewLine);
                    sb.Append("Circles = " + (nd.Circles == null ? "" : nd.Circles) + Environment.NewLine);
                    sb.Append("DistanceToLeaf = " + nd.DistanceToLeaf.ToString() + Environment.NewLine);
                    sb.Append("DistanceToFork = " + nd.DistanceToNextFork.ToString() + Environment.NewLine);
                    for (int j = 0; j < nd.Children.Count; j++)
                    {
                        sb.Append("    Child " + j.ToString() + " Node Id = " + nd.Children[j].NodeId.ToString() + Environment.NewLine);
                    }
                    sb.Append(Environment.NewLine + Environment.NewLine);
                }
            }

            try
            {
                File.WriteAllText(filePath, sb.ToString());
            }
            catch
            {
                MessageBox.Show("DEBUG", "Error writing out Variation Tree to " + filePath, MessageBoxButton.OK, MessageBoxImage.Error);
            };
        }

        /// <summary>
        /// Writes out states and timers.
        /// </summary>
        /// <param name="filePath"></param>
        [Conditional("DEBUG")]
        public static void DumpStatesAndTimers(string filePath)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("Application States" + Environment.NewLine);
            sb.Append("==================" + Environment.NewLine);
            sb.Append("Learning mode = " + AppStateManager.CurrentLearningMode.ToString() + Environment.NewLine);
            sb.Append("IsTrainingInProgress = " + TrainingSession.IsTrainingInProgress.ToString() + Environment.NewLine);
            sb.Append("TrainingMode = " + TrainingSession.CurrentState.ToString() + Environment.NewLine);
            sb.Append("EvaluationMode = " + AppStateManager.CurrentEvaluationMode.ToString() + Environment.NewLine);
            sb.Append("GameState = " + EngineGame.CurrentState.ToString() + Environment.NewLine);

            sb.Append(Environment.NewLine);

            sb.Append("Timer States" + Environment.NewLine);
            sb.Append("============" + Environment.NewLine);

            AppTimers timers = AppStateManager.MainWin.Timers;

            bool isMessagePollEnabled = EngineMessageProcessor.ChessEngineService.IsMessagePollEnabled();
            sb.Append("ENGINE MESSAGE POLL" + ": IsEnabled = "
                + isMessagePollEnabled.ToString() + Environment.NewLine);

            sb.Append(AppTimers.TimerId.AUTO_SAVE.ToString() + ": IsEnabled = "
                + timers.IsEnabled(AppTimers.TimerId.AUTO_SAVE).ToString() + Environment.NewLine);
            sb.Append(Environment.NewLine);
            sb.Append(AppTimers.TimerId.EVALUATION_LINE_DISPLAY.ToString() + ": IsEnabled = "
                + timers.IsEnabled(AppTimers.TimerId.EVALUATION_LINE_DISPLAY).ToString() + Environment.NewLine);
            sb.Append(Environment.NewLine);
            sb.Append(AppTimers.TimerId.CHECK_FOR_USER_MOVE.ToString() + ": IsEnabled = " 
                + timers.IsEnabled(AppTimers.TimerId.CHECK_FOR_USER_MOVE).ToString() + Environment.NewLine);
            sb.Append(AppTimers.TimerId.CHECK_FOR_TRAINING_WORKBOOK_MOVE_MADE.ToString() + ": IsEnabled = " 
                + timers.IsEnabled(AppTimers.TimerId.CHECK_FOR_TRAINING_WORKBOOK_MOVE_MADE).ToString() + Environment.NewLine);
            sb.Append(AppTimers.TimerId.REQUEST_WORKBOOK_MOVE.ToString() + ": IsEnabled = " 
                + timers.IsEnabled(AppTimers.TimerId.REQUEST_WORKBOOK_MOVE).ToString() + Environment.NewLine);
            sb.Append(Environment.NewLine);

            sb.Append(AppTimers.TimerId.SHOW_TRAINING_PROGRESS_POPUP_MENU.ToString() + ": IsEnabled = " 
                + timers.IsEnabled(AppTimers.TimerId.SHOW_TRAINING_PROGRESS_POPUP_MENU).ToString() + Environment.NewLine);
            sb.Append(AppTimers.TimerId.FLASH_ANNOUNCEMENT.ToString() + ": IsEnabled = " 
                + timers.IsEnabled(AppTimers.TimerId.FLASH_ANNOUNCEMENT).ToString() + Environment.NewLine);
            sb.Append(AppTimers.TimerId.APP_START.ToString() + ": IsEnabled = " 
                + timers.IsEnabled(AppTimers.TimerId.APP_START).ToString() + Environment.NewLine);

            try
            {
                File.WriteAllText(filePath, sb.ToString());
            }
            catch
            {
                MessageBox.Show("DEBUG", "Error writing out Workbook Tree to " + filePath, MessageBoxButton.OK, MessageBoxImage.Error);
            };
        }

    }
}
