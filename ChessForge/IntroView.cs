using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows;
using ChessPosition;
using System.Windows.Controls;
using GameTree;
using System.Windows.Media;


namespace ChessForge
{
    /// <summary>
    /// Encapsulates Intro Tab view with RichTextBox  
    /// </summary>
    public class IntroView
    {
        /// <summary>
        /// The selected node.
        /// If no previous selection, returns the root node.
        /// </summary>
        public TreeNode SelectedNode
        {
            get
            {
                return _selectedNode ?? Nodes[0];
            }
        }

        /// <summary>
        /// The list of diagrams in this view.
        /// </summary>
        private List<IntroViewDiagram> DiagramList = new List<IntroViewDiagram>();

        /// <summary>
        /// List of nodes currently represented in the view.
        /// </summary>
        private List<TreeNode> Nodes
        {
            get => Intro.Tree.Nodes;
        }

        // currently selected node
        private TreeNode _selectedNode;

        // refrence to the RichTextBox of this view.
        private RichTextBox _rtb = AppState.MainWin.UiRtbIntroView;

        // flag to use to prevent unnecessary saving after the load.
        private bool _ignoreTextChange = false;

        /// <summary>
        /// Constructor. Builds the content if not empty.
        /// Initializes data structures.
        /// </summary>
        public IntroView(Chapter parentChapter)
        {
            _rtb.Document.Blocks.Clear();
            _rtb.IsDocumentEnabled = true;

            ParentChapter = parentChapter;

            // set the event handler for text changes.
            _rtb.TextChanged += UiRtbIntroView_TextChanged;
            if (!string.IsNullOrEmpty(Intro.Tree.RootNode.Data))
            {
                _ignoreTextChange = true;
                LoadXAMLContent();
            }
        }

        /// <summary>
        /// Clear the content of the RTB
        /// </summary>
        public void Clear()
        {
            _ignoreTextChange = true;
            _rtb.Document.Blocks.Clear();
        }

        /// <summary>
        /// Chapter for which this view was created.
        /// </summary>
        public Chapter ParentChapter { get; set; }

        /// <summary>
        /// The Intro article shown in the view.
        /// </summary>
        public Article Intro
        {
            get => ParentChapter.Intro;
        }

        /// <summary>
        /// Loads content of the view
        /// </summary>
        public void LoadXAMLContent()
        {
            if (!string.IsNullOrEmpty(Intro.CodedContent))
            {
                string xaml = EncodingUtils.Base64Decode(Intro.CodedContent);
                _rtb.Document = StringToFlowDocument(xaml);
            }
        }

        /// <summary>
        /// Saves the content of the view into the root node of the view.
        /// </summary>
        /// <returns></returns>
        public void SaveXAMLContent()
        {
            string xamlText = XamlWriter.Save(_rtb.Document);
            Nodes[0].Data = EncodingUtils.Base64Encode(xamlText);
        }

        /// <summary>
        /// Inserts new move at the caret.
        /// This function is invoked when the user made a move on the main chessboard.
        /// TODO: try guessing the move number based on what number we see in the preceding paragraphs.
        /// </summary>
        /// <param name="node"></param>
        public void InsertMove(TreeNode node)
        {
            Run rMove = new Run();
            rMove.Text = node.LastMoveAlgebraicNotation;
            // TODO: TRANSLATE at this point
            rMove.Foreground = Brushes.Blue;

            InsertMoveTextBlock(rMove);
        }

