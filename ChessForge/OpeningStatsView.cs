using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Linq;
using WebAccess;

namespace ChessForge
{
    public class OpeningStatsView : RichTextBuilder
    {
        // maximum allowed number of rows
        private readonly int MAX_MOVE_ROW_COUNT = 15;

        // List of objects that encapsulate TableRows used in the _gamesTable.
        private List<OpeningStatsViewRow> _lstRows = new List<OpeningStatsViewRow>();

        // string to use when query returned no opening name for the position
        private string POSITION_NOT_NAMED = Properties.Resources.NotNamed;

        // scale factor for table cell sizes
        private double scaleFactor = 3.5;

        // The top table showing the name of the opening 
        private Table _openingNameTable;

        // Label containing the opening name
        private Label _lblOpeningName;

        // Lable containing the ECO code 
        private Label _lblEcoCode;

        // The main table holding the stats
        private Table _openingStatsTable;

        /// <summary>
        /// Id of the tree to which the node being handled belongs.
        /// </summary>
        private int _treeId;

        /// <summary>
        /// Node for which we are showing the stats
        /// </summary>
        private TreeNode _node;

        /// <summary>
        /// The move number string to prefix the moves in the table with
        /// </summary>
        private string _moveNumberString;

        /// <summary>
        /// What kind of data is being shown.
        /// </summary>
        private enum DataMode
        {
            NO_DATA,
            OPENINGS,
            TABLEBASE
        }

        /// <summary>
        /// Creates the view and registers a listener with WebAccess
        /// </summary>
        /// <param name="doc"></param>
        public OpeningStatsView(FlowDocument doc) : base(doc)
        {
            // listen to Data Received Errors events
            OpeningExplorer.OpeningStatsErrorReceived += OpeningStatsErrorReceived;
            TablebaseExplorer.TablebaseReceived += TablebaseDataReceived;

            CreateOpeningStatsTable();
        }

        // column widths in the stats table
        private readonly double _moveColumnWidth = 20;
        private readonly double _totalGamesColumnWidth = 20;
        // 100 for the percentage bar and 10 for the left margin
        private readonly double _statsColumnWidth = 110;

        private readonly double _tablebaseMoveColumnWidth = 60;
        private readonly double _dtzColumnWidth = 40;
        private readonly double _dtmColumnWidth = 40;

        // column widths in the stats table's header
        private readonly double _ecoColumnWidth = 20;
        private readonly double _openingNameColumnWidth = 130;

        private readonly string MOVE_PREFIX = "_move_";

        /// <summary>
        /// RichTextPara dictionary accessor
        /// </summary>
        override internal Dictionary<string, RichTextPara> RichTextParas { get { return _richTextParas; } }

        private static readonly string STYLE_WORKBOOK_TITLE = "workbook_title";

        // base font size for the control
        private int _baseFontSize = 11;

        /// <summary>
        /// Layout definitions for paragraphs at different levels.
        /// </summary>
        private readonly Dictionary<string, RichTextPara> _richTextParas = new Dictionary<string, RichTextPara>()
        {
            [STYLE_WORKBOOK_TITLE] = new RichTextPara(0, 10, 18, FontWeights.Bold, TextAlignment.Left),
            ["default"] = new RichTextPara(140, 5, 11, FontWeights.Normal, TextAlignment.Left),
        };

        /// <summary>
        /// The width of the stats table being a sum of
        /// the declared column widths.
        /// </summary>
        private double TotalStatsTableWidth
        {
            get => _moveColumnWidth + _totalGamesColumnWidth + _statsColumnWidth;
        }

        // Used to ensure that _node object is not accessed simultaneously by OpeningStatsReceived and SetOpeningName
        private object _lockNodeAccess = new object();

