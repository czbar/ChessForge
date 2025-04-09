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
using System.Windows.Input;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace ChessForge
{
    /// <summary>
    /// Encapsulates Intro Tab view with RichTextBox  
    /// </summary>
    public partial class IntroView : RichTextBuilder
    {
        /// <summary>
        /// Not needed in this class
        /// but required for the class derived from RichTextBuilder.
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
        private static bool _isTextDirty = false;

        // used to temporarily block text change events
        private static bool _allowTextChanged = true;

        // whether static methods have been initialized
        private bool _initialized = false;

        // last clicked hyperlink
        private Hyperlink _selectedHyperlink;

        /// <summary>
        /// Names and prefixes for xaml elements.
        /// </summary>
        private readonly string _run_move_ = "run_move_";
        private readonly string _vbox_diag_ = "vbox_move_";
        private readonly string _flip_img_ = "flip_img_";

        // default font size for the Move element text
        private double MOVE_FONT_SIZE = 14;

        // current highest run id (it is 0 initially, because we have the root node)
        private int _maxRunId = 0;

        // selection opacity value to use when restoring the original opacity
        private const double _defaultSelectionOpacity = 0.4;

        /// <summary>
        /// List of nodes currently represented in the view.
        /// </summary>
        private List<TreeNode> Nodes
        {
            get => Intro.Tree.Nodes;
        }

        // currently selected node
        private TreeNode _selectedNode;

        // static reference to HostRtb.
        private static RichTextBox _rtb;

        // flags if the view is in the "print" mode
        private bool _isPrinting;

        /// <summary>
        /// Constructor. Builds the content if not empty.
        /// Initializes data structures.
        /// </summary>
        public IntroView(RichTextBox rtb, Chapter parentChapter, bool isPrinting = false) : base(rtb)
        {
            bool isAppDirty = AppState.IsDirty;
            _isPrinting = isPrinting;

            _rtb = HostRtb;
            HostRtb.Document.Blocks.Clear();

            ParentChapter = parentChapter;

            // set the event handler for text changes.
            if (!_initialized && !_isPrinting)
            {
                _rtb.TextChanged += UiRtbIntroView_TextChanged;
                _initialized = true;
            }

            if (!string.IsNullOrEmpty(Intro.Tree.RootNode.Data))
            {
                LoadXAMLContent();
                SetEventHandlers();

                // as a result of the call to Clear(), the dirty flag were set so reset them
                _isTextDirty = false;
                AppState.IsDirty = isAppDirty;

            }
            Nodes[0].Position = PositionUtils.SetupStartingPosition();
            foreach (var node in Nodes)
            {
                if (node.NodeId > _maxRunId)
                {
                    _maxRunId = node.NodeId;
                    // in older Workbooks castling rights may be set wrong
                    // thus affecting OpeningExplorer queries, so correct them
                    PositionUtils.CorrectCastlingRights(ref node.Position);
                }
            }

            // we are updating diagram shapes so that everything looks ok even if
            // we changed shape graphics in the meantime.
            // However the call will set AppState.IsDirty so let's preserve what it is now.
            bool currDirty = AppState.IsDirty;
            foreach (var node in Nodes)
            {
                UpdateDiagramShapes(node);
            }
            // same for moves as we set the foreground accordign to the current theme
            // rather than what's in XAML
            UpdateMovesColor();

            AppState.IsDirty = currDirty;

            if (!_isPrinting)
            {
                _selectedNode = Nodes[0];
                if (AppState.ActiveVariationTree != null)
                {
                    AppState.ActiveVariationTree.SetSelectedNodeId(_selectedNode.NodeId);
                }

                WebAccessManager.ExplorerRequest(Intro.Tree.TreeId, Nodes[0]);
            }
        }

        /// <summary>
        /// Clear the content of the RTB
        /// </summary>
        public void Clear()
        {
            HostRtb.Document.Blocks.Clear();
        }

        /// <summary>
        /// Restore the original opacity for selections.
        /// </summary>
        public static void RestoreSelectionOpacity()
        {
            AppState.MainWin.UiRtbIntroView.SelectionOpacity = _defaultSelectionOpacity;
        }

        /// <summary>
        /// Make selections invisible.
        /// </summary>
        public void RemoveSelectionOpacity()
        {
            AppState.MainWin.UiRtbIntroView.SelectionOpacity = 0.1;
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
                if (_isPrinting)
                {
                    // if printing, font size directives must be removed.
                    xaml = RemoveFontSizes(xaml);
                    // now set the font size for the whole Intro
                    _rtb.FontSize = Constants.BASE_FIXED_FONT_SIZE + Configuration.FontSizeDiff;
                }
                StringToFlowDocument(xaml);
                _rtb.Document = HostRtb.Document;
                _maxRunId = GetHighestId();
            }
        }

        /// <summary>
        /// Saves the content of the view into the root node of the view.
        /// </summary>
        /// <returns></returns>
        public void SaveXAMLContent(bool cleanup)
        {
            if (_isTextDirty)
            {
                // needs the dispatcher context so it doesn't throw when called from the autosave timer event. 
                AppState.MainWin.Dispatcher.Invoke(() =>
                {
                    RemoveDuplicateNames();
                    if (cleanup && IsDocumentEmpty(HostRtb.Document))
                    {
                        Nodes[0].Data = "";
                        Nodes[0].Comment = "";
                    }
                    else
                    {
                        string xamlText = XamlWriter.Save(HostRtb.Document);
                        Nodes[0].Data = EncodingUtils.Base64Encode(xamlText);
                        Nodes[0].Comment = CopySelectionToClipboard(true);
                        RemoveUnusedNodes();
                    }
                });
            }

            _isTextDirty = false;
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

                DiagramSetupDialog dlg = new DiagramSetupDialog(node);
                GuiUtilities.PositionDialog(dlg, AppState.MainWin, 100);

                if (dlg.ShowDialog() == true)
                {
                    BoardPosition pos = dlg.PositionSetup;
                    node.Position = new BoardPosition(pos);

                    InsertDiagram(node, false);
                    _isTextDirty = true;
                    AppState.IsDirty = true;
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("CreateDiagram()", ex);
            }
        }

        /// <summary>
        /// Asks the user for the link and creates one if data is valid.
        /// If an empty uri is entered in the dialog, this only replaces the selected text. 
        /// </summary>
        public void CreateHyperlink()
        {
            try
            {
                string hlText = _rtb.Selection.Text;

                EditHyperlinkDialog dlg = new EditHyperlinkDialog(null, hlText);
                GuiUtilities.PositionDialog(dlg, AppState.MainWin, 100);

                if (dlg.ShowDialog() == true)
                {
                    // if the dialog returned then the URL is valid or empty (if the latter, dlg.DeleteUrl is true)
                    hlText = dlg.UiTbUrl.Text;
                    if (dlg.DeleteUrl)
                    {
                        // replace selection text and do nothing else
                        _rtb.Selection.Text = dlg.UiTbText.Text;
                    }
                    else
                    {
                        try // just defensive, there should be no exception here
                        {
                            TextPointer tp = _rtb.CaretPosition;
                            // clear selection, it will be replaced by the hyperlink's text
                            _rtb.Selection.Text = "";

                            // get insertion place
                            RichTextBoxUtilities.GetMoveInsertionPlace(_rtb, out Paragraph para, out Inline insertBefore, out double fontSize);

                            Run run = new Run(dlg.UiTbText.Text);
                            run.FontSize = fontSize;

                            Hyperlink hyperlink = new Hyperlink(run);
                            SetupHyperlink(hyperlink, hlText);
                            if (insertBefore == null)
                            {
                                para.Inlines.Add(hyperlink);
                            }
                            else
                            {
                                para.Inlines.InsertBefore(insertBefore, hyperlink);
                            }
                        }
                        catch
                        {
                            _rtb.Selection.Text = dlg.UiTbText.Text;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("CreateHyperlink()", ex);
            }
        }

        /// <summary>
        /// Configures colors and events for a hyperlink.
        /// Returns false if URL is invalid.
        /// </summary>
        /// <param name="hyperlink"></param>
        /// <param name="url"></param>
        private bool SetupHyperlink(Hyperlink hyperlink, string url)
        {
            bool valid = false;

            if (hyperlink != null)
            {
                try
                {
                    hyperlink.NavigateUri = new Uri(url);
                    hyperlink.ToolTip = url;
                    hyperlink.MouseDown += EventHyperlinkClicked;
                    hyperlink.Foreground = ChessForgeColors.CurrentTheme.HyperlinkForeground;
                    hyperlink.MouseEnter += EventHyperlinkMouseEnter;
                    hyperlink.MouseLeave += EventHyperlinkMouseLeave;
                    if (hyperlink.Inlines.Count > 0)
                    {
                        hyperlink.Inlines.FirstInline.Cursor = Cursors.Hand;
                    }
                    valid = true;
                }
                catch
                {
                    valid = false;
                }
            }

            return valid;
        }

        /// <summary>
        /// Removes all expressions FontSize="n".
        /// This is needed for printing because otherwise, the printout
        /// can be a bad mix of font sizes.
        /// TODO: set font sizes on every run in Intro.
        /// </summary>
        /// <param name="xaml"></param>
        /// <returns></returns>
        private string RemoveFontSizes(string xaml)
        {
            string result = xaml;
            if (!string.IsNullOrEmpty(xaml))
            {
                string pattern = @"FontSize=""\d+""";
                string replacement = "";

                result = Regex.Replace(xaml, pattern, replacement);
            }

            return result;
        }

        /// <summary>
        /// A hyperlink part of the comment was clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventHyperlinkClicked(object sender, MouseButtonEventArgs e)
        {
            _selectedHyperlink = null;

            if (e.ChangedButton == MouseButton.Left)
            {
                var hyperlink = sender as Hyperlink;
                if (hyperlink != null)
                {
                    Process.Start(hyperlink.NavigateUri.ToString());
                }
            }
            else if (e.ChangedButton == MouseButton.Right)
            {
                var hyperlink = sender as Hyperlink;
                if (hyperlink != null)
                {
                    _selectedHyperlink = hyperlink;
                    EnableMenuItems(false, false, true, null);
                }
            }
        }

        /// <summary>
        /// Highlight the link when hovered over. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventHyperlinkMouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Hyperlink hl)
            {
                hl.Foreground = ChessForgeColors.CurrentTheme.HyperlinkHoveredForeground;
                e.Handled = true;
            }
        }

        /// <summary>
        /// Back to normal hyperlink color when mouse left.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventHyperlinkMouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Hyperlink hl)
            {
                hl.Foreground = ChessForgeColors.CurrentTheme.HyperlinkForeground;
                e.Handled = true;
            }
        }

        /// <summary>
        /// Due to RTB idiosyncrasies, there may be multiple
        /// Paragraphs with the same name which will cause failures
        /// when loading saved document.
        /// This method renamed all paragraphs sharing the same name
        /// except the first one.
        /// Also renames paragraph if empty (as opposed to null) names as
        /// empty names break XAML.
        /// </summary>
        public void RemoveDuplicateNames()
        {
            List<string> names = new List<string>();

            foreach (Block block in HostRtb.Document.Blocks)
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
                            block.Name = TextUtils.GenerateRandomElementName();
                        }
                    }
                    else
                    {
                        if (name == string.Empty)
                        {
                            block.Name = TextUtils.GenerateRandomElementName();
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
        /// <param name="isHyperlink"></param>
        /// <param name="nd"></param>
        public void EnableMenuItems(bool isDiagram, bool isMove, bool isHyperlink, TreeNode nd)
        {
            try
            {
                foreach (var item in AppState.MainWin.UiMncIntro.Items)
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
                            case "UiCmiSaveDiagram":
                                menuItem.Visibility = isDiagram && nd != null ? Visibility.Visible : Visibility.Collapsed;
                                break;
                            case "UiCmiFlipDiagram":
                                menuItem.Visibility = isDiagram && nd != null ? Visibility.Visible : Visibility.Collapsed;
                                break;
                            case "UiCmiInsertHyperlink":
                                menuItem.Visibility = (!isHyperlink && !isDiagram && !isMove) ? Visibility.Visible : Visibility.Collapsed;
                                break;
                            case "UiCmiEditHyperlink":
                                menuItem.Visibility = isHyperlink ? Visibility.Visible : Visibility.Collapsed;
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
        /// Finds the highest inline id in the document
        /// </summary>
        /// <returns></returns>
        private int GetHighestId()
        {
            int maxId = 0;

            foreach (Block block in HostRtb.Document.Blocks)
            {
                if (block is Paragraph para)
                {
                    foreach (Inline inl in para.Inlines)
                    {
                        int id = TextUtils.GetIdFromPrefixedString(inl.Name);
                        if (id > maxId)
                        {
                            maxId = id;
                        }
                    }
                }
            }

            return maxId;
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
            foreach (Block block in HostRtb.Document.Blocks)
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
        /// Inserts a new move at the caret.
        /// This function is invoked when the user made a move on the main chessboard
        /// or a move from the clipboard is being inserted.
        /// If the latter, the text of the move will be taken from LastMoveAlgebraicNotation
        /// rather then worked out based on the context.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="fromClipboard"></param>
        /// <returns></returns>
        public TextBlock InsertMove(TreeNode node, bool fromClipboard = false)
        {
            if (!_isPrinting)
            {
                AppState.MainWin.UiImgMainChessboard.Source = ChessBoards.ChessBoardBlue;
            }

            if (string.IsNullOrEmpty(node.LastMoveAlgebraicNotation))
            {
                return null;
            }

            PositionUtils.GuessCastlingRights(ref node.Position);

            _selectedNode = node;
            int nodeId = AddNode(node);

            Run rMove = new Run();
            rMove.Name = _run_move_ + nodeId.ToString();
            rMove.Foreground = ChessForgeColors.CurrentTheme.IntroMoveForeground;
            rMove.FontWeight = FontWeights.Bold;
            rMove.FontSize = MOVE_FONT_SIZE;

            if (!_isPrinting)
            {
                WebAccessManager.ExplorerRequest(AppState.ActiveTreeId, _selectedNode);
            }

            return InsertMoveTextBlock(rMove, node, fromClipboard);
        }

        /// <summary>
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
                string uicName = RichTextBoxUtilities.UicMovePrefix + SelectedNode.NodeId.ToString();
                Inline inlClicked = FindInlineByName(HostRtb.Document, uicName);

                TextBlock tb = (inlClicked as InlineUIContainer).Child as TextBlock;

                Run run = null;
                foreach (Inline inl in tb.Inlines)
                {
                    if (inl is Run r)
                    {
                        run = r;
                        break;
                    }
                }

                if (run != null)
                {
                    IntroMoveDialog dlg = new IntroMoveDialog(SelectedNode, run);
                    GuiUtilities.PositionDialog(dlg, AppState.MainWin, 100);

                    if (dlg.ShowDialog() == true)
                    {
                        _isTextDirty = true;
                        AppState.IsDirty = true;

                        SelectedNode.LastMoveAlgebraicNotation = dlg.MoveText;
                        run.Text = PadOutMoveRunText(dlg.MoveText);

                        if (double.TryParse(dlg.MoveFontSize, out double fontSize))
                        {
                            // make sure it is a reasonable value
                            fontSize = Math.Max(fontSize, 8);
                            fontSize = Math.Min(fontSize, 42);

                            run.FontSize = fontSize;
                        }

                        if (dlg.InsertDialogRequest)
                        {
                            TextSelection sel = _rtb.Selection;
                            _rtb.CaretPosition = inlClicked.ElementEnd;
                            InsertDiagram(SelectedNode, false);
                        }

                        // select the edited node on exit
                        _rtb.Selection.Select(inlClicked.ElementStart, inlClicked.ElementEnd);
                        WebAccessManager.ExplorerRequest(Intro.Tree.TreeId, SelectedNode, true);
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

            DiagramSetupDialog dlg = new DiagramSetupDialog(SelectedNode);
            GuiUtilities.PositionDialog(dlg, AppState.MainWin, 100);

            if (dlg.ShowDialog() == true)
            {
                BoardPosition pos = dlg.PositionSetup;
                SelectedNode.Position = new BoardPosition(pos);
                UpdateDiagram(para, SelectedNode);
                _isTextDirty = true;
                AppState.IsDirty = true;
                WebAccessManager.ExplorerRequest(AppState.ActiveTreeId, SelectedNode);
            }
        }

        /// <summary>
        /// Allows the user to edit the clicked hyperlink.
        /// If the uri is cleared, the hyperlink will be replaced by the text Run.
        /// </summary>
        public void EditHyperlink(object sender)
        {
            if (_selectedHyperlink != null)
            {
                EditHyperlinkDialog dlg = new EditHyperlinkDialog(_selectedHyperlink, null);
                GuiUtilities.PositionDialog(dlg, AppState.MainWin, 100);
                if (dlg.ShowDialog() == true)
                {
                    Run run = dlg.HyperlinkRun;
                    string uri = dlg.UiTbUrl.Text;
                    string text = dlg.UiTbText.Text;

                    if (run != null)
                    {
                        run.Text = text;
                    }

                    if (string.IsNullOrEmpty(uri))
                    {
                        Paragraph para = _selectedHyperlink.Parent as Paragraph;
                        if (para != null)
                        {
                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                Run r = new Run(text);
                                para.Inlines.InsertAfter(_selectedHyperlink, r);
                            }
                            para.Inlines.Remove(_selectedHyperlink);
                        }
                    }
                    else
                    {
                        try
                        {
                            _selectedHyperlink.NavigateUri = new Uri(uri);
                        }
                        catch { }
                    }
                }
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

                    TreeNode nd = HandleMoveSelection(r.Name, true);
                    AppState.ActiveVariationTree.SetSelectedNodeId(nd.NodeId);
                    if (nd != null && e.ChangedButton == MouseButton.Left && e.ClickCount == 2)
                    {
                        EditMove();
                    }
                }
                catch { }
            }
        }

        /// <summary>
        /// Calls the OpeningExplorer, adjusts context menu items 
        /// and optionally sets the caret on the selected move.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="adjustCaret"></param>
        /// <returns></returns>
        private TreeNode HandleMoveSelection(string name, bool adjustCaret)
        {
            int nodeId = TextUtils.GetIdFromPrefixedString(name);
            TreeNode nd = GetNodeById(nodeId);
            WebAccessManager.ExplorerRequest(Intro.Tree.TreeId, nd);
            if (nd != null)
            {
                _selectedNode = nd;
                string uicName = RichTextBoxUtilities.UicMovePrefix + nodeId.ToString();
                Inline inlClicked = FindInlineByName(HostRtb.Document, uicName);
                if (adjustCaret)
                {
                    HostRtb.CaretPosition = inlClicked.ElementEnd;
                }
                AppState.MainWin.DisplayPosition(_selectedNode);

                EnableMenuItems(false, true, false, nd);
            }

            return nd;
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
                if (sender is InlineUIContainer)
                {
                    InlineUIContainer iuc = sender as InlineUIContainer;

                    // is a proper diagram para?
                    //if (HasInlineUIContainer(para))
                    {
                        RemoveSelectionOpacity();
                        AppState.MainWin.UiImgMainChessboard.Source = ChessBoards.ChessBoardGrey;

                        Paragraph para = iuc.Parent as Paragraph;
                        _rtb.Focus();
                        _rtb.CaretPosition = para.ContentEnd;

                        int nodeId = TextUtils.GetIdFromPrefixedString(para.Name);
                        AppState.ActiveVariationTree.SetSelectedNodeId(nodeId);
                        TreeNode nd = GetNodeById(nodeId);
                        _selectedNode = nd;
                        WebAccessManager.ExplorerRequest(AppState.ActiveTreeId, nd);
                        if (nd != null)
                        {
                            _selectedNode = nd;
                            AppState.MainWin.DisplayPosition(nd);

                            EnableMenuItems(true, false, false, nd);

                            if (e.ChangedButton == MouseButton.Left && e.ClickCount == 2)
                            {
                                EditDiagram(para);
                            }
                        }
                    }
                }
            }
            catch { }

            e.Handled = true;
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

            foreach (Block block in HostRtb.Document.Blocks)
            {
                if (block is Paragraph)
                {
                    Paragraph p = (Paragraph)block;
                    if (p.Name.StartsWith(RichTextBoxUtilities.DiagramParaPrefix))
                    {
                        if (TextUtils.GetIdFromPrefixedString(p.Name) == nd.NodeId)
                        {
                            para = p;
                            break;
                        }
                    }
                }
            }

            // is a proper diagram para?
            if (HasInlineUIContainer(para))
            {
                return para;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Sets event handlers for the Move and Diagram InlineUIContainers.
        /// </summary>
        private void SetEventHandlers()
        {
            try
            {
                foreach (Block block in HostRtb.Document.Blocks)
                {
                    if (block is Paragraph para)
                    {
                        if (para.Name.StartsWith(RichTextBoxUtilities.DiagramParaPrefix))
                        {
                            Image flipImg = FindFlipImage(para);
                            if (flipImg != null)
                            {
                                flipImg.MouseDown -= EventFlipRequest;
                                flipImg.MouseDown += EventFlipRequest;
                            }
                        }

                        SetInlineUIContainerEventHandlers(para);
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("SetEventHandlers()", ex);
            }
        }

        /// <summary>
        /// Sets event handlers for the Move and Diagram InlineUIContainers in a Paragraph.
        /// </summary>
        /// <param name="para"></param>
        /// <param name="isDiag"></param>
        private void SetInlineUIContainerEventHandlers(Paragraph para)
        {
            bool isDiag = para.Name.StartsWith(RichTextBoxUtilities.DiagramParaPrefix);
            foreach (Inline inline in para.Inlines)
            {
                if (inline is InlineUIContainer iuc && inline.Name.StartsWith(RichTextBoxUtilities.UicMovePrefix))
                {
                    iuc.MouseDown -= EventMoveClicked;
                    iuc.MouseDown += EventMoveClicked;

                    if (isDiag && iuc.Child is Viewbox)
                    {
                        iuc.MouseDown -= EventDiagramClicked;
                        iuc.MouseDown += EventDiagramClicked;
                    }
                }
                else if (inline is Hyperlink hyperlink)
                {
                    hyperlink.MouseDown -= EventHyperlinkClicked;
                    hyperlink.MouseDown += EventHyperlinkClicked;

                    hyperlink.Foreground = ChessForgeColors.CurrentTheme.HyperlinkForeground;

                    hyperlink.MouseEnter -= EventHyperlinkMouseEnter;
                    hyperlink.MouseEnter += EventHyperlinkMouseEnter;

                    hyperlink.MouseLeave -= EventHyperlinkMouseLeave;
                    hyperlink.MouseLeave += EventHyperlinkMouseLeave;
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
        private int FindLastMoveNumber(InlineUIContainer uicCurrent, out PieceColor color, out bool isTextInbetween)
        {
            int number = -1;
            color = PieceColor.None;
            isTextInbetween = false;

            bool done = false;

            try
            {
                foreach (Block block in HostRtb.Document.Blocks)
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
                                else
                                {
                                    isTextInbetween = true;
                                }

                                InlineUIContainer uic = inl as InlineUIContainer;
                                TextBlock tb = uic.Child as TextBlock;
                                if (tb != null)
                                {
                                    foreach (Inline tbInline in tb.Inlines)
                                    {
                                        if (tbInline is Run)
                                        {
                                            int no = MoveUtils.ExtractMoveNumber((tbInline as Run).Text, out PieceColor pc);
                                            if (no >= 0)
                                            {
                                                number = no;
                                                color = pc;
                                                isTextInbetween = false;
                                            }
                                            else
                                            {
                                                int nodeId = TextUtils.GetIdFromPrefixedString(tbInline.Name);
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
                            else
                            {
                                if (inl is Run && !string.IsNullOrEmpty((inl as Run).Text))
                                {
                                    isTextInbetween = true;
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
        /// Checks if the paragraph has at least one InlineUIContainer.
        /// This is useful when we have a Diagram para that is an empty
        /// post-deletion remnant and we don't want to rebuild it.
        /// </summary>
        /// <param name="para"></param>
        /// <returns></returns>
        private static bool HasInlineUIContainer(Paragraph para)
        {
            if (para == null)
            {
                return false;
            }

            bool res = false;

            foreach (Inline inl in para.Inlines)
            {
                if (inl is InlineUIContainer)
                {
                    res = true;
                    break;
                }
            }
            return res;
        }


        /// <summary>
        /// Inserts a diagram at the current position for the selected Node
        /// </summary>
        /// <param name="nd"></param>
        private Paragraph InsertDiagram(TreeNode nd, bool isFlipped)
        {
            TextPointer tp = _rtb.CaretPosition.InsertParagraphBreak();
            Paragraph nextPara = tp.Paragraph;

            // need copy of the node as we may need the original for a move Run
            TreeNode node = nd.CloneMe(true);            
            node.IsDiagram = true;

            int node_id = AddNode(node);
            _selectedNode = node;

            IntroViewDiagram diag = new IntroViewDiagram();
            Paragraph para = BuildDiagramParagraph(diag, node, isFlipped);

            if (!_isPrinting)
            {
                diag.Chessboard.DisplayPosition(node, true);
                diag.Node = node;

                AppState.MainWin.DisplayPosition(node);

                _isTextDirty = true;
                AppState.IsDirty = true;
            }
            DiagramList.Add(diag);

            HostRtb.Document.Blocks.InsertBefore(nextPara, para);

            SetInlineUIContainerEventHandlers(para);

            AppState.MainWin.UiImgMainChessboard.Source = ChessBoards.ChessBoardGrey;

            return para;
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

            bool flipState = RichTextBoxUtilities.GetDiagramFlipState(para);
            if (flip)
            {
                flipState = !flipState;
            }
            nd.IsDiagramFlipped = flipState;

            CreateDiagramElements(para, diag, nd, flipState);
            DiagramList.Add(diag);

            if (!_isPrinting)
            {
                SetInlineUIContainerEventHandlers(para);
                diag.Chessboard.DisplayPosition(nd, true);
                AppState.MainWin.DisplayPosition(nd);
                _isTextDirty = true;
                AppState.IsDirty = true;
            }
        }

        /// <summary>
        /// Invoke from main windows when a shape was drawn on the main chesboard while INTRO tab was active.
        /// </summary>
        /// <param name="nd"></param>
        public void UpdateDiagramShapes(TreeNode nd)
        {
            Paragraph para = FindDiagramParagraph(nd);
            if (para != null)
            {
                UpdateDiagram(para, nd);
            }
        }

        /// <summary>
        /// Update foreground color of all Moves.
        /// This is necessary when opening the first time
        /// as color configuration may have changed,
        /// and when switching modes.
        /// </summary>
        private void UpdateMovesColor()
        {
            foreach (Block block in _rtb.Document.Blocks)
            {
                Paragraph para = block as Paragraph;
                if (para != null)
                {
                    foreach (Inline inPara in para.Inlines)
                    {
                        if (inPara is InlineUIContainer iuc)
                        {
                            TextBlock tb = iuc.Child as TextBlock;
                            if (tb != null)
                            {
                                foreach (Inline inTb in tb.Inlines)
                                {
                                    if (inTb is Run run)
                                    {
                                        run.Foreground = ChessForgeColors.CurrentTheme.IntroMoveForeground;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Inserts a move's Run into a TextBlock that is then inserted into an InlineUIContainer
        /// and finally in the Document.
        /// We determine the place to insert the new move in, and are guessing the number 
        /// to prefix it with.
        /// This is called after the user made a move on the main board or when pasting a move from the clipboard.
        /// If the latter, the text of the move will be taken from LastMoveAlgebraicNotation
        /// rather then worked out based on the context.
        /// </summary>
        /// <param name="run"></param>
        /// <param name="node"></param>
        /// <param name="fromClipboard"></param>
        /// <returns></returns>
        private TextBlock InsertMoveTextBlock(Run run, TreeNode node, bool fromClipboard)
        {
            TextBlock tbMove = null;

            try
            {
                RichTextBoxUtilities.GetMoveInsertionPlace(_rtb, out Paragraph paraToInsertIn, out Inline inlineToInsertBefore, out double fontSize);

                tbMove = new TextBlock();
                tbMove.Name = RichTextBoxUtilities.MoveTextBoxPrefix + node.NodeId.ToString();
                tbMove.Inlines.Add(run);

                InlineUIContainer uic = new InlineUIContainer();
                uic.Name = RichTextBoxUtilities.UicMovePrefix + node.NodeId.ToString();
                uic.Child = tbMove;

                uic.MouseDown -= EventMoveClicked;
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
                if (fromClipboard)
                {
                    run.Text = RichTextBoxUtilities.GetEmbeddedElementPlainText(node);
                }
                else
                {
                    run.Text = PadOutMoveRunText(BuildMoveRunText(node, uic));
                }

                // set caret to the end of the new move
                _rtb.CaretPosition = uic.ElementEnd;

                _isTextDirty = true;
                AppState.IsDirty = true;
            }
            catch (Exception ex)
            {
                AppLog.Message("InsertMoveTextBlock()", ex);
            }

            return tbMove;
        }

        /// <summary>
        /// Prepends and/or appends a space to the move if it is not already there.
        /// </summary>
        /// <param name="moveText"></param>
        /// <returns></returns>
        private string PadOutMoveRunText(string moveText)
        {
            if (string.IsNullOrEmpty(moveText))
            {
                return "";
            }

            StringBuilder sb = new StringBuilder();
            if (moveText[0] != ' ')
            {
                sb.Append(' ' + moveText);
            }
            if (moveText[moveText.Length - 1] != ' ')
            {
                sb.Append(' ');
            }

            return sb.ToString();
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
            int moveNo = FindLastMoveNumber(iuc, out PieceColor previousMoveColor, out bool isTextInbetween);
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
            if (moveColor == PieceColor.White)
            {
                res = moveNo.ToString() + ". " + node.LastMoveAlgebraicNotation;
            }
            else
            {
                if (previousMoveFound && previousMoveColor != PieceColor.Black && !isTextInbetween)
                {
                    res = node.LastMoveAlgebraicNotation;
                }
                else
                {
                    res = moveNo.ToString() + "... " + node.LastMoveAlgebraicNotation;
                }
            }
            res = Languages.MapPieceSymbols(res, moveColor);
            node.LastMoveAlgebraicNotation = res;

            return res;
        }

        /// <summary>
        /// Builds a paragraph with the diagram.
        /// </summary>
        /// <param name="diag"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        private Paragraph BuildDiagramParagraph(IntroViewDiagram diag, TreeNode nd, bool isFlipped)
        {
            Paragraph para = new Paragraph();
            para.Margin = new Thickness(20, 20, 0, 20);
            para.Name = RichTextBoxUtilities.DiagramParaPrefix + nd.NodeId.ToString();

            CreateDiagramElements(para, diag, nd, isFlipped);
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
            _allowTextChanged = false;

            try
            {
                para.Inlines.Clear();
                Canvas baseCanvas = SetupDiagramCanvas();
                Image imgChessBoard = CreateChessBoard(baseCanvas, diag);
                if (flipState)
                {
                    diag.Chessboard.FlipBoard();
                }
                diag.Chessboard.EnableShapes(true, nd);
                baseCanvas.Children.Add(imgChessBoard);

                Canvas sideCanvas = CreateDiagramSideCanvas(baseCanvas);
                CreateDiagramFlipImage(sideCanvas, nd);

                // add invisible checkbox holding the flipped state
                CreateFlippedCheckBox(baseCanvas, flipState);

                Viewbox viewBox = SetupDiagramViewbox(baseCanvas);

                InlineUIContainer uic = new InlineUIContainer();
                uic.Child = viewBox;
                uic.Name = RichTextBoxUtilities.UicMovePrefix + nd.NodeId.ToString();
                viewBox.Name = _vbox_diag_ + nd.NodeId.ToString();
                para.Inlines.Add(uic);
            }
            finally
            {
                _allowTextChanged = true;
            }
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
            sideCanvas.Width = 21;
            sideCanvas.Height = parent.Height + 2;
            sideCanvas.Background = ChessForgeColors.CurrentTheme.IntroDiagSideCanvasBackground;
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
                CheckBox cb = RichTextBoxUtilities.FindFlippedCheckBox(para);
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
            canvas.Background = ChessForgeColors.CurrentTheme.IntroDiagBackground;
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
        /// If the first attempt to deserialize fails we will try
        /// to deduplicate names (which is usually the reason) and try again.
        /// </summary>
        /// <param name="xamlString"></param>
        /// <returns></returns>
        private FlowDocument StringToFlowDocument(string xamlString)
        {
            bool success;

            try
            {
                HostRtb.Document = XamlToDocument(xamlString);
                success = true;
            }
            catch (Exception ex)
            {
                success = false;
                HostRtb.Document = new FlowDocument();
                AppLog.Message("StringToFlowDocument() first attempt", ex);
            }

            if (!success)
            {
                // run dedupe and try again
                xamlString = DedupeXamlString(xamlString);
                try
                {
                    HostRtb.Document = XamlToDocument(xamlString);
                }
                catch (Exception ex)
                {
                    HostRtb.Document = new FlowDocument();
                    AppLog.Message("StringToFlowDocument() second attempt", ex);
                    MessageBox.Show(Properties.Resources.ErrorParsingIntro, Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            return HostRtb.Document;
        }

        /// <summary>
        /// Attempt to deserialize the xaml string.
        /// Let the caller handle exceptions.
        /// </summary>
        /// <param name="xamlString"></param>
        /// <returns></returns>
        private FlowDocument XamlToDocument(string xamlString)
        {
            return XamlReader.Parse(xamlString) as FlowDocument;
        }

        /// <summary>
        /// Deduplicate "Name" strings in the xaml string.
        /// It should never be needed so don't worry about performance.
        /// </summary>
        /// <param name="xamlString"></param>
        /// <returns></returns>
        private string DedupeXamlString(string xamlString)
        {
            string SEARCH_STRING = " Name=\"";

            int currpos = 0;
            while (true)
            {
                currpos = xamlString.IndexOf(SEARCH_STRING, currpos);
                if (currpos == -1)
                {
                    // we're done
                    break;
                }
                else
                {
                    int closeQuotePos = xamlString.IndexOf('"', currpos + SEARCH_STRING.Length + 1);
                    if (closeQuotePos == -1)
                    {
                        // something wrong but nothing we can fix
                        break;
                    }
                    else
                    {
                        // duplicates to search for in the remainder of the string
                        string nameToSearch = xamlString.Substring(currpos, (closeQuotePos - currpos) + 1);
                        int nameToSearchPos = xamlString.IndexOf(nameToSearch, closeQuotePos);

                        if (nameToSearchPos > 0)
                        {
                            // duplicate found so replace
                            string nameToReplaceWith = SEARCH_STRING + TextUtils.GenerateRandomElementName() + "\"";
                            string xamlPart1 = xamlString.Substring(0, closeQuotePos + 1);
                            string xamlPart2 = xamlString.Substring(closeQuotePos + 1);
                            xamlPart1 = xamlPart1.Replace(nameToSearch, nameToReplaceWith);
                            xamlString = xamlPart1 + xamlPart2;
                        }
                        currpos = closeQuotePos;
                    }
                }
            }

            return xamlString;
        }

        /// <summary>
        /// If the mouse is outside a diagram, make sure the main board is blue.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (_rtb.CaretPosition.Paragraph != null)
            {
                if (!RichTextBoxUtilities.IsDiagramPara(_rtb.CaretPosition.Paragraph))
                {
                    RestoreSelectionOpacity();
                    AppState.MainWin.UiImgMainChessboard.Source = Configuration.StudyBoardSet.MainBoard;
                }
            }

            if ((Keyboard.Modifiers & ModifierKeys.Control) > 0 && (Keyboard.Modifiers & ModifierKeys.Alt) == 0)
            {
                try
                {
                    switch (e.Key)
                    {
                        case Key.Z:
                            Command_Undo(null, null);
                            e.Handled = true;
                            break;
                        case Key.Home:
                            _rtb.ScrollToHome();
                            e.Handled = true;
                            break;
                        case Key.End:
                            _rtb.ScrollToEnd();
                            e.Handled = true;
                            break;
                        case Key.G:
                            AppState.MainWin.UiMnFindGames_Click(null, null);
                            e.Handled = true;
                            break;
                    }
                }
                catch
                {
                }
            }
            else if ((Keyboard.Modifiers & ModifierKeys.Shift) > 0 && (Keyboard.Modifiers & (ModifierKeys.Alt | ModifierKeys.Control)) == 0)
            {
                switch (e.Key)
                {
                    case Key.F3:
                        AppState.MainWin.UiMnFindPositions_Click(null, null);
                        break;
                }
            }
            else if (e.Key == Key.Left || e.Key == Key.Right)
            {
                e.Handled = ProcessArrowKey(e.Key);
            }
            else if (e.Key == Key.F3)
            {
                AppState.MainWin.UiMnFindIdenticalPosition_Click(null, null);
                e.Handled = true;
            }
            else if (e.Key == Key.PageUp)
            {
                _rtb.PageUp();
                e.Handled = true;
            }
            else if (e.Key == Key.PageDown)
            {
                _rtb.PageDown();
                e.Handled = true;
            }
        }

        /// <summary>
        /// if the Left/Right key was pressed, check if we have a single move element selected.
        /// If so go to the previous/next move, otherwise allow the default behavior.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private bool ProcessArrowKey(Key key)
        {
            bool handled = false;

            if (key == Key.Left || key == Key.Right)
            {
                InlineUIContainer moveUiElement = GetSelectedMoveElement();
                if (moveUiElement != null)
                {
                    InlineUIContainer nearestMove = FindNearestMoveElement(key == Key.Right ? _rtb.Selection.End : _rtb.Selection.Start, key == Key.Right);
                    if (nearestMove != null)
                    {
                        // set new selection
                        _rtb.Selection.Select(nearestMove.ContentStart, nearestMove.ContentEnd);

                        // must mark the event as handled
                        handled = true;
                        HandleMoveSelection(nearestMove.Name, false);
                    }
                }
            }

            return handled;
        }

        /// <summary>
        /// Finds the Move InlineUIElement nearest to the passed text pointer
        /// </summary>
        /// <param name="position"></param>
        /// <param name="forward">if true search in the forward direction, otherwise backward</param>
        /// <returns></returns>
        private InlineUIContainer FindNearestMoveElement(TextPointer position, bool forward)
        {
            InlineUIContainer nearest = null;

            while ((position = position.GetNextContextPosition(forward ? LogicalDirection.Forward : LogicalDirection.Backward)) != null)
            {
                TextPointerContext tpc = position.GetPointerContext(forward ? LogicalDirection.Forward : LogicalDirection.Backward);
                if (tpc == TextPointerContext.EmbeddedElement)
                {
                    TextElement elem = position.Parent as TextElement;
                    if (elem != null)
                    {
                        // make sure this is not a diagram i.e. this element is not inside a diagram paragraph
                        if (!(elem.Parent is Paragraph)
                            || !(elem.Parent as Paragraph).Name.Contains(RichTextBoxUtilities.DiagramParaPrefix))
                        {
                            nearest = position.Parent as InlineUIContainer;
                            break;
                        }
                    }
                }
            }

            return nearest;
        }

        /// <summary>
        /// Given a TextPointer and the search direction, finds the InlineUIContainer
        /// immediately adjacent to the pointer.
        /// </summary>
        /// <param name="tp"></param>
        /// <param name="forward"></param>
        /// <returns></returns>
        private InlineUIContainer GetMoveContainerFromTextPointer(TextPointer tp, bool forward)
        {
            InlineUIContainer iuc = null;

            var elem = tp.GetAdjacentElement(forward ? LogicalDirection.Forward : LogicalDirection.Backward);

            if (elem is InlineUIContainer)
            {
                iuc = elem as InlineUIContainer;
            }
            else
            {
                Run run = tp.Parent as Run;
                if (run != null)
                {
                    iuc = forward ? run.NextInline as InlineUIContainer : run.PreviousInline as InlineUIContainer;
                }
            }

            return iuc;
        }

        /// <summary>
        /// If the current selection consists of exactly one InlineUIContainer (move),
        /// the name of the container will be returned.
        /// Otherwise returns null.
        /// </summary>
        /// <returns></returns>
        private InlineUIContainer GetSelectedMoveElement()
        {
            InlineUIContainer move = null;

            try
            {
                if (!_rtb.Selection.IsEmpty)
                {
                    InlineUIContainer uicStart = GetMoveContainerFromTextPointer(_rtb.Selection.Start, true);
                    InlineUIContainer uicEnd = GetMoveContainerFromTextPointer(_rtb.Selection.End, false);

                    // do start and end point to the same Element?
                    if (uicStart != null && uicEnd != null && !string.IsNullOrEmpty(uicStart.Name))
                    {
                        if (uicStart.Name.Contains(RichTextBoxUtilities.UicMovePrefix) && uicStart.Name == uicEnd.Name)
                        {
                            move = uicStart;
                        }
                    }
                }
            }
            catch { }

            return move;
        }

        /// <summary>
        /// Handles the Text Change event. Sets the Workbook's dirty flag.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void UiRtbIntroView_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_allowTextChanged)
            {
                return;
            }

            _isTextDirty = true;

            // stop TextChanged event handler! 
            _rtb.TextChanged -= UiRtbIntroView_TextChanged;

            // we want to avoid adding anything to the diagram paragraph outside of the diagram InlineUIElement so check for this
            TextPointer tpCaret = _rtb.CaretPosition;
            Paragraph para = tpCaret.Paragraph;

            if (RichTextBoxUtilities.GetDiagramFromParagraph(para, out InlineUIContainer diagram))
            {
                CleanupDiagramPara(para, diagram);
            }
            else
            {
                RestoreSelectionOpacity();
                AppState.MainWin.UiImgMainChessboard.Source = Configuration.StudyBoardSet.MainBoard;
            }

            e.Handled = true;
            AppState.IsDirty = true;

            // reset TextChanged event handler
            _rtb.TextChanged += UiRtbIntroView_TextChanged;
        }

        /// <summary>
        /// Ensures that only the Diagram InlineUIContainer remains
        /// in the Diagram Paragraph.
        /// If the user inserted something we will handle this as follows:
        /// - if the extra inline was before the diagram, it will be deleted
        /// - if the inline was after the diagram, a new Paragraph will be created
        ///   and the inline will be moved there along with the caret.
        /// </summary>
        /// <param name="para"></param>
        /// <param name="diagram"></param>
        private static void CleanupDiagramPara(Paragraph para, InlineUIContainer diagram)
        {
            _allowTextChanged = false;
            List<Inline> inlinesToDelete = new List<Inline>();
            List<Inline> inlinesToMove = new List<Inline>();
            if (para != null && diagram != null)
            {
                bool beforeDiagram = true;
                foreach (Inline inl in para.Inlines)
                {
                    if (inl != diagram)
                    {
                        if (beforeDiagram)
                        {
                            inlinesToDelete.Add(inl);
                        }
                        else
                        {
                            inlinesToMove.Add(inl);
                        }
                    }
                    else
                    {
                        beforeDiagram = false;
                    }
                }

                try
                {
                    foreach (Inline inl in inlinesToDelete)
                    {
                        para.Inlines.Remove(inl);
                    }

                    if (inlinesToMove.Count > 0)
                    {
                        Paragraph newPara = _rtb.CaretPosition.InsertParagraphBreak().Paragraph;
                        foreach (Inline inl in inlinesToMove)
                        {
                            newPara.Inlines.Add(inl);
                            _rtb.CaretPosition = inl.ElementEnd;
                            para.Inlines.Remove(inl);
                        }
                    }
                }
                catch { }
            }

            _allowTextChanged = true;
            _isTextDirty = true;
        }
    }
}
