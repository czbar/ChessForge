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
            if (Configuration.DebugMode == 0)
                return;

            lock (AppLogLock)
            {
                string timeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "  ";
                Log.Add(timeStamp + msg);
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
        /// Writes out the WorkbookTree
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="tree"></param>
        [Conditional("DEBUG")]
        public static void DumpWorkbookTree(string filePath, WorkbookTree tree)
        {
            StringBuilder sb = new StringBuilder();

            if (tree == null)
            {
                sb.Append("WorkbookTree reference is null.");
            }
            else
            {
                for (int i = 0; i < tree.Nodes.Count; i++)
                {
                    TreeNode nd = tree.Nodes[i];
                    sb.Append("Node index = " + i.ToString() + Environment.NewLine);
                    sb.Append("Node Id = " + nd.NodeId.ToString() + Environment.NewLine);
                    sb.Append("Parent Node Id = " + (nd.Parent == null ? "-" : nd.Parent.NodeId.ToString()) + Environment.NewLine);
                    sb.Append("Move alg = " + nd.LastMoveAlgebraicNotation + Environment.NewLine);
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
                MessageBox.Show("DEBUG", "Error writing out Workbook Tree to " + filePath, MessageBoxButton.OK, MessageBoxImage.Error);
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
            sb.Append("IsTrainingInProgress = " + TrainingState.IsTrainingInProgress.ToString() + Environment.NewLine);
            sb.Append("TrainingMode = " + TrainingState.CurrentState.ToString() + Environment.NewLine);
            sb.Append("EvaluationMode = " + AppStateManager.CurrentEvaluationMode.ToString() + Environment.NewLine);
            sb.Append("GameState = " + EngineGame.CurrentState.ToString() + Environment.NewLine);

            sb.Append(Environment.NewLine);

            sb.Append("Timer States" + Environment.NewLine);
            sb.Append("============" + Environment.NewLine);

            AppTimers timers = AppStateManager.MainWin.Timers;

            bool isMessagePollEnabled = EngineMessageProcessor.ChessEngineService.IsMessagePollEnabled();
            sb.Append("ENGINE MESSAGE POLL" + ": IsEnabled = "
                + isMessagePollEnabled.ToString() + Environment.NewLine);
            sb.Append(AppTimers.TimerId.CHECK_FOR_USER_MOVE.ToString() + ": IsEnabled = " 
                + timers.IsEnabled(AppTimers.TimerId.CHECK_FOR_USER_MOVE).ToString() + Environment.NewLine);
            sb.Append(AppTimers.TimerId.CHECK_FOR_TRAINING_WORKBOOK_MOVE_MADE.ToString() + ": IsEnabled = " 
                + timers.IsEnabled(AppTimers.TimerId.CHECK_FOR_TRAINING_WORKBOOK_MOVE_MADE).ToString() + Environment.NewLine);
            sb.Append(AppTimers.TimerId.EVALUATION_LINE_DISPLAY.ToString() + ": IsEnabled = " 
                + timers.IsEnabled(AppTimers.TimerId.EVALUATION_LINE_DISPLAY).ToString() + Environment.NewLine);
            sb.Append(AppTimers.TimerId.REQUEST_WORKBOOK_MOVE.ToString() + ": IsEnabled = " 
                + timers.IsEnabled(AppTimers.TimerId.REQUEST_WORKBOOK_MOVE).ToString() + Environment.NewLine);
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
