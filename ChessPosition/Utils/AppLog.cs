using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using GameTree;
using System.Windows;
using System.Diagnostics;
using ChessPosition;
using System.Threading;

namespace ChessForge
{
    /// <summary>
    /// LogLevel enums
    /// </summary>
    public enum LogLevel
    {
        BASIC  = 0,
        ERROR  = 1,
        DETAIL = 2,
    }

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

        /// Debug level determining logging detail.
        private static int _debugLevel = 1;

        /// <summary>
        /// Sets the debug level.
        /// </summary>
        public static void Initialize(int debugLevel)
        {
            _debugLevel = debugLevel;
        }

        /// <summary>
        /// Logs a message adding a time stamp.
        /// </summary>
        /// <param name="msg"></param>
        [Conditional("DEBUG")]
        public static void Message(string msg)
        {
            if (_debugLevel == 0)
                return;

            lock (AppLogLock)
            {
                string timeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "  ";
                Log.Add(timeStamp + msg);
            }
        }

        /// <summary>
        /// Logs a debug message if the min debug level is met.
        /// </summary>
        /// <param name="minDebugLevel"></param>
        /// <param name="msg"></param>
        public static void Message(int minDebugLevel, string msg)
        {
            if (_debugLevel < minDebugLevel)
            {
                return;
            }

            lock (AppLogLock)
            {
                string timeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "  ";
                Log.Add(timeStamp + msg);
            }
        }

        /// <summary>
        /// Overload using LogLevel.
        /// </summary>
        /// <param name="level"></param>
        /// <param name="msg"></param>
        public static void Message(LogLevel level, string msg)
        {
            Message((int)level, msg);
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
            if (_debugLevel == 0 || ex == null)
                return;

            lock (AppLogLock)
            {
                string timeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "  ";
                Log.Add(timeStamp + "Exception in " + location + " " + ex.Message);
            }
        }

        /// <summary>
        /// Returns the numbers of threads that are still avaliable.
        /// </summary>
        /// <param name="workerThreads"></param>
        /// <param name="asyncIoThreads"></param>
        [Conditional("DEBUG")]
        public static void LogAvailableThreadsCounts()
        {
            ThreadPool.GetAvailableThreads(out int workerThreads, out int asyncIoThreads);
            Message("ThreadPool available threads: workers=" + workerThreads.ToString() + " async=" + asyncIoThreads.ToString());
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
                sb.AppendLine("Tree Id = " + tree.TreeId.ToString());
                sb.AppendLine();

                for (int i = 0; i < tree.Nodes.Count; i++)
                {
                    TreeNode nd = tree.Nodes[i];
                    sb.Append("Node index = " + i.ToString() + Environment.NewLine);
                    sb.Append("Node Id = " + nd.NodeId.ToString() + Environment.NewLine);
                    sb.Append("Parent Node Id = " + (nd.Parent == null ? "-" : nd.Parent.NodeId.ToString()) + Environment.NewLine);
                    sb.Append("Line Id = " + nd.LineId.ToString() + Environment.NewLine);
                    sb.Append("Move Number = " + nd.MoveNumber.ToString() + Environment.NewLine);
                    sb.Append("Move alg = " + nd.LastMoveAlgebraicNotationWithNag + Environment.NewLine);
                    sb.Append("EnPassant = " + nd.Position.EnPassantSquare.ToString() + Environment.NewLine);
                    sb.Append("InheritedEnPassant = " + nd.Position.InheritedEnPassantSquare.ToString() + Environment.NewLine);
                    if (nd.NodeId != 0 && nd.Position.LastMove.Origin != null && nd.Position.LastMove.Destination != null)
                    {
                        sb.Append("Origin = " + nd.Position.LastMove.Origin.Xcoord.ToString() + " " + nd.Position.LastMove.Origin.Ycoord.ToString() + Environment.NewLine);
                        sb.Append("Destination = " + nd.Position.LastMove.Destination.Xcoord.ToString() + " " + nd.Position.LastMove.Destination.Ycoord.ToString() + Environment.NewLine);
                    }
                    sb.Append("Comment = " + (nd.Comment == null ? "" : nd.Comment) + Environment.NewLine);
                    sb.Append("Nags = " + (nd.Nags == null ? "" : nd.Nags) + Environment.NewLine);
                    sb.Append("IsNewTrainingMove = " + nd.IsNewTrainingMove.ToString() + Environment.NewLine);
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
        /// Writes out a LineSectorTree
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="lineSectors"></param>
        [Conditional("DEBUG")]
        public static void DumpLineSectorTree(string filePath, List<LineSector> lineSectors)
        {
            StringBuilder sb = new StringBuilder();

            if (lineSectors == null)
            {
                sb.Append("LineSectorTree reference is null.");
            }
            else
            {
                for (int i = 0; i < lineSectors.Count; i++)
                {
                    LineSector sector = lineSectors[i];
                    sb.Append("LineSector Id = " + sector.LineSectorId.ToString() + Environment.NewLine);
                    sb.Append("LineSector index = " + i.ToString() + Environment.NewLine);
                    sb.Append("Branch level = " + sector.BranchLevel.ToString() + Environment.NewLine);
                    sb.Append("Display level = " + sector.DisplayLevel.ToString() + Environment.NewLine);
                    sb.Append("Sector type = " + sector.SectorType.ToString() + Environment.NewLine);
                    sb.Append("Parent LineSector Id = " + (sector.Parent == null ? "-" : sector.Parent.LineSectorId.ToString()) + Environment.NewLine);
                    for (int j = 0; j < sector.Children.Count; j++)
                    {
                        sb.Append("    Child " + j.ToString() + " LineSector Id = " + sector.Children[j].LineSectorId.ToString() + Environment.NewLine);
                    }
                    sb.AppendLine();
                    sb.Append("**** Line Sector Nodes ****" + Environment.NewLine);
                    foreach (TreeNode nd in sector.Nodes)
                    {
                        sb.AppendLine();
                        sb.Append("Node Id = " + nd.NodeId.ToString() + Environment.NewLine);
                        sb.Append("Line Id = " + nd.LineId.ToString() + Environment.NewLine);
                        sb.Append("Move Number = " + nd.MoveNumber.ToString() + Environment.NewLine);
                        sb.Append("Move alg = " + nd.LastMoveAlgebraicNotationWithNag + Environment.NewLine);
                    }
                    sb.AppendLine();
                    sb.Append("************" + Environment.NewLine);
                    sb.Append(Environment.NewLine + Environment.NewLine);
                }
            }

            try
            {
                File.WriteAllText(filePath, sb.ToString());
            }
            catch
            {
                MessageBox.Show("DEBUG", "Error writing out LineSectorTree to " + filePath, MessageBoxButton.OK, MessageBoxImage.Error);
            };
        }
    }
}
