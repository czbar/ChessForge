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
using System.Windows.Input;
using System.Windows.Media.TextFormatting;

namespace ChessForge
{
    /// <summary>
    /// Encapsulates Intro Tab view with RichTextBox  
    /// </summary>
    public class IntroView : RichTextBuilder
    {
        /// <summary>
        /// Not needed in this class
        /// but required for the calss dervied from RichTextBuilder.
        /// </summary>
        internal override Dictionary<string, RichTextPara> RichTextParas => throw new NotImplementedException();

        /// <summary>
        /// The selected node.
        /// If no previous selection, returns the root node.
        /// </summary>
        public TreeNode SelectedNode
        {
            get => _selectedNode ?? Nodes[0];
        }

        /// <summary>
        /// The list of diagrams in this view.
        /// </summary>
        private List<IntroViewDiagram> DiagramList = new List<IntroViewDiagram>();

        private bool _textDirty = false;

        /// <summary>
        /// Names and prefixes for xaml elements.
        /// </summary>
        private readonly string _run_move_ = "run_move_";
        private readonly string _tb_move_ = "tb_move_";
        private readonly string _uic_move_ = "uic_move_";
        private readonly string _para_diagram_ = "para_diag_";

        // current highest run id (it is 0 initially, because we have the root node)
        private int _maxRunId = 0;

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
        public IntroView(FlowDocument doc, Chapter parentChapter) : base(doc)
        {
            Document.Blocks.Clear();

            ParentChapter = parentChapter;

            // set the event handler for text changes.
            _rtb.TextChanged += UiRtbIntroView_TextChanged;
            if (!string.IsNullOrEmpty(Intro.Tree.RootNode.Data))
            {
                _ignoreTextChange = true;
                LoadXAMLContent();
                SetEventHandlers();
            }
            Nodes[0].Position = PositionUtils.SetupStartingPosition();
            foreach (var node in Nodes)
            {
                if (node.NodeId > _maxRunId)
                {
                    _maxRunId = node.NodeId;
                }
            }
        }

        /// <summary>
        /// Clear the content of the RTB
        /// </summary>
        public void Clear()
        {
            _ignoreTextChange = true;
            Document.Blocks.Clear();
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
                StringToFlowDocument(xaml);
                _rtb.Document = Document;
            }
        }

        /// <summary>
        /// Saves the content of the view into the root node of the view.
        /// </summary>
        /// <returns></returns>
        public void SaveXAMLContent()
        {
            if (_textDirty)
            {
                string xamlText = XamlWriter.Save(Document);
                Nodes[0].Data = EncodingUtils.Base64Encode(xamlText);
                RemoveUnusedNodes();
            }

            _textDirty = false;
        }

        /// <summary>
        /// Remove nodes that are in the Nodes list but not
        /// used in XAML.
        /// Don't delete NodeId == 0.
        /// </summary>
        private void RemoveUnusedNodes()
        {
            List<int> activeNodes = new List<int>();

            // get all active node ids
            foreach (Block block in Document.Blocks)
            {
                if (block is Paragraph)
                {
                    foreach (Inline inl in (block as Paragraph).Inlines)
                    {
                        if (inl is InlineUIContainer)
                        {
                            activeNodes.Add(TextUtils.GetIdFromPrefixedString(inl.Name));
                        }
                    }
                }
            }

            List<TreeNode> nodesToRemove = new List<TreeNode>();
            // remove those whose IDs are not in the list above.
            foreach (TreeNode nd in Intro.Tree.Nodes)
            {
                if (activeNodes.Find(x => x == nd.NodeId) == 0 && nd.NodeId != 0)
                {
                    nodesToRemove.Add(nd);
                }
            }
            foreach (TreeNode nd in nodesToRemove)
            {
                Intro.Tree.Nodes.Remove(nd);
            }
        }

        /// <summary>
        /// Inserts new move at the caret.
        /// This function is invoked when the user made a move on the main chessboard.
        /// TODO: try guessing the move number based on what number we see in the preceding paragraphs.
        /// </summary>
        /// <param name="node"></param>
        public void InsertMove(TreeNode node)
        {
            if (string.IsNullOrEmpty(node.LastMoveAlgebraicNotation))
            {
                return;
            }

            _selectedNode = node;
            int nodeId = AddNode(node);

            Run rMove = new Run();
            rMove.Name = _run_move_ + nodeId.ToString();
            rMove.Foreground = Brushes.Blue;
            rMove.FontWeight = FontWeights.Bold;

            InsertMoveTextBlock(rMove, node);
        }

        /// <summary>
        /// Returns the Node with the passed id.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        private TreeNode GetNodeById(int nodeId)
        {
            return Nodes.FirstOrDefault(x => x.NodeId == nodeId);
        }

