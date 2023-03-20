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

        // indicates if the text has been modified.
        private bool _textDirty = false;

        /// <summary>
        /// Names and prefixes for xaml elements.
        /// </summary>
        private readonly string _run_move_ = "run_move_";
        private readonly string _tb_move_ = "tb_move_";
        private readonly string _uic_move_ = "uic_move_";
        private readonly string _para_diagram_ = "para_diag_";
        private readonly string _flip_img_ = "flip_img_";

        // current highest run id (it is 0 initially, because we have the root node)
        private int _maxRunId = 0;

        // selection opacity value to use when restoring the original opacity
        private double _defaultSelectionOpacity = 0.4;

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

        /// <summary>
        /// Constructor. Builds the content if not empty.
        /// Initializes data structures.
        /// </summary>
        public IntroView(FlowDocument doc, Chapter parentChapter) : base(doc)
        {
            bool isAppDirty = AppState.IsDirty;
            _defaultSelectionOpacity = AppState.MainWin.UiRtbIntroView.SelectionOpacity;

            Document.Blocks.Clear();

            ParentChapter = parentChapter;

            // set the event handler for text changes.
            _rtb.TextChanged += UiRtbIntroView_TextChanged;
            if (!string.IsNullOrEmpty(Intro.Tree.RootNode.Data))
            {
                LoadXAMLContent();
                SetEventHandlers();

                // as a result of the call to Clear(), the dirty flag were set so reset them
                _textDirty = false;
                AppState.IsDirty = isAppDirty;

            }
            Nodes[0].Position = PositionUtils.SetupStartingPosition();
            foreach (var node in Nodes)
            {
                if (node.NodeId > _maxRunId)
                {
                    _maxRunId = node.NodeId;
                }
            }
            WebAccessManager.ExplorerRequest(Intro.Tree.TreeId, Nodes[0]);
        }

        /// <summary>
        /// Clear the content of the RTB
        /// </summary>
        public void Clear()
        {
            Document.Blocks.Clear();
        }

        /// <summary>
        /// Restore the original opacity for selections.
        /// </summary>
        public void RestoreSelectionOpacity()
        {
            AppState.MainWin.UiRtbIntroView.SelectionOpacity = _defaultSelectionOpacity;
        }

        /// <summary>
        /// Make selections invisible.
        /// </summary>
        public void RemoveSelectionOpacity()
        {
            AppState.MainWin.UiRtbIntroView.SelectionOpacity = 0;
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
                RemoveDuplicateNames();
                string xamlText = XamlWriter.Save(Document);
                Nodes[0].Data = EncodingUtils.Base64Encode(xamlText);
                RemoveUnusedNodes();
            }

            _textDirty = false;
        }

        /// <summary>
        /// Due to RTB idiosyncrasies, there may be multiple
        /// Paragraphs with the same name which will cause failures
        /// when loading saved document.
        /// This method renamed all paragraphs sharing the same name
        /// except the first one.
        /// </summary>
        public void RemoveDuplicateNames()
        {
            List<string> names = new List<string>();

            foreach (Block block in Document.Blocks)
            {
                if (block is Paragraph)
                {
                    string name = block.Name;
                    if (!string.IsNullOrEmpty(name))
                    {
                        if (names.Find(x => x == name) == null)
                        {
                            names.Add(name);
                        }
                        else
                        {
                            block.Name = Guid.NewGuid().ToString("N");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Enables context menu items according to the passed arguments
        /// </summary>
        /// <param name="isDiagram"></param>
        /// <param name="isMove"></param>
        /// <param name="nd"></param>
        public void EnableMenuItems(bool isDiagram, bool isMove, TreeNode nd)
        {
            try
            {
                foreach (var item in AppState.MainWin.UiCmIntro.Items)
                {
                    if (item is MenuItem)
                    {
                        MenuItem menuItem = item as MenuItem;
                        switch (menuItem.Name)
                        {
                            case "UiCmiInsertDiagram":
                                menuItem.Visibility = !isDiagram ? Visibility.Visible : Visibility.Collapsed;
                                break;
                            case "UiCmiEditDiagram":
                                menuItem.Visibility = isDiagram && nd != null ? Visibility.Visible : Visibility.Collapsed;
                                break;
                            case "UiCmiFlipDiagram":
                                menuItem.Visibility = isDiagram && nd != null ? Visibility.Visible : Visibility.Collapsed;
                                break;
                            case "UiCmiEditMove":
                                menuItem.Visibility = isMove && nd != null ? Visibility.Visible : Visibility.Collapsed;
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("EnableMenuItems()", ex);
            }

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

        /// Edits a Move element.
        /// </summary>
        public void EditMove()
        {
            if (SelectedNode == null)
            {
                return;
            }

            try
            {
                string uicName = _uic_move_ + SelectedNode.NodeId.ToString();
                Inline inlClicked = FindInlineByName(uicName);

                IntroMoveDialog dlg = new IntroMoveDialog(SelectedNode)
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

                    SelectedNode.LastMoveAlgebraicNotation = dlg.MoveText;

                    TextBlock tb = (inlClicked as InlineUIContainer).Child as TextBlock;

                    foreach (Inline inl in tb.Inlines)
                    {
                        if (inl is Run r)
                        {
                            r.Text = " " + dlg.MoveText + " ";
                        }
                    }

                    if (dlg.InsertDialogRequest)
                    {
                        TextSelection sel = _rtb.Selection;
                        _rtb.CaretPosition = inlClicked.ElementEnd;
                        InsertDiagram(SelectedNode);
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("EditMove", ex);
            }
        }

        /// <summary>
        /// Invokes a dialog allowing the user to edit position on the clicked dialog.
        /// </summary>
        /// <param name="para"></param>
        public void EditDiagram(Paragraph para = null)
        {
            if (para == null)
            {
                para = FindDiagramParagraph(SelectedNode);
            }

            if (para == null || SelectedNode == null)
            {
                return;
            }

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
                SelectedNode.Position = new BoardPosition(pos);
                UpdateDiagram(para, SelectedNode);
                _textDirty = true;
                AppState.IsDirty = true;
                WebAccessManager.ExplorerRequest(AppState.ActiveTreeId, SelectedNode);
            }
        }

        /// <summary>
        /// Flips the diagram.
        /// </summary>
        public void FlipDiagram()
        {
            Paragraph para = FindDiagramParagraph(SelectedNode);
            if (para != null)
            {
                UpdateDiagram(para, SelectedNode, true);
                return;
            }
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
                    WebAccessManager.ExplorerRequest(Intro.Tree.TreeId, nd);
                    if (nd != null)
                    {
                        _selectedNode = nd;
                        string uicName = _uic_move_ + nodeId.ToString();
                        Inline inlClicked = FindInlineByName(uicName);
                        _rtb.CaretPosition = inlClicked.ElementEnd;
                        AppState.MainWin.DisplayPosition(_selectedNode);

                        EnableMenuItems(false, true, nd);

                        if (e.ChangedButton == MouseButton.Left && e.ClickCount == 2)
                        {
                            EditMove();
                        }
                    }
                }
                catch { }
            }
        }

        /// <summary>
        /// <summary>
        /// The diagram paragraph was clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventDiagramClicked(object sender, MouseButtonEventArgs e)
        {
            try
            {
                RemoveSelectionOpacity();
                if (sender is Paragraph)
                {
                    Paragraph para = sender as Paragraph;
                    _rtb.CaretPosition = para.ContentStart;

                    int nodeId = TextUtils.GetIdFromPrefixedString(para.Name);
                    TreeNode nd = GetNodeById(nodeId);
                    _selectedNode = nd;
                    WebAccessManager.ExplorerRequest(AppState.ActiveTreeId, nd);
                    if (nd != null)
                    {
                        _selectedNode = nd;
                        AppState.MainWin.DisplayPosition(nd);

                        EnableMenuItems(true, false, nd);

                        if (e.ChangedButton == MouseButton.Left && e.ClickCount == 2)
                        {
                            EditDiagram(para);
                        }
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Responds to the click on the "flip" image
        /// by effecting the flip.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventFlipRequest(object sender, MouseButtonEventArgs e)
        {
            if (sender is Image)
            {
                try
                {
                    Image image = sender as Image;
                    int nodeId = TextUtils.GetIdFromPrefixedString(image.Name);
                    TreeNode nd = GetNodeById(nodeId);
                    if (nd != null)
                    {
                        _selectedNode = nd;
                        FlipDiagram();
                    }
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// Finds diagram paragraph for a given node.
        /// </summary>
        /// <param name="nd"></param>
        /// <returns></returns>
        private Paragraph FindDiagramParagraph(TreeNode nd)
        {
            if (nd == null)
            {
                return null;
            }

            Paragraph para = null;

            foreach (Block block in Document.Blocks)
            {
                if (block is Paragraph)
                {
                    Paragraph p = (Paragraph)block;
                    if (p.Name.StartsWith(_para_diagram_))
                    {
                        if (TextUtils.GetIdFromPrefixedString(p.Name) == nd.NodeId)
                        {
                            para = p;
                            break;
                        }
                    }
                }
            }

            return para;
        }

        /// <summary>
        /// Sets event handlers for the move Runs
        /// and diagram Paragraphs.
        /// </summary>
        private void SetEventHandlers()
        {
            try
            {
                foreach (Block block in Document.Blocks)
                {
                    if (block is Paragraph)
                    {
                        Paragraph p = (Paragraph)block;
                        if (p.Name.StartsWith(_para_diagram_))
                        {
                            p.MouseDown += EventDiagramClicked;

                            Image flipImg = FindFlipImage(p);
                            if (flipImg != null)
                            {
                                flipImg.MouseDown += EventFlipRequest;
                            }
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
            catch (Exception ex)
            {
                AppLog.Message("SetEventHandlers()", ex);
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
                                            else
                                            {
                                                int nodeId = TextUtils.GetIdFromPrefixedString(tbLinline.Name);
                                                TreeNode nd = GetNodeById(nodeId);
                                                if (nd != null)
                                                {
                                                    color = MoveUtils.ReverseColor(nd.ColorToMove);
                                                }
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
                TreeNode node = new TreeNode(null, "", 0);
                node.Position = new BoardPosition(SelectedNode.Position);

                DiagramSetupDialog dlg = new DiagramSetupDialog(node)
                {
                    Left = AppState.MainWin.ChessForgeMain.Left + 100,
                    Top = AppState.MainWin.Top + 100,
                    Topmost = false,
                    Owner = AppState.MainWin
                };

                if (dlg.ShowDialog() == true)
                {
                    BoardPosition pos = dlg.PositionSetup;
                    node.Position = new BoardPosition(pos);

                    InsertDiagram(node);
                    _textDirty = true;
                    AppState.IsDirty = true;
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("CreateDiagram()", ex);
            }
        }

        /// <summary>
        /// Inserts a diagram at the current position for the selected Node
        /// </summary>
        /// <param name="nd"></param>
        private void InsertDiagram(TreeNode nd)
        {
            TextPointer tp = _rtb.CaretPosition.InsertParagraphBreak();
            Paragraph nextPara = tp.Paragraph;

            // need copy of the node as we may need the original for a move Run
            TreeNode node = nd.CloneMe(true);
            int node_id = AddNode(node);
            _selectedNode = node;

            IntroViewDiagram diag = new IntroViewDiagram();
            Paragraph para = BuildDiagramParagraph(diag, node);
            diag.Chessboard.DisplayPosition(node, true);
            diag.Node = node;

            DiagramList.Add(diag);

            AppState.MainWin.DisplayPosition(node);

            _textDirty = true;
            AppState.IsDirty = true;

            Document.Blocks.InsertBefore(nextPara, para);
            para.MouseDown += EventDiagramClicked;
        }

        /// <summary>
        /// Updates an existing diagram.
        /// The caller passes a Paragraph hosting the diagram to update.
        /// </summary>
        /// <param name="para"></param>
        /// <param name="nd"></param>
        public void UpdateDiagram(Paragraph para, TreeNode nd, bool flip = false)
        {
            IntroViewDiagram diag = DiagramList.Find(x => x.Node.NodeId == nd.NodeId);
            if (diag == null)
            {
                diag = new IntroViewDiagram();
                diag.Node = nd;
            }

            bool flipState = GetDiagramFlipState(para);
            if (flip)
            {
                flipState = !flipState;
            }

            CreateDiagramElements(para, diag, nd, flipState);
            DiagramList.Add(diag);

            if (flipState)
            {
                diag.Chessboard.FlipBoard();
            }

            diag.Chessboard.DisplayPosition(nd, true);
            AppState.MainWin.DisplayPosition(nd);

            _textDirty = true;
            AppState.IsDirty = true;
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

                _textDirty = true;
                AppState.IsDirty = true;
            }
            catch (Exception ex)
            {
                AppLog.Message("InsertMoveTextBlock()", ex);
            }
        }

        /// <summary>
        /// Builds a string to display in the GUI for the passed node.
        /// We are looking for the previous Node TextBlock. If found
        /// we parse the text to see if we can figure out the move number
        /// and color.  If color cannot be determined (because it is Black's move
        /// without ... in front) we determine the color from the Node's properties.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="iuc"></param>
        /// <returns></returns>
        private string BuildMoveRunText(TreeNode node, InlineUIContainer iuc)
        {
            int moveNo = FindLastMoveNumber(iuc, out PieceColor previousMoveColor);
            bool previousMoveFound = moveNo > 0;

            PieceColor moveColor = MoveUtils.ReverseColor(node.ColorToMove);

            if (moveNo == -1)
            {
                moveNo = 1;
            }
            else
            {
                if (moveColor != previousMoveColor && moveColor == PieceColor.White)
                {
                    moveNo++;
                }
            }

            string res;
            //string res = moveNo.ToString() + (moveColor == PieceColor.Black ? "... " : ". ") + node.LastMoveAlgebraicNotation;
            if (moveColor == PieceColor.White)
            {
                res = moveNo.ToString() + ". " + node.LastMoveAlgebraicNotation;
            }
            else
            {
                if (previousMoveFound && previousMoveColor != PieceColor.Black)
                {
                    res = node.LastMoveAlgebraicNotation;
                }
                else
                {
                    res = moveNo.ToString() + "... " + node.LastMoveAlgebraicNotation;
                }
            }
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

            CreateDiagramElements(para, diag, nd, false);
            return para;
        }

        /// <summary>
        /// Creates UI Elements for the diagram.
        /// </summary>
        /// <param name="para"></param>
        /// <param name="diag"></param>
        /// <param name="nd"></param>
        /// <returns></returns>
        private Paragraph CreateDiagramElements(Paragraph para, IntroViewDiagram diag, TreeNode nd, bool flipState)
        {
            para.Inlines.Clear();
            Canvas baseCanvas = SetupDiagramCanvas();
            Image imgChessBoard = CreateChessBoard(baseCanvas, diag);
            diag.Chessboard.EnableShapes(true, nd);
            baseCanvas.Children.Add(imgChessBoard);

            Canvas sideCanvas = CreateDiagramSideCanvas(baseCanvas);
            CreateDiagramFlipImage(sideCanvas, nd);

            // add invisible checkbox holding the flipped state
            CreateFlippedCheckBox(baseCanvas, flipState);

            Viewbox viewBox = SetupDiagramViewbox(baseCanvas);

            InlineUIContainer uic = new InlineUIContainer();
            uic.Child = viewBox;
            uic.Name = _uic_move_ + nd.NodeId.ToString();
            para.Inlines.Add(uic);
            return para;
        }

        /// <summary>
        /// Creates a canvas on the outside of the main canvas to host the flip image.
        /// </summary>
        /// <param name="parent"></param>
        /// <returns></returns>
        private Canvas CreateDiagramSideCanvas(Canvas parent)
        {
            Canvas sideCanvas = new Canvas();
            sideCanvas.Width = 20;
            sideCanvas.Height = parent.Height + 2;
            sideCanvas.Background = Brushes.White;
            parent.Children.Add(sideCanvas);
            Canvas.SetLeft(sideCanvas, 250);
            Canvas.SetTop(sideCanvas, -1);
            return sideCanvas;
        }

        /// <summary>
        /// Creates a "flip image" button and inserts it into the side canvas.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="nd"></param>
        /// <returns></returns>
        private Image CreateDiagramFlipImage(Canvas parent, TreeNode nd)
        {
            Image imgFlip = new Image();
            imgFlip.Source = ImageSources.FlipBoard;
            imgFlip.Width = 16;
            imgFlip.Height = 16;
            imgFlip.MouseDown += EventFlipRequest;
            imgFlip.Name = _flip_img_ + nd.NodeId;
            parent.Children.Add(imgFlip);
            Canvas.SetLeft(imgFlip, 4);
            Canvas.SetTop(imgFlip, 117);
            return imgFlip;
        }

        /// <summary>
        /// Creates an invisible flip state CheckBox and inserts it in the diagram.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="isFlipped"></param>
        /// <returns></returns>
        private CheckBox CreateFlippedCheckBox(Canvas parent, bool isFlipped)
        {
            CheckBox cbFlipped = new CheckBox();
            cbFlipped.IsChecked = isFlipped;
            cbFlipped.Visibility = Visibility.Collapsed;
            parent.Children.Add(cbFlipped);

            Canvas.SetLeft(cbFlipped, 100);
            Canvas.SetTop(cbFlipped, 100);

            return cbFlipped;
        }

        /// <summary>
        /// Determines the flipped state of the diagram by finding the hidden
        /// Flipped Check Box and reading it state.
        /// The passed Pararaph will have an InlineUIContainer as one of its inlines
        /// which has a ViewBox as its only child which in turn has a Canvas
        /// as its only child.
        /// The 'flipped" CheckBox is one of the children of that Canvas.
        /// 
        /// </summary>
        /// <param name="para"></param>
        /// <param name="isFlipped"></param>
        private void SetDiagramFlipState(Paragraph para, bool isFlipped)
        {
            try
            {
                CheckBox cb = FindFlippedCheckBox(para);
                if (cb != null)
                {
                    cb.IsChecked = isFlipped;
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Gets the orientation (aka "flip state") of the diagram
        /// by checking the status of the hidden checkbox in the diagram.
        /// </summary>
        /// <param name="para"></param>
        /// <returns></returns>
        private bool GetDiagramFlipState(Paragraph para)
        {
            bool res = false;

            try
            {
                CheckBox cb = FindFlippedCheckBox(para);
                if (cb != null)
                {
                    res = cb.IsChecked == true;
                }
            }
            catch
            {
            }

            return res;
        }

        /// <summary>
        /// Returns the diagram's "flip state" CheckBox
        /// </summary>
        /// <param name="para"></param>
        /// <returns></returns>
        private CheckBox FindFlippedCheckBox(Paragraph para)
        {
            try
            {
                CheckBox cb = null;

                foreach (Inline inl in para.Inlines)
                {
                    if (inl is InlineUIContainer)
                    {
                        Viewbox vb = ((InlineUIContainer)inl).Child as Viewbox;
                        Canvas canvas = vb.Child as Canvas;
                        foreach (UIElement uie in canvas.Children)
                        {
                            if (uie is CheckBox)
                            {
                                cb = uie as CheckBox;
                                break;
                            }
                        }
                    }
                }

                return cb;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Returns the diagram's "flip state" image.
        /// </summary>
        /// <param name="para"></param>
        /// <returns></returns>
        private Image FindFlipImage(Paragraph para)
        {
            try
            {
                Image img = null;

                foreach (Inline inl in para.Inlines)
                {
                    if (inl is InlineUIContainer)
                    {
                        Viewbox vb = ((InlineUIContainer)inl).Child as Viewbox;
                        Canvas canvas = vb.Child as Canvas;
                        foreach (UIElement uie in canvas.Children)
                        {
                            if (uie is Canvas)
                            {
                                foreach (UIElement elm in (uie as Canvas).Children)
                                {
                                    if (elm is Image)
                                    {
                                        img = elm as Image;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                return img;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Returns the Canvas for the chessboard if found
        /// </summary>
        /// <param name="para"></param>
        /// <returns></returns>
        private Canvas FindBoardCanvas(Paragraph para)
        {
            try
            {
                Canvas canvas = null;

                foreach (Inline inl in para.Inlines)
                {
                    if (inl is InlineUIContainer)
                    {
                        Viewbox vb = ((InlineUIContainer)inl).Child as Viewbox;
                        canvas = vb.Child as Canvas;
                        break;
                    }
                }

                return canvas;
            }
            catch
            {
                return null;
            }
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
            canvas.Width = 270;
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
            viewBox.Width = 270;
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
            _textDirty = true;
            AppState.IsDirty = true;
        }
    }
}