        /// <summary>
        /// Invokes the Diagram Setup dialog,
        /// creates GUI objects for the position
        /// and inserts in the document.
        /// </summary>
        public void CreateDiagram()
        {
            try
            {
                TextPointer tp = _rtb.CaretPosition.InsertParagraphBreak();
                Paragraph nextPara = tp.Paragraph;

                _selectedNode = AppState.MainWin.MainChessBoard.DisplayedNode;
                DiagramSetupDialog dlg = new DiagramSetupDialog(SelectedNode)
                {
                    Left = AppState.MainWin.ChessForgeMain.Left + 100,
                    Top = AppState.MainWin.Top + 100,
                    Topmost = false,
                    Owner = AppState.MainWin
                };

                if (dlg.ShowDialog() == true)
                {
                    BoardPosition pos = dlg.PositionSetup;
                    IntroViewDiagram diag = new IntroViewDiagram();
                    Paragraph para = BuildDiagramParagraph(diag, pos);
                    diag.Chessboard.DisplayPosition(null, pos);

                    TreeNode node = new TreeNode(null, "", 0);
                    node.Position = new BoardPosition(SelectedNode.Position);
                    node.Position = pos;
                    diag.Node = node;

                    DiagramList.Add(diag);
                    Nodes.Add(node);
                    _selectedNode = node;

                    AppState.MainWin.DisplayPosition(node);

                    AppState.IsDirty = true;

                    _rtb.Document.Blocks.InsertBefore(nextPara, para);
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("CreateDiagram()", ex);
            }
        }

        /// <summary>
        /// Inserts a Run into a TextBlock that is then inserted into the Document.
        /// </summary>
        /// <param name="run"></param>
        private void InsertMoveTextBlock(Run run)
        {
            // TEMP just break the paragraph
            // later on, set all Inlines aside and rebuild inserting the text block in the right place
            TextPointer tp = _rtb.CaretPosition.InsertParagraphBreak();
            Paragraph nextPara = tp.Paragraph;

            TextBlock newTextBlock = new TextBlock();
            newTextBlock.Inlines.Add(run);

            Paragraph para = new Paragraph();
            para.Inlines.Add(run);

            _rtb.Document.Blocks.InsertBefore(nextPara, para);
            AppState.IsDirty = true;

#if false
            // If the current position is a Run, insert the new run after it.
            // If it is a Paragraph, insert it as a new Run in the paragraph.
            // Otherwise create a new paragraph.

            Run runToInsertAfter = null;
            Paragraph paraToInsertAfter = null;
            Paragraph paraToInsertBefore = null;

            if (caretPosition.Parent.GetType() != typeof(Run))
            {
                runToInsertAfter = caretPosition.Parent as Run;
            }
            else if (caretPosition.Parent.GetType() != typeof(Run))
            {
                paraToInsertAfter = caretPosition.Paragraph;
            }

            if (runToInsertAfter == null && paraToInsertAfter == null)
            {
                paraToInsertBefore = caretPosition.InsertParagraphBreak().Paragraph;
            }

            // Create the new TextBlock
            TextBlock newTextBlock = new TextBlock();
            newTextBlock.Inlines.Add(run);

            // Insert the new TextBlock after the current Paragraph
            Paragraph paragraph = (caretPosition.Parent as Run).Parent as Paragraph;
            paragraph.Inlines.Add(newTextBlock);

            paragraph.Inlines.InsertAfter(paragraph.Inlines.FirstInline, paraToInsertAfter);

            //Paragraph para2 = new Paragraph();

            //TextBlock tBlock = new TextBlock(run);
            //tBlock.Background = ChessForgeColors.INTRO_MOVE_BACKGROUND;
            //para2.Inlines.Add(tBlock);
            //// add the new Paragraph to the FlowDocument
            //_rtb.Document.Blocks.InsertBefore(paraToInsertBefore, para2);
#endif
        }

        /// <summary>
        /// Builds a paragraph with the diagram.
        /// </summary>
        /// <param name="diag"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        private Paragraph BuildDiagramParagraph(IntroViewDiagram diag, BoardPosition pos)
        {
            Paragraph para = new Paragraph();
            para.Margin = new Thickness(20, 20, 0, 20);
            para.Name = "Chessboard";

            Canvas canvas = SetupDiagramCanvas();
            Image imgChessBoard = CreateChessBoard(canvas, diag);
            canvas.Children.Add(imgChessBoard);
            Viewbox viewBox = SetupDiagramViewbox(canvas);

            InlineUIContainer uIContainer = new InlineUIContainer();
            uIContainer.Child = viewBox;
            para.Inlines.Add(uIContainer);

            return para;
        }

        /// <summary>
        /// Creates the chessboard control.
        /// </summary>
        /// <param name="canvas"></param>
        /// <returns></returns>
        private Image CreateChessBoard(Canvas canvas, IntroViewDiagram diag)
        {
            Image imgChessBoard = new Image();
            imgChessBoard.Margin = new Thickness(5, 5, 5, 5);
            imgChessBoard.Source = ChessBoards.ChessBoardGreySmall;

            diag.Chessboard = new ChessBoardSmall(canvas, imgChessBoard, null, null, false, false);
            AlignExerciseAndMainBoards();

            return imgChessBoard;
        }

        /// <summary>
        /// Sets the "passive" exercise board to the same
        /// orientation as the main board.
        /// </summary>
        public void AlignExerciseAndMainBoards()
        {
        }

        /// <summary>
        /// Creates a Canvas for the chessboard. 
        /// </summary>
        /// <returns></returns>
        private Canvas SetupDiagramCanvas()
        {
            Canvas canvas = new Canvas();
            canvas.Background = Brushes.Black;
            canvas.Width = 250;
            canvas.Height = 250;

            return canvas;
        }

        /// <summary>
        /// Creates a Viewbox for the chessboard
        /// </summary>
        /// <param name="canvas"></param>
        /// <returns></returns>
        private Viewbox SetupDiagramViewbox(Canvas canvas)
        {
            Viewbox viewBox = new Viewbox();
            viewBox.Child = canvas;
            viewBox.Width = 250;
            viewBox.Height = 250;
            viewBox.Visibility = Visibility.Visible;

            return viewBox;
        }

        /// <summary>
        /// Creates a FlowDocument from XAML string.
        /// </summary>
        /// <param name="xamlString"></param>
        /// <returns></returns>
        private FlowDocument StringToFlowDocument(string xamlString)
        {
            return XamlReader.Parse(xamlString) as FlowDocument;
        }

        /// <summary>
        /// Handles the Text Change event. Sets the Workbook's dirty flag.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiRtbIntroView_TextChanged(object sender, TextChangedEventArgs e)
        {
            //TODO: get Paragraph from CaretPosition to see if we are deleting a diagram
            if (_ignoreTextChange)
            {
                _ignoreTextChange = false;
            }
            else
            {
                AppState.IsDirty = true;
            }
        }

    }
}