        /// <summary>
        /// Handles the click move event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventMoveClicked(object sender, MouseButtonEventArgs e)
        {
            if (e.Source is Run)
            {
                try
                {
                    Run r = e.Source as Run;
                    int nodeId = TextUtils.GetIdFromPrefixedString(r.Name);
                    TreeNode nd = GetNodeById(nodeId);
                    if (nd != null)
                    {
                        _selectedNode = nd;
                        string uicName = _uic_move_ + nodeId.ToString();
                        Inline inlClicked = FindInlineByName(uicName);
                        _rtb.CaretPosition = inlClicked.ElementEnd;
                        AppState.MainWin.DisplayPosition(nd);

                        if (e.ChangedButton == MouseButton.Left && e.ClickCount == 2)
                        {
                            IntroMoveDialog dlg = new IntroMoveDialog(nd)
                            {
                                Left = AppState.MainWin.Left + 100,
                                Top = AppState.MainWin.Top + 100,
                                Topmost = false,
                                Owner = AppState.MainWin
                            };

                            if (dlg.ShowDialog() == true)
                            {
                                _textDirty = true;
                                AppState.IsDirty = true;

                                nd.LastMoveAlgebraicNotation = dlg.MoveText;
                                r.Text = " " + dlg.MoveText + " ";
                            }
                        }
                    }
                }
                catch { }
            }
        }

