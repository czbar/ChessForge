using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
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
        private readonly string _para_gamemoves_ = "para_gamemoves_";
        private readonly string _para_moveprompt_ = "para_moveprompt_";

        private readonly string _run_ = "run_";

        /// <summary>
        /// Creates an instance of this class and sets reference 
        /// to the FlowDocument managed by the object.
        /// </summary>
        /// <param name="doc"></param>
        public EngineGameView(FlowDocument doc) : base(doc)
        {
        }


        /// <summary>
        /// Property referencing definitions of Paragraphs 
        /// </summary>
        override internal Dictionary<string, RichTextPara> RichTextParas { get { return _richTextParas; } }

        private static readonly string STYLE_INTRO = "intro";
        private static readonly string STYLE_GAME_MOVES = "moves_main";
        private static readonly string STYLE_MOVE_PROMPT = "move_prompt";
        private static readonly string STYLE_DEFAULT = "default";

        /// <summary>
        /// Layout definitions for paragraphs at different levels.
        /// </summary>
        internal Dictionary<string, RichTextPara> _richTextParas = new Dictionary<string, RichTextPara>()
        {
            [STYLE_INTRO] = new RichTextPara(0, 0, 14, FontWeights.Normal, new SolidColorBrush(Color.FromRgb(0, 0, 0)), TextAlignment.Left),
            [STYLE_GAME_MOVES] = new RichTextPara(10, 20, 16, FontWeights.Bold, new SolidColorBrush(Color.FromRgb(120, 61, 172)), TextAlignment.Left),
            [STYLE_MOVE_PROMPT] = new RichTextPara(0, 0, 14, FontWeights.Bold, Brushes.Green, TextAlignment.Left, Brushes.Green),
            [STYLE_DEFAULT] = new RichTextPara(10, 5, 12, FontWeights.Normal, new SolidColorBrush(Color.FromRgb(128, 98, 63)), TextAlignment.Left),
        };

        /// <summary>
        /// Builds the FlowDocument representing the current GameLine.
        /// Inserts dummy (no text) run for the starting position (NodeId == 0)
        /// </summary>
        public void BuildFlowDocumentForGameLine(PieceColor colorForUser)
        {
            Document.Blocks.Clear();
            Document.Blocks.Add(BuildDummyPararaph());

            UpdateIntroParagraph(colorForUser);
            UpdateGameMovesParagraph();
            UpdateMovePromptParagraph(true);
        }

        /// <summary>
        /// Adds the passed move to the game moves paragraph.
        /// </summary>
        /// <param name="nd"></param>
        public void AddMove(TreeNode nd)
        {
            Paragraph para = FindParagraphByName(_para_gamemoves_, false);
            if (para == null)
            {
                para = CreateParagraph(STYLE_GAME_MOVES, true);
                para.Name = _para_gamemoves_;
                Document.Blocks.Add(para);
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
            Paragraph para = FindParagraphByName(_para_intro_, false);
            if (para == null)
            {
                para = CreateParagraph(STYLE_INTRO, true);
                para.Name = _para_intro_;
                Document.Blocks.Add(para);
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
        /// Creates or updates the move prompt paragraph.
        /// </summary>
        /// <param name="userToMove">If true prompts the user, if false asks to wait for engine's move</param>
        /// <returns></returns>
        public Paragraph UpdateMovePromptParagraph(bool userToMove)
        {
            Paragraph para = FindParagraphByName(_para_moveprompt_, false);
            if (para == null)
            {
                para = CreateParagraph(STYLE_MOVE_PROMPT, true);
                para.Name = _para_moveprompt_;
                Document.Blocks.Add(para);
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
            Paragraph para = FindParagraphByName(_para_moveprompt_, false);
            if (para != null)
            {
                para.Inlines.Clear();
            }
        }

        /// <summary>
        /// Creates or updates the moves paragraph.
        /// </summary>
        /// <returns></returns>
        private Paragraph UpdateGameMovesParagraph()
        {
            Paragraph para = FindParagraphByName(_para_gamemoves_, false);
            if (para == null)
            {
                para = CreateParagraph(STYLE_GAME_MOVES, true);
                para.Name = _para_gamemoves_;
                Document.Blocks.Add(para);
            }

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
                run.Name = _run_ + nd.NodeId.ToString();
                //r.PreviewMouseDown += EventRunClicked;
                para.Inlines.Add(run);

            }
            catch (Exception ex)
            {
                AppLog.Message("AddRunToParagraph()", ex);
            }

            return run;
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