        /// <summary>
        /// Updates the view with received data.
        /// </summary>
        /// <param name="stats"></param>
        /// <param name="node"></param>
        /// <param name="treeId"></param>
        public void OpeningStatsReceived(LichessOpeningsStats stats, TreeNode node, int treeId)
        {
            lock (_lockNodeAccess)
            {
                _treeId = treeId;
                _node = node;
                if (_node != null)
                {
                    _moveNumberString = BuildMoveNumberString(_node);
                    BuildFlowDocument(DataMode.OPENINGS, stats);
                    if (stats.Opening != null)
                    {
                        if (_node.Eco != stats.Opening.Eco || _node.OpeningName != stats.Opening.Name)
                        {
                            _node.Eco = stats.Opening.Eco;
                            _node.OpeningName = stats.Opening.Name;
                            UpdateOpeningNameTable();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Rebuilds the view to show the error.
        /// </summary>
        /// <param name="message"></param>
        public void OpeningStatsErrorReceived(object sender, WebAccessEventArgs e)
        {
            BuildFlowDocument(DataMode.NO_DATA, null, e.Message);
        }

        /// <summary>
        /// Rebuilds the view when Openings data is received.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OpeningStatsReceived(object sender, WebAccessEventArgs e)
        {
            if (e.Success)
            {
                lock (_lockNodeAccess)
                {
                    _treeId = e.TreeId;
                    if (AppState.ActiveVariationTree != null)
                    {
                        _node = AppState.ActiveVariationTree.GetNodeFromNodeId(e.NodeId);
                    }
                    if (_node != null)
                    {
                        _moveNumberString = BuildMoveNumberString(_node);
                        BuildFlowDocument(DataMode.OPENINGS, e.OpeningStats);
                        if (e.OpeningStats.Opening != null)
                        {
                            if (_node.Eco != e.OpeningStats.Opening.Eco || _node.OpeningName != e.OpeningStats.Opening.Name)
                            {
                                _node.Eco = e.OpeningStats.Opening.Eco;
                                _node.OpeningName = e.OpeningStats.Opening.Name;
                                UpdateOpeningNameTable();
                            }
                        }
                    }
                }
            }
            else
            {
                BuildFlowDocument(DataMode.NO_DATA, null, e.Message);
            }
        }

        /// <summary>
        /// Sets opening code name on the node and refreshes UI table.
        /// </summary>
        public void SetOpeningName()
        {
            try
            {
                lock (_lockNodeAccess)
                {
                    _node = AppState.MainWin.ActiveVariationTree.SelectedNode;
                    string eco;
                    if (_node != null)
                    {
                        string opening = EcoUtils.GetOpeningNameFromDictionary(_node, out eco);
                        if (opening != null)
                        {
                            _node.Eco = eco;
                            _node.OpeningName = opening;
                        }
                        else
                        {
                            string openingName = FindOpeningNameFromPredecessors(_node, out eco);
                            if (!string.IsNullOrEmpty(openingName))
                            {
                                _node.Eco = eco;
                                _node.OpeningName = openingName;
                            }
                        }

                        if (NodeHasOpeningName(_node))
                        {
                            UpdateOpeningNameTable();
                        }
                    }
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Updates the opening name table
        /// </summary>
        private void UpdateOpeningNameTable()
        {
            UpdateEcoCodeLabel();
            UpdateOpeningNameLabel();
        }

        /// <summary>
        /// Updates the name of the opening in the table
        /// </summary>
        private void UpdateOpeningNameLabel()
        {
            string openingName;

            if (_node != null && _lblOpeningName != null)
            {
                openingName = _node.OpeningName;
                if (string.IsNullOrEmpty(openingName) || openingName == POSITION_NOT_NAMED)
                {
                    openingName = string.Empty;
                }

                _lblOpeningName.Content = openingName;
            }
        }

        /// <summary>
        /// Updates the ECO in the table
        /// </summary>
        private void UpdateEcoCodeLabel()
        {
            string eco;

            if (_node != null && _lblEcoCode != null)
            {
                eco = _node.Eco;
                if (string.IsNullOrEmpty(eco))
                {
                    eco = string.Empty;
                }

                _lblEcoCode.Content = eco;
            }
        }

        /// <summary>
        /// Rebuilds the view when Tablebase data is received.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void TablebaseDataReceived(object sender, WebAccessEventArgs e)
        {
            if (e.Success)
            {
                _treeId = e.TreeId;
                if (AppState.ActiveVariationTree != null)
                {
                    _node = AppState.ActiveVariationTree.GetNodeFromNodeId(e.NodeId);
                }
                BuildFlowDocument(DataMode.TABLEBASE, null);
            }
            else
            {
                BuildFlowDocument(DataMode.NO_DATA, null, e.Message);
            }
        }

        /// <summary>
        /// Builds a paragraph showing the passed error text.
        /// </summary>
        /// <param name="errorMessage"></param>
        /// <returns></returns>
        private Paragraph BuildErrorMessagePara(string errorMessage)
        {
            Paragraph para = new Paragraph();

            if (string.IsNullOrEmpty(errorMessage) || !errorMessage.ToLower().Contains("too many requests"))
            {
                Run rIntro = new Run(Properties.Resources.ErrorLichess + ": ");
                rIntro.FontSize = 14 + Configuration.FontSizeDiff;
                para.Inlines.Add(rIntro);

                Run rError = new Run("    " + errorMessage ?? ("[" + Properties.Resources.UnknownError + "]"));
                rError.FontSize = 12 + Configuration.FontSizeDiff;
                para.Inlines.Add(rError);
            }

            AppLog.Message("Error in lichess access: " + errorMessage ?? "[empty message]");

            return para;
        }

        /// <summary>
        /// Builds the flow for this view.
        /// Queries the Web Client for Openings Stats data to show in the 
        /// main table.
        /// </summary>
        private void BuildFlowDocument(DataMode mode, LichessOpeningsStats openingStats, string errorMessage = "")
        {
            Document.Blocks.Clear();
            Document.PageWidth = 590;

            if (_node != null)
            {
                switch (mode)
                {
                    case DataMode.OPENINGS:
                        BuildOpeningNameTable();
                        if (_openingNameTable != null)
                        {
                            Document.Blocks.Add(_openingNameTable);
                        }
                        BuildOpeningStatsTable(openingStats);
                        Document.Blocks.Add(_openingStatsTable);
                        break;
                    case DataMode.TABLEBASE:
                        if (_node.ColorToMove == PieceColor.White)
                        {
                            InsertTablebaseCategoryTable("loss");
                            InsertTablebaseCategoryTable("unknown");
                            InsertTablebaseCategoryTable("draw");
                            InsertTablebaseCategoryTable("win");
                        }
                        else
                        {
                            InsertTablebaseCategoryTable("loss");
                            InsertTablebaseCategoryTable("unknown");
                            InsertTablebaseCategoryTable("draw");
                            InsertTablebaseCategoryTable("win");
                        }
                        break;
                    case DataMode.NO_DATA:
                        BuildOpeningNameTable();
                        if (_openingNameTable != null)
                        {
                            Document.Blocks.Add(_openingNameTable);
                        }
                        Document.Blocks.Add(BuildErrorMessagePara(errorMessage));
                        break;
                }
            }
        }

        /// <summary>
        /// Checks if the node has a real Opening name.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private bool NodeHasOpeningName(TreeNode node)
        {
            return !string.IsNullOrEmpty(node.OpeningName) && node.OpeningName != POSITION_NOT_NAMED;
        }

        /// <summary>
        /// Finds the name of the opening and the Eco for
        /// a given node checking if there is a value in the 
        /// predecessors.
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="eco"></param>
        /// <returns></returns>
        private string FindOpeningNameFromPredecessors(TreeNode nd, out string eco)
        {
            string name = null;
            eco = "";

            while (nd != null)
            {
                //if (string.IsNullOrEmpty(nd.OpeningName) && nd.MoveNumber <= Constants.OPENING_MAX_MOVE)
                //{
                //    // the chain is "broken" yet and we don't want incorrect name
                //    // unless the move number is higher than OPENING_MAX_MOVE.
                //    break;
                //}

                name = nd.OpeningName;
                eco = nd.Eco;
                if (nd.OpeningName != POSITION_NOT_NAMED && !string.IsNullOrEmpty(nd.OpeningName))
                {
                    // we have a valid name
                    break;
                }
                else
                {
                    name = EcoUtils.GetOpeningNameFromDictionary(nd, out eco);
                    if (name != null)
                    {
                        // we have a valid name
                        break;
                    }
                    // keep looking 
                    nd = nd.Parent;
                }
            }

            if (name == POSITION_NOT_NAMED)
            {
                name = null;
            }

            return name;
        }

        //*************************************************************
        //
        //  OPENING STATS VIEW
        //
        //*************************************************************

        /// <summary>
        /// Builds the header table for the main table
        /// </summary>
        private void BuildOpeningNameTable()
        {
            if (_openingNameTable == null)
            {
                _openingNameTable = CreateTable(0);
                _openingNameTable.FontSize = _baseFontSize + 1 + Configuration.FontSizeDiff;
                _openingNameTable.CellSpacing = 0;
                _openingNameTable.Foreground = Brushes.Black;
                _openingNameTable.Background = ChessForgeColors.TABLE_HEADER_GREEN;
                _openingNameTable.RowGroups.Add(new TableRowGroup());

                _openingNameTable.Columns.Add(new TableColumn());
                _openingNameTable.Columns[0].Width = new GridLength(_ecoColumnWidth * scaleFactor);

                _openingNameTable.Columns.Add(new TableColumn());
                _openingNameTable.Columns[1].Width = new GridLength((_openingNameColumnWidth * scaleFactor) + 1);

                TableRow row = new TableRow();
                _openingNameTable.RowGroups[0].Rows.Add(row);

                TableCell cellEco = new TableCell(BuildEcoPara(""));
                cellEco.FontSize = _baseFontSize + 1 + Configuration.FontSizeDiff;
                cellEco.FontWeight = FontWeights.Bold;
                cellEco.Foreground = Brushes.Black;
                cellEco.Background = ChessForgeColors.TABLE_HEADER_GREEN;
                row.Cells.Add(cellEco);

                TableCell cellOpeningName = new TableCell(BuildOpeningNamePara(""));
                cellOpeningName.FontSize = _baseFontSize + 1 + Configuration.FontSizeDiff;
                cellOpeningName.Foreground = Brushes.Black;
                row.Cells.Add(cellOpeningName);
            }

            UpdateEcoCodeLabel();
            UpdateOpeningNameLabel();
        }

        /// <summary>
        /// Creates the Opening Stats table.
        /// Only called once at the start of the program and then reused.
        /// </summary>
        private void CreateOpeningStatsTable()
        {
            _openingStatsTable = CreateTable(0);
            _openingStatsTable.FontSize = _baseFontSize + 1 + Configuration.FontSizeDiff;
            _openingStatsTable.CellSpacing = 0;
            _openingStatsTable.RowGroups.Add(new TableRowGroup());

            CreateStatsTableColumns(scaleFactor);

            for (int i = 0; i < MAX_MOVE_ROW_COUNT; i++)
            {
                OpeningStatsViewRow row = new OpeningStatsViewRow(this);
                _lstRows.Add(row);
                _openingStatsTable.RowGroups[0].Rows.Add(row.Row);
            }
        }

        /// <summary>
        /// Adjusts the number of TableRows in the table.
        /// We don't construct or reconstruct the objects here but rather
        /// remove or add them to the Table.
        /// They are always kept the _lstRows list.
        /// </summary>
        /// <param name="moveCount"></param>
        private void AdjustGamesTableRowCount(int moveCount)
        {
            moveCount = Math.Min(MAX_MOVE_ROW_COUNT, moveCount);
            int currentRowCount = _openingStatsTable.RowGroups[0].Rows.Count;
            if (currentRowCount < moveCount)
            {
                for (int i = currentRowCount; i < moveCount; i++)
                {
                    _openingStatsTable.RowGroups[0].Rows.Add(_lstRows[i].Row);
                }
            }
            else
            {
                for (int i = currentRowCount - 1; i >= moveCount; i--)
                {
                    _openingStatsTable.RowGroups[0].Rows.Remove(_lstRows[i].Row);
                }
            }
        }


        /// <summary>
        /// Builds the main table with opening stats
        /// </summary>
        private void BuildOpeningStatsTable(LichessOpeningsStats stats)
        {
            int rowNo = 0;

            AdjustGamesTableRowCount(stats.Moves.Length);
            foreach (WebAccess.LichessMoveStats move in stats.Moves)
            {
                if (rowNo >= MAX_MOVE_ROW_COUNT)
                {
                    break;
                }

                TableRow row = _lstRows[rowNo].Row;
                _lstRows[rowNo].SetLabels(move, _moveNumberString, _node.ColorToMove);
                rowNo++;
            }
        }

        /// <summary>
        /// A move Run was clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void EventMoveClicked(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Run rMove = e.Source as Run;
                TabViewType tab = AppState.ActiveTab;
                if (tab == TabViewType.STUDY
                    || tab == TabViewType.MODEL_GAME
                    || tab == TabViewType.EXERCISE)
                {
                    string moveEngCode = GetMoveCodeFromCellName(rMove.Name);
                    MoveUtils.EngineNotationToCoords(moveEngCode, out _, out SquareCoords destSquare, out PieceType promoteTo);
                    UserMoveProcessor.ProcessMove(moveEngCode, out TreeNode node, out bool isCastle, out bool reportDupe);
                    UserMoveProcessor.PostMoveReporting(node, reportDupe);
                    AppState.MainWin.DisplayPosition(node);
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("EventMoveClicked()", ex);
            }

            e.Handled = true;
        }

        /// <summary>
        /// Gets the move notation from the name of the cell.
        /// The name should consist of the MOVE_PREFIX prefix
        /// and the move code.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private string GetMoveCodeFromCellName(string name)
        {
            return name.Substring(MOVE_PREFIX.Length);
        }

        /// <summary>
        /// Builds the move number string to suffix the moves in the table with.
        /// </summary>
        /// <param name="nd"></param>
        /// <returns></returns>
        private string BuildMoveNumberString(TreeNode nd)
        {
            if (AppState.ActiveTab == TabViewType.INTRO)
            {
                return nd.ColorToMove == ChessPosition.PieceColor.Black ? "#..." : "#.";
            }

            StringBuilder sb = new StringBuilder();
            
            uint moveNumberOffset = 0;
            if (AppState.ActiveVariationTree != null)
            {
                moveNumberOffset = AppState.ActiveVariationTree.MoveNumberOffset;
            }
            if (nd.ColorToMove == ChessPosition.PieceColor.Black)
            {
                sb.Append((nd.MoveNumber + moveNumberOffset).ToString() + "...");
            }
            else
            {
                sb.Append((nd.MoveNumber + moveNumberOffset + 1).ToString() + ".");
            }

            return sb.ToString();
        }


        /// <summary>
        /// Creates columns in the table.
        /// </summary>
        /// <param name="scaleFactor"></param>
        private void CreateStatsTableColumns(double scaleFactor)
        {
            // Move
            _openingStatsTable.Columns.Add(new TableColumn());
            _openingStatsTable.Columns[0].Width = new GridLength(_moveColumnWidth * scaleFactor);

            // Total games
            _openingStatsTable.Columns.Add(new TableColumn());
            _openingStatsTable.Columns[1].Width = new GridLength(_totalGamesColumnWidth * scaleFactor);

            // Scoring
            _openingStatsTable.Columns.Add(new TableColumn());
            _openingStatsTable.Columns[2].Width = new GridLength(_statsColumnWidth * scaleFactor + 1);
        }

        /// <summary>
        /// Builds Paragraph with the name of the Opening.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private Paragraph BuildOpeningNamePara(string name)
        {
            Paragraph para = new Paragraph();

            Canvas canvas = new Canvas
            {
                Width = scaleFactor * (TotalStatsTableWidth - _ecoColumnWidth) + 1,
                Height = 22 + Configuration.FontSizeDiff,
                Background = Brushes.White
            };

            _lblOpeningName = new Label
            {
                Width = scaleFactor * (TotalStatsTableWidth - _ecoColumnWidth) + 1,
                Height = 22 + Configuration.FontSizeDiff,
                FontSize = _baseFontSize + 1 + Configuration.FontSizeDiff,
                VerticalContentAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Content = name,

                BorderThickness = new Thickness(0, 0, 0, 0),
                Padding = new Thickness(0, 0, 0, 0)
            };

            _lblOpeningName.Foreground = Brushes.Black;
            _lblOpeningName.Background = ChessForgeColors.TABLE_HEADER_GREEN;

            canvas.Children.Add(_lblOpeningName);

            Canvas.SetLeft(_lblOpeningName, 0 * scaleFactor);

            InlineUIContainer uIContainer = new InlineUIContainer
            {
                Child = canvas
            };
            para.Inlines.Add(uIContainer);

            return para;
        }

        /// <summary>
        /// Builds Paragraph showing the ECO code.
        /// </summary>
        /// <param name="eco"></param>
        /// <returns></returns>
        private Paragraph BuildEcoPara(string eco)
        {
            Paragraph para = new Paragraph();

            Canvas canvas = new Canvas
            {
                Width = scaleFactor * _ecoColumnWidth,
                Height = 22 + Configuration.FontSizeDiff,
                Background = Brushes.White
            };

            _lblEcoCode = new Label
            {
                Width = scaleFactor * (_ecoColumnWidth),
                Height = 22 + Configuration.FontSizeDiff,
                FontSize = _baseFontSize + 1 + Configuration.FontSizeDiff,
                VerticalContentAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Content = "  " + eco,

                BorderThickness = new Thickness(0, 0, 0, 0),
                Padding = new Thickness(0, 0, 0, 0)
            };

            _lblEcoCode.Foreground = Brushes.Black;
            _lblEcoCode.Background = ChessForgeColors.TABLE_HEADER_GREEN;

            canvas.Children.Add(_lblEcoCode);

            Canvas.SetLeft(_lblEcoCode, 0 * scaleFactor);

            InlineUIContainer uIContainer = new InlineUIContainer
            {
                Child = canvas
            };
            para.Inlines.Add(uIContainer);

            return para;
        }


        //*************************************************************
        //
        //  TABLEBASE VIEW
        //
        //*************************************************************

        /// <summary>
        /// Creates the header table and the data table for a given category.
        /// Inserts both into the document.
        /// </summary>
        /// <param name="category"></param>
        private void InsertTablebaseCategoryTable(string category)
        {
            Table header = BuildTablebaseCategoryHeader(category);
            Table moves = BuildTablebaseCategoryMoves(category);
            if (moves.RowGroups[0].Rows.Count > 0)
            {
                Document.Blocks.Add(header);
                Document.Blocks.Add(moves);
            }
        }

        /// <summary>
        /// Builds the Tablebase moves table with
        /// one move per row.
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        private Table BuildTablebaseCategoryMoves(string category)
        {
            Table table = CreateTable(0);
            table.FontSize = _baseFontSize + 1 + Configuration.FontSizeDiff;
            table.CellSpacing = 0;
            table.Foreground = Brushes.Black;
            table.Background = Brushes.White;
            table.RowGroups.Add(new TableRowGroup());

            table.Columns.Add(new TableColumn());
            table.Columns[0].Width = new GridLength(_tablebaseMoveColumnWidth * scaleFactor);

            table.Columns.Add(new TableColumn());
            table.Columns[1].Width = new GridLength(_dtzColumnWidth * scaleFactor);

            table.Columns.Add(new TableColumn());
            table.Columns[2].Width = new GridLength(_dtmColumnWidth * scaleFactor);

            LichessTablebaseMove[] moves = TablebaseExplorer.Response.Moves;
            foreach (LichessTablebaseMove move in moves)
            {
                if (move.category == category)
                {
                    TableRow row = new TableRow();
                    table.RowGroups[0].Rows.Add(row);

                    Run rMove = new Run(move.San);
                    rMove.MouseLeftButtonDown += EventMoveClicked;
                    rMove.Name = MOVE_PREFIX + move.Uci;
                    rMove.Cursor = Cursors.Arrow;

                    TableCell cellMove = new TableCell(new Paragraph(rMove));
                    row.Cells.Add(cellMove);

                    Run rDtz = new Run(GetDtzText(move, category));
                    rDtz.FontSize = _baseFontSize + Configuration.FontSizeDiff;
                    TableCell cellDtz = new TableCell(new Paragraph(rDtz));
                    row.Cells.Add(cellDtz);

                    Run rDtm = new Run(GetDtmText(move, category));
                    rDtm.FontSize = _baseFontSize + Configuration.FontSizeDiff;
                    TableCell cellDtm = new TableCell(new Paragraph(rDtm));
                    row.Cells.Add(cellDtm);
                }
            }

            return table;
        }


        /// <summary>
        /// Gets text for the DTM cell.
        /// </summary>
        /// <param name="move"></param>
        /// <returns></returns>
        private string GetDtmText(LichessTablebaseMove move, string category)
        {
            string txt = "";
            if (move.Checkmate)
            {
                txt = Properties.Resources.OebCheckmate;
            }
            else if (move.Stalemate)
            {
                txt = Properties.Resources.OebStalemate;
            }
            else if (move.Insufficient_material)
            {
                txt = Properties.Resources.OebInsufficientMaterial;
            }
            else if (category == "draw")
            {
                txt = Properties.Resources.OebDraw;
            }
            else if (move.dtm != null && category != "unknown")
            {
                txt += "DTM " + Math.Abs(move.dtm.Value).ToString();
            }

            return txt;
        }

        /// <summary>
        /// Gets text for the DTZ cell.
        /// </summary>
        /// <param name="move"></param>
        /// <returns></returns>
        private string GetDtzText(LichessTablebaseMove move, string category)
        {
            if (move.dtz != null && category != "draw" && category != "unknown")
            {
                return "DTZ " + Math.Abs(move.dtz.Value).ToString();
            }
            else
            {
                return "";
            }
        }

        /// <summary>
        /// Builds a Table for the Tablebase view header.
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        private Table BuildTablebaseCategoryHeader(string category)
        {
            Table table = CreateTable(0);
            table.FontSize = _baseFontSize + 1 + Configuration.FontSizeDiff;
            table.CellSpacing = 0;
            table.Foreground = Brushes.Black;
            table.Background = ChessForgeColors.TABLE_HEADER_GREEN;
            table.RowGroups.Add(new TableRowGroup());

            table.Columns.Add(new TableColumn());
            table.Columns[0].Width = new GridLength((_ecoColumnWidth + _openingNameColumnWidth) * scaleFactor + 1);

            TableRow row = new TableRow();
            table.RowGroups[0].Rows.Add(row);

            string title;
            switch (category)
            {
                case "win":
                    title = Properties.Resources.OebLosing;
                    break;
                case "unknown":
                    title = Properties.Resources.OebUnknown;
                    break;
                case "draw":
                    title = Properties.Resources.OebDrawing;
                    break;
                case "loss":
                    title = Properties.Resources.OebWinning;
                    break;
                default:
                    title = "-";
                    break;
            }

            TableCell cellTitle = new TableCell(new Paragraph(new Run(title)));
            cellTitle.FontSize = _baseFontSize + 1 + Configuration.FontSizeDiff;
            cellTitle.FontWeight = FontWeights.Bold;
            cellTitle.Foreground = Brushes.Black;
            cellTitle.Background = ChessForgeColors.TABLE_HEADER_GREEN;
            row.Cells.Add(cellTitle);

            return table;
        }

    }
}
