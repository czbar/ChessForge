using ChessForge.TreeViewManagement;
using ChessPosition;
using GameTree;
using System;
using System.Text;

namespace ChessForge
{
    public class TextWriter
    {
        // The tree to export
        private static VariationTree _tree;

        // TODO: move to Constants
        private const char START_COMMENT = '{';

        // TODO: move to Constants
        private const char END_COMMENT = '}';

        /// <summary>
        /// Exports the scoped articles into a text file.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static bool WriteText(string fileName)
        {
            bool result = true;

            StringBuilder sb = new StringBuilder();
            try
            {
                _tree = AppState.ActiveVariationTree;
                sb.AppendLine(BuildPageHeader(_tree, _tree.ContentType));
                LineSectorsBuilder builder = new LineSectorsBuilder();
                builder.BuildLineSectors(_tree.Nodes[0], false);
                foreach (LineSector sector in builder.LineSectors)
                {
                    string lineText = WriteLineSector(sector);
                    sb.AppendLine(lineText);
                }
            }
            catch (Exception ex)
            {
                result = false;
                AppLog.Message("WriteText()", ex);
            }

            return result;
        }

        /// <summary>
        /// Writes a line sector to a string.
        /// </summary>
        /// <param name="sector"></param>
        /// <returns></returns>
        private static string WriteLineSector(LineSector sector)
        {
            StringBuilder sb = new StringBuilder();

            bool firstMove = true;

            string indent = "";
            
            for(int i = 0; i < sector.DisplayLevel; i++)
            {
                indent += "  ";
            }

            for (int i = 0; i < sector.Nodes.Count; i++)
            {
                if (firstMove)
                {
                    sb.Append(indent);
                }

                TreeNode nd = sector.Nodes[i];
                if (nd.NodeId != 0)
                {
                    if (nd.NodeId == LineSector.OPEN_BRACKET)
                    {
                        sb.Append("(");
                    }
                    else if (nd.NodeId == LineSector.CLOSE_BRACKET)
                    {
                        sb.Append(") ");
                    }
                    else
                    {
                        string textForNode = GetTextForNode(nd, ref firstMove, indent);
                        sb.Append(textForNode);
                        if (i < sector.Nodes.Count - 1 && sector.Nodes[i + 1].NodeId != LineSector.CLOSE_BRACKET)
                        {
                            sb.Append(' ');
                        }
                    }
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Builds the header for the page.
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="contentType"></param>
        /// <returns></returns>
        private static string BuildPageHeader(VariationTree tree, GameData.ContentType contentType)
        {
            StringBuilder sb = new StringBuilder();

            if (tree != null)
            {
                switch (contentType)
                {
                    case GameData.ContentType.MODEL_GAME:
                    case GameData.ContentType.EXERCISE:
                        string whitePlayer = tree.Header.GetWhitePlayer(out _);
                        string blackPlayer = tree.Header.GetBlackPlayer(out _);

                        string whitePlayerElo = tree.Header.GetWhitePlayerElo(out _);
                        string blackPlayerElo = tree.Header.GetBlackPlayerElo(out _);

                        bool hasPlayerNames = !(string.IsNullOrWhiteSpace(whitePlayer) && string.IsNullOrWhiteSpace(blackPlayer));

                        if (hasPlayerNames)
                        {
                            sb.AppendLine(BuildPlayerLine(whitePlayer, whitePlayerElo));
                            sb.AppendLine(BuildPlayerLine(blackPlayer, blackPlayerElo));
                        }

                        if (!string.IsNullOrEmpty(tree.Header.GetEventName(out _)))
                        {
                            if (hasPlayerNames)
                            {
                                string round = tree.Header.GetRound(out _);
                                if (!string.IsNullOrWhiteSpace(round))
                                {
                                    round = " (" + round + ")";
                                }
                                else
                                {
                                    round = "";
                                }
                                sb.AppendLine(tree.Header.GetEventName(out _) + round);
                            }
                            else
                            {
                                sb.AppendLine(tree.Header.GetEventName(out _));
                            }
                        }

                        string annotator = tree.Header.GetAnnotator(out _);
                        if (!string.IsNullOrWhiteSpace(annotator))
                        {
                            annotator = "      " + Properties.Resources.Annotator + ": " + annotator;
                            sb.AppendLine(annotator);
                        }

                        string dateForDisplay = TextUtils.BuildDateFromDisplayFromPgnString(tree.Header.GetDate(out _));
                        if (!string.IsNullOrEmpty(dateForDisplay))
                        {
                            sb.AppendLine("      " + Properties.Resources.Date + ": " + dateForDisplay);
                        }

                        string eco = tree.Header.GetECO(out _);
                        string result = tree.Header.GetResult(out _);
                        BuildResultAndEcoLine(eco, result, out string rEco, out string rResult);
                        if (rEco != null || rResult != null)
                        {
                            sb.Append("      ");

                            if (rEco != null)
                            {
                                sb.Append(rEco);
                            }
                            if (rResult != null)
                            {
                                sb.Append(rResult);
                            }

                            sb.AppendLine();
                        }
                        break;
                    case GameData.ContentType.STUDY_TREE:
                        sb.AppendLine(BuildChapterTitle());
                        break;
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Builds the chapter title.
        /// </summary>
        /// <returns></returns>
        private static string BuildChapterTitle()
        {
            StringBuilder sb = new StringBuilder();
            Chapter chapter = WorkbookManager.SessionWorkbook.ActiveChapter;
            if (chapter != null)
            {
                sb.AppendLine(chapter.GetTitle());

                if (!string.IsNullOrWhiteSpace(chapter.GetAuthor()))
                {
                    sb.AppendLine();
                    string rAuthor = "    " + Properties.Resources.Author + ": " + chapter.GetAuthor();
                    sb.AppendLine(rAuthor);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Builds the result and eco line.
        /// </summary>
        /// <param name="eco"></param>
        /// <param name="result"></param>
        /// <param name="rEco"></param>
        /// <param name="rResult"></param>
        private static void BuildResultAndEcoLine(string eco, string result, out string rEco, out string rResult)
        {
            rEco = null;
            rResult = null;
            if (!string.IsNullOrWhiteSpace(eco))
            {
                rEco = eco + "  ";
            }

            if (!string.IsNullOrWhiteSpace(result) && result != "*")
            {
                rResult = "(" + result + ")";
            }
        }

        /// <summary>
        /// Gets the text for a node.
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="isFirstNode"></param>
        /// <param name="indent"></param>
        /// <returns></returns>
        private static string GetTextForNode(TreeNode nd, ref bool isFirstNode, string indent)
        {
            StringBuilder sb = new StringBuilder();

            bool includeNumber = isFirstNode;
            isFirstNode = false;

            if (!string.IsNullOrWhiteSpace(nd.CommentBeforeMove))
            {
                sb.Append(START_COMMENT + nd.CommentBeforeMove + END_COMMENT + " ");
                includeNumber = true;
            }

            sb.Append(MoveUtils.BuildSingleMoveText(nd, includeNumber, true, _tree.MoveNumberOffset));

            string fen = "";
            if (nd.IsDiagram)
            {
                fen = "[" + FenParser.GenerateFenFromPosition(nd.Position, _tree.MoveNumberOffset) + "]";
            }

            if (fen.Length > 0 && nd.IsDiagramPreComment)
            {
                sb.Append(Environment.NewLine + indent + fen + Environment.NewLine);
                isFirstNode = true;
            }
            if (!string.IsNullOrWhiteSpace(nd.Comment))
            {
                sb.Append(" " + START_COMMENT + nd.Comment + END_COMMENT);
            }
            if (fen.Length > 0 && !nd.IsDiagramPreComment)
            {
                sb.Append(Environment.NewLine + indent + fen + Environment.NewLine);
                isFirstNode = true;
            }

            return sb.ToString();
        }

        /// <summary>
        /// Builds a player line.
        /// </summary>
        /// <param name="playerName"></param>
        /// <param name="playerElo"></param>
        /// <returns></returns>
        private static string BuildPlayerLine(string playerName, string playerElo)
        {
            if (string.IsNullOrWhiteSpace(playerName))
            {
                return "NN";
            }

            if (string.IsNullOrWhiteSpace(playerElo))
            {
                return playerName;
            }
            else
            {
                return playerName + " (" + playerElo + ")";
            }
        }

    }
}
