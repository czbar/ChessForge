using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace ChessForge
{
    public class EngineGameView : RichTextBuilder
    {
        /// <summary>
        /// Types of paragraphs used in this document.
        /// There can only be one, or none, paragraph of a given type in the document
        /// </summary>
        private enum ParaType
        {
            INTRO,
            GAME_MOVES
        }

        /// <summary>
        /// Maps paragraph types to Paragraph objects
        /// </summary>
        private Dictionary<ParaType, Paragraph> _dictParas = new Dictionary<ParaType, Paragraph>()
        {
            [ParaType.INTRO] = null,
            [ParaType.GAME_MOVES] = null,
        };

        private readonly string _para_intro_ = "para_intro_";
        private readonly string _para_engineoptions_ = "para_engineoptions_";
        private readonly string _para_gamemoves_ = "para_gamemoves_";
        private readonly string _para_moveprompt_ = "para_moveprompt_";

        // id of the last node clicked
        private int _lastClickedNodeId = -1;

        /// <summary>
        /// Creates an instance of this class and sets reference 
        /// to the FlowDocument managed by the object.
        /// </summary>
        /// <param name="rtb"></param>
        public EngineGameView(RichTextBox rtb) : base(rtb)
        {
        }

        /// <summary>
        /// Property referencing definitions of Paragraphs 
        /// </summary>
        override internal Dictionary<string, RichTextPara> RichTextParas { get { return _richTextParas; } }

        private static readonly string STYLE_INTRO = "intro";
        private static readonly string STYLE_ENGINE_OPTIONS = "engine_options";
        private static readonly string STYLE_GAME_MOVES = "moves_main";
        private static readonly string STYLE_MOVE_PROMPT = "move_prompt";
        private static readonly string STYLE_DEFAULT = "default";

        /// <summary>
        /// Layout definitions for paragraphs at different levels.
        /// </summary>
        internal Dictionary<string, RichTextPara> _richTextParas = new Dictionary<string, RichTextPara>()
        {
            [STYLE_INTRO] = new RichTextPara(0, 0, 14, FontWeights.Normal, TextAlignment.Left),
            [STYLE_ENGINE_OPTIONS] = new RichTextPara(20, 0, 12, FontWeights.Normal, TextAlignment.Left),
            [STYLE_GAME_MOVES] = new RichTextPara(10, 20, 16, FontWeights.Bold, TextAlignment.Left),
            [STYLE_MOVE_PROMPT] = new RichTextPara(0, 0, 14, FontWeights.Bold, TextAlignment.Left, Brushes.Green),
            [STYLE_DEFAULT] = new RichTextPara(10, 5, 12, FontWeights.Normal, TextAlignment.Left),
        };

        /// <summary>
        /// Builds the FlowDocument representing the current GameLine.
        /// Inserts dummy (no text) run for the starting position (NodeId == 0)
        /// </summary>
        public void BuildFlowDocumentForGameLine(PieceColor colorForUser)
        {
            HostRtb.Document.Blocks.Clear();
            HostRtb.Document.Blocks.Add(BuildDummyPararaph());

            UpdateIntroParagraph(colorForUser);
            UpdateEngineOptionsParagraph();
            UpdateGameMovesParagraph();
            UpdateMovePromptParagraph(true);
        }

        /// <summary>
        /// Adds the passed move to the game moves paragraph.
        /// </summary>
        /// <param name="nd"></param>
        public void AddMove(TreeNode nd)
        {
            Paragraph para = FindParagraphByName(HostRtb.Document, _para_gamemoves_, false);
            if (para == null)
            {
                para = CreateParagraph(STYLE_GAME_MOVES, true);
                para.Name = _para_gamemoves_;
                HostRtb.Document.Blocks.Add(para);
            }

            BuildNodeTextAndAddToPara(nd, false, para);
        }

        /// <summary>
        /// Creates or updates the intro paragraph with basic info
        /// about this game session. 
        /// </summary>
        /// <param name="colorForUser"></param>
        /// <returns></returns>
        public Paragraph UpdateIntroParagraph(PieceColor colorForUser)
        {
            Paragraph para = FindParagraphByName(HostRtb.Document, _para_intro_, false);
            if (para == null)
            {
                para = CreateParagraph(STYLE_INTRO, true);
                para.Name = _para_intro_;
                HostRtb.Document.Blocks.Add(para);
            }

            para.Inlines.Clear();

            string text1 = Properties.Resources.EngGamePlayingVsEngine + " ";
            Run run1 = new Run(text1);
            para.Inlines.Add(run1);

            string text2 = colorForUser == PieceColor.White ? Properties.Resources.EngGameUserPlaysWhite : Properties.Resources.EngGameUserPlaysBlack;
            Run run2 = new Run(text2 + "\n");
            para.Inlines.Add(run2);

            return para;
        }

        /// <summary>
        /// Creates or updates the engine options paragraph.
        /// </summary>
        /// <returns></returns>
        public Paragraph UpdateEngineOptionsParagraph()
        {
            Paragraph para = FindParagraphByName(HostRtb.Document, _para_engineoptions_, false);
            if (para == null)
            {
                para = CreateParagraph(STYLE_ENGINE_OPTIONS, true);
                para.Name = _para_engineoptions_;
                HostRtb.Document.Blocks.Add(para);
            }

            para.Inlines.Clear();

            para.Inlines.Add(CreateEngineOptionsTitle());
            para.Inlines.Add(CreateEngineOptionsEditLink());
            AddSelectionAccuracyRuns(para);
            AddCentipawnAccuracyRuns(para);

            return para;
        }

        /// <summary>
        /// Creates or updates the move prompt paragraph.
        /// </summary>
        /// <param name="userToMove">If true prompts the user, if false asks to wait for engine's move</param>
        /// <returns></returns>
        public Paragraph UpdateMovePromptParagraph(bool userToMove)
        {
            Paragraph para = FindParagraphByName(HostRtb.Document, _para_moveprompt_, false);
            if (para == null)
            {
                para = CreateParagraph(STYLE_MOVE_PROMPT, true);
                para.Name = _para_moveprompt_;
                HostRtb.Document.Blocks.Add(para);
            }

            para.Inlines.Clear();

            string promptText = userToMove ? Properties.Resources.EngGameYourMove : Properties.Resources.EngGameEngineMove;
            para.Inlines.Add(new Run(promptText));

            return para;
        }

        /// <summary>
        /// Clears the move prompt paragraph e.g. when the game ended
        /// because of checkmate or stalemate.
        /// </summary>
        public void ClearMovePromptParagraph()
        {
            Paragraph para = FindParagraphByName(HostRtb.Document, _para_moveprompt_, false);
            if (para != null)
            {
                para.Inlines.Clear();
            }
        }

        /// <summary>
        /// The user requested that the sides be swapped.
        /// Engine will move now.
        /// </summary>
        public void SwapSides()
        {
            if (EngineGame.CurrentState == EngineGame.GameState.USER_THINKING)
            {
                TreeNode nd = EngineGame.GetLastGameNode();
                AppState.MainWin.MainChessBoard.FlipBoard(MoveUtils.ReverseColor(nd.ColorToMove));
                EngineGame.SwitchToAwaitEngineMove(nd, false);
                EngineGame.EngineColor = nd.ColorToMove;
                AppState.MainWin.EngineGameView.UpdateMovePromptParagraph(false);
                AppState.MainWin.EngineGameView.UpdateIntroParagraph(MoveUtils.ReverseColor(EngineGame.EngineColor));
            }
        }

        /// <summary>
        /// Restart the game from the passed node.
        /// </summary>
        /// <param name="currentNode"></param>
        public void RestartFromNode(bool currentNode)
        {
            if (EngineGame.CurrentState == EngineGame.GameState.USER_THINKING || EngineGame.CurrentState == EngineGame.GameState.IDLE)
            {
                TreeNode startNode = null;
                if (currentNode)
                {
                    startNode = EngineGame.Line.Tree.GetNodeFromNodeId(_lastClickedNodeId);
                }
                else
                {
                    startNode = EngineGame.Line.NodeList.First();
                }

                if (startNode != null && !startNode.Position.IsCheckmate && !startNode.Position.IsStalemate)
                {
                    EngineGame.Line.RollbackToNode(startNode, false);
                    UpdateGameMovesParagraph();
                    AppState.MainWin.DisplayPosition(startNode);
                    AppState.MainWin.MainChessBoard.FlipBoard(startNode.ColorToMove);
                    EngineGame.SwitchToAwaitUserMove(startNode);
                    AppState.MainWin.BoardCommentBox.EngineGameStart();
                    UpdateMovePromptParagraph(true);
                }
            }
        }

        /// <summary>
        /// The view was clicked somewhere.
        /// Reset the _lastClikedNodeId value and configure
        /// the context menu.
        /// </summary>
        /// <param name="e"></param>
        public void GeneralMouseClick(MouseButtonEventArgs e)
        {
            _lastClickedNodeId = -1;
            EnableActiveTreeViewMenus();
        }

        /// <summary>
        /// Configures the context menu.
        /// </summary>
        public void EnableActiveTreeViewMenus()
        {
            bool specificNodeClicked = _lastClickedNodeId >= 0;

            bool isEnabled = EngineGame.CurrentState != EngineGame.GameState.ENGINE_THINKING;

            foreach (var item in AppState.MainWin.UiMncEngineGame.Items)
            {
                if (item is MenuItem)
                {
                    MenuItem menuItem = item as MenuItem;
                    switch (menuItem.Name)
                    {
                        case "UiMnEngGame_SwapSides":
                            menuItem.IsEnabled = isEnabled;
                            break;
                        case "UiMnEngGame_RestartFromMove":
                            menuItem.IsEnabled = isEnabled;
                            menuItem.Visibility = specificNodeClicked ? Visibility.Visible : Visibility.Collapsed;
                            break;
                        case "UiMnEngGame_StartFromInit":
                            menuItem.IsEnabled = isEnabled;
                            break;
                        case "UiMnEngGame_ExitGame":
                            menuItem.IsEnabled = true;
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Creates a run with the title for Engine Configuration paragraph.
        /// </summary>
        /// <returns></returns>
        private Run CreateEngineOptionsTitle()
        {
            Run run = new Run(Properties.Resources.EgEngineConfiguration + " ");
            run.FontWeight = FontWeights.Bold;
            return run;
        }

        /// <summary>
        /// Creates a link for invoking the engine configuration dialog.
        /// </summary>
        /// <returns></returns>
        private Run CreateEngineOptionsEditLink()
        {
            string text = "(" + Properties.Resources.EgChange + ")";
            Run run = new Run(text + "\n");
            run.Foreground = Brushes.Blue;
            run.TextDecorations = TextDecorations.Underline;
            run.MouseDown += EventInvokeEngineOptionsDialog;
            run.Cursor = Cursors.Hand;

            return run;
        }

        /// <summary>
        /// Inserts a run with engine accuracy.
        /// </summary>
        /// <param name="para"></param>
        private void AddSelectionAccuracyRuns(Paragraph para)
        {
            Run runLabel = new Run("      " + Properties.Resources.EgSelectionAccuracyLabel + " ");
            para.Inlines.Add(runLabel);

            string acc = GuiUtilities.ConvertCentipawnsToAccuracy((uint)Configuration.ViableMoveCpDiff).ToString();
            Run runValue = new Run(acc + "%\n");
            runValue.FontWeight = FontWeights.Bold;
            para.Inlines.Add(runValue);
        }

        /// <summary>
        /// Inserts a run with the allowed centipawn difference.
        /// </summary>
        /// <param name="para"></param>
        private void AddCentipawnAccuracyRuns(Paragraph para)
        {
            Run runLabel = new Run("      (" + Properties.Resources.EgCentipawnAccuracy + " ");
            para.Inlines.Add(runLabel);

            string acc = GuiUtilities.ConvertCentipawnsToAccuracy((uint)Configuration.ViableMoveCpDiff).ToString();
            Run runValue = new Run(Configuration.ViableMoveCpDiff.ToString() + ")\n");
            runValue.FontWeight = FontWeights.Bold;
            para.Inlines.Add(runValue);
        }

        /// <summary>
        /// Handles click on the "change" run by invoking the Engien Options dialog. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventInvokeEngineOptionsDialog(object sender, MouseButtonEventArgs e)
        {
            AppState.MainWin.ShowEngineOptionsDialog();
            UpdateEngineOptionsParagraph();
        }


        /// <summary>
        /// Creates or updates the moves paragraph.
        /// </summary>
        /// <returns></returns>
        private Paragraph UpdateGameMovesParagraph()
        {
            Paragraph para = FindParagraphByName(HostRtb.Document, _para_gamemoves_, false);
            if (para == null)
            {
                para = CreateParagraph(STYLE_GAME_MOVES, true);
                para.Name = _para_gamemoves_;
                HostRtb.Document.Blocks.Add(para);
            }

            para.Inlines.Clear();

            ObservableCollection<TreeNode> gameMoves = EngineGame.Line.NodeList;
            for (int i = 1; i < EngineGame.Line.NodeList.Count; i++)
            {
                BuildNodeTextAndAddToPara(EngineGame.Line.NodeList[i], false, para);
            }

            return para;
        }

        /// <summary>
        /// Builds text of an individual node (ply),
        /// creates a new Run and adds it to the paragraph.
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="includeNumber"></param>
        private void BuildNodeTextAndAddToPara(TreeNode nd, bool includeNumber, Paragraph para)
        {
            string nodeText = BuildNodeText(nd, includeNumber);
            Run r = AddMoveRunToParagraph(nd, para, nodeText);
        }

        /// <summary>
        /// Creates a move run with passed text and adds it to the passed paragraph.
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="para"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        private Run AddMoveRunToParagraph(TreeNode nd, Paragraph para, string text)
        {
            Run run = null;

            try
            {
                run = new Run(text.ToString());
                run.Name = RichTextBoxUtilities.NameMoveRun(nd.NodeId);
                run.PreviewMouseDown += EventMoveRunClicked;
                para.Inlines.Add(run);

            }
            catch (Exception ex)
            {
                AppLog.Message("AddRunToParagraph() " + (run == null ? "null" : (run.Name ?? "")) , ex);
            }

            return run;
        }

        /// <summary>
        /// Event handler for the clicked node.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventMoveRunClicked(object sender, MouseButtonEventArgs e)
        {
            Run run = sender as Run;

            if (run != null)
            {
                _lastClickedNodeId = TextUtils.GetIdFromPrefixedString(run.Name);

                if (e.ChangedButton == MouseButton.Right)
                {
                    EnableActiveTreeViewMenus();
                }
            }
        }

        /// <summary>
        /// Builds text for the passed Node.
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="includeNumber"></param>
        /// <returns></returns>
        private string BuildNodeText(TreeNode nd, bool includeNumber)
        {
            if (nd.NodeId == 0)
            {
                return "";
            }

            StringBuilder sb = new StringBuilder();

            if (nd.Position.ColorToMove == PieceColor.Black)
            {
                sb.Append(nd.Position.MoveNumber.ToString() + ".");
            }

            if (nd.Position.ColorToMove == PieceColor.White && includeNumber)
            {
                sb.Append(nd.Position.MoveNumber.ToString() + "...");
            }

            sb.Append(nd.GetGuiPlyText(true));
            sb.Append(" ");
            return sb.ToString();
        }

        /// <summary>
        /// Creates a dummy paragraph to use for spacing before
        /// the first "real" paragraph. 
        /// </summary>
        /// <returns></returns>
        private Paragraph BuildDummyPararaph()
        {
            Paragraph dummy = new Paragraph();
            dummy.Margin = new Thickness(0, 0, 0, 0);
            dummy.Inlines.Add(new Run(""));
            return dummy;
        }

    }
}