        /// <summary>
        /// The diagram paragraph was clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventDiagramClicked(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (sender is Paragraph)
                {
                    Paragraph para = sender as Paragraph;
                    _rtb.CaretPosition = para.ContentStart;

                    string s = para.Name;
                    int nodeId = TextUtils.GetIdFromPrefixedString(s);
                    TreeNode nd = GetNodeById(nodeId);
                    if (nd != null)
                    {
                        _selectedNode = nd;
                        AppState.MainWin.DisplayPosition(nd);

                        if (e.ChangedButton == MouseButton.Left && e.ClickCount == 2)
                        {
                        }
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Sets event handlers for the move Runs
        /// and diagram Paragraphs.
        /// </summary>
        private void SetEventHandlers()
        {
            foreach (Block block in Document.Blocks)
            {
                if (block is Paragraph)
                {
                    Paragraph p = (Paragraph)block;
                    if (p.Name.StartsWith(_para_diagram_))
                    {
                        p.MouseDown += EventDiagramClicked;
                    }
                    foreach (Inline inl in p.Inlines)
                    {
                        if (inl is InlineUIContainer && inl.Name.StartsWith(_uic_move_))
                        {
                            ((InlineUIContainer)inl).MouseDown += EventMoveClicked;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Finds the last InlineUIContainer before the passed one
        /// with a Run that has a text looking
        /// like starting with a move number and then parses it.
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        private int FindLastMoveNumber(InlineUIContainer uicCurrent, out PieceColor color)
        {
            int number = -1;
            color = PieceColor.None;

            bool done = false;

            try
            {
                foreach (Block block in Document.Blocks)
                {
                    if (done)
                    {
                        break;
                    }

                    if (block is Paragraph)
                    {
                        foreach (Inline inl in (block as Paragraph).Inlines)
                        {
                            if (inl is InlineUIContainer)
                            {
                                if (inl == uicCurrent)
                                {
                                    done = true;
                                    break;
                                }

                                InlineUIContainer uic = inl as InlineUIContainer;
                                TextBlock tb = uic.Child as TextBlock;
                                if (tb != null)
                                {
                                    foreach (Inline tbLinline in tb.Inlines)
                                    {
                                        if (tbLinline is Run)
                                        {
                                            int no = MoveUtils.ExtractMoveNumber((tbLinline as Run).Text, out PieceColor pc);
                                            if (no >= 0)
                                            {
                                                number = no;
                                                color = pc;
                                            }
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                return -1;
            }

            return number;
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
                    TreeNode node = new TreeNode(null, "", 0);
                    node.Position = new BoardPosition(pos);

                    int node_id = AddNode(node);
                    _selectedNode = node;

                    IntroViewDiagram diag = new IntroViewDiagram();
                    Paragraph para = BuildDiagramParagraph(diag, node);
                    diag.Chessboard.DisplayPosition(node, false);
                    diag.Node = node;

                    DiagramList.Add(diag);

                    AppState.MainWin.DisplayPosition(node);

                    AppState.IsDirty = true;

                    Document.Blocks.InsertBefore(nextPara, para);
                    para.MouseDown += EventDiagramClicked;
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("CreateDiagram()", ex);
            }
        }

        /// <summary>
        /// Inserts a move's Run into a TextBlock that is then inserted into an InlineUIContainer
        /// and finally in the Document.
        /// This is called after the user made a move on the main board.
        /// We determine the place to insert the new move in, and are guessing the number 
        /// to prefix it with.
        /// </summary>
        /// <param name="run"></param>
        private void InsertMoveTextBlock(Run run, TreeNode node)
        {
            try
            {
                GetMoveInsertionPlace(out Paragraph paraToInsertIn, out Inline inlineToInsertBefore);

                TextBlock tbMove = new TextBlock();
                tbMove.Name = _tb_move_ + node.NodeId.ToString();
                tbMove.Inlines.Add(run);

                InlineUIContainer uic = new InlineUIContainer();
                uic.Name = _uic_move_ + node.NodeId.ToString();
                uic.Child = tbMove;
                uic.MouseDown += EventMoveClicked;


                if (inlineToInsertBefore == null)
                {
                    paraToInsertIn.Inlines.Add(uic);
                }
                else
                {
                    paraToInsertIn.Inlines.InsertBefore(inlineToInsertBefore, uic);
                }

                // only now we will build the text so we get the number right
                run.Text = " " + BuildMoveRunText(node, uic) + " ";

                // set caret to the end of the new move
                _rtb.CaretPosition = uic.ElementEnd;

                AppState.IsDirty = true;
            }
            catch (Exception ex)
            {
                AppLog.Message("InsertMoveTextBlock()", ex);
            }
        }

        private string BuildMoveRunText(TreeNode node, InlineUIContainer iuc)
        {
            int moveNo = FindLastMoveNumber(iuc, out PieceColor color);

            PieceColor moveColor = MoveUtils.ReverseColor(node.ColorToMove);

            if (moveNo == -1)
            {
                moveNo = 1;
            }
            else
            {
                if (moveColor != color && moveColor == PieceColor.White)
                {
                    moveNo++;
                }
            }

            string res = moveNo.ToString() + (moveColor == PieceColor.Black ? "... " : ". ") + node.LastMoveAlgebraicNotation;
            res = Languages.MapPieceSymbols(res);
            // TODO: TRANSLATE back when saving ?! Detect if this has a move?
            node.LastMoveAlgebraicNotation = res;

            return res;
        }

        /// <summary>
        /// Based on the current caret position and selaction (if any)
        /// determine the paragraph in which to insert the new move
        /// and an Inline before which to insert it.
        /// </summary>
        /// <param name="para"></param>
        /// <param name="insertBefore"></param>
        private void GetMoveInsertionPlace(out Paragraph para, out Inline insertBefore)
        {
            TextSelection selection = _rtb.Selection;
            if (!selection.IsEmpty)
            {
                // if there is a selection we want to insert after it.
                // e.g. we just highlighted the move by clicking on it and, intuitively,
                // want the next move to come after it.
                _rtb.CaretPosition = selection.End;
            }

            // if caret is inside a Run, split it and return the second part
            insertBefore = SplitRun(_rtb);
            if (insertBefore != null && insertBefore.Parent is Paragraph)
            {
                para = insertBefore.Parent as Paragraph;
            }
            else
            {
                TextPointer tp = _rtb.CaretPosition;
                para = tp.Paragraph;

                DependencyObject inl = tp.GetAdjacentElement(LogicalDirection.Forward);
                if (inl != null && inl is Inline && para != null)
                {
                    insertBefore = inl as Inline;
                }
                else
                {
                    // there is no Inline ahead so just append to the current paragraph
                    // or create a new one if null
                    insertBefore = null;
                    if (para == null)
                    {
                        para = _rtb.CaretPosition.InsertParagraphBreak().Paragraph;
                    }

                    if (tp.Paragraph != null)
                    {
                        para = tp.Paragraph;
                        insertBefore = null;
                    }
                }
            }
        }

        /// <summary>
        /// Builds a paragraph with the diagram.
        /// </summary>
        /// <param name="diag"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        private Paragraph BuildDiagramParagraph(IntroViewDiagram diag, TreeNode nd)
        {
            Paragraph para = new Paragraph();
            para.Margin = new Thickness(20, 20, 0, 20);
            para.Name = _para_diagram_ + nd.NodeId.ToString();

            Canvas canvas = SetupDiagramCanvas();
            Image imgChessBoard = CreateChessBoard(canvas, diag);
            canvas.Children.Add(imgChessBoard);
            Viewbox viewBox = SetupDiagramViewbox(canvas);

            InlineUIContainer uic = new InlineUIContainer();
            uic.Child = viewBox;
            uic.Name = _uic_move_ + nd.NodeId.ToString();
            para.Inlines.Add(uic);

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
        /// Adds a new Node to the list and increments max run id.
        /// </summary>
        /// <param name="nd"></param>
        /// <returns></returns>
        private int AddNode(TreeNode nd)
        {
            _maxRunId++;
            nd.NodeId = _maxRunId;
            Nodes.Add(nd);
            return nd.NodeId;
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
            try
            {
                Document = XamlReader.Parse(xamlString) as FlowDocument;
            }
            catch (Exception ex)
            {
                Document = new FlowDocument();
                AppLog.Message("StringToFlowDocument()", ex);
            }

            return Document;
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
                _textDirty = true;
                AppState.IsDirty = true;
            }
        }
    }
}
