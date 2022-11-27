using GameTree;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Xml.Linq;
using WebAccess;

namespace ChessForge
{
    public class OpeningStatsView : RichTextBuilder
    {
        // scale factor for table cell sizes
        private double scaleFactor = 3.5;

        // The top table showing the name of the opening 
        private Table _openingNameTable;

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
        /// Creates the view and registers a listener with WebAccess
        /// </summary>
        /// <param name="doc"></param>
        public OpeningStatsView(FlowDocument doc) : base(doc)
        {
            // listen to Data Received events
            OpeningExplorer.DataReceived += OpeningStatsReceived;
        }

        // column widths in the stats table
        private readonly double _moveColumnWidth = 20;
        private readonly double _totalGamesColumnWidth = 20;
        // 100 for the percentage bar and 10 for the left margin
        private readonly double _statsColumnWidth = 110;
 
        // column widths in the stats table's header
        private readonly double _ecoColumnWidth = 20;
        private readonly double _openingNameColumnWidth = 130;

        /// <summary>
        /// The width of the stats table being a sum of
        /// the declared column widths.
        /// </summary>
        private double TotalStatsTableWidth
        {
            get => _moveColumnWidth + _totalGamesColumnWidth + _statsColumnWidth;
        }

        /// <summary>
        /// Rebuilds the view when data is received.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OpeningStatsReceived(object sender, WebAccessEventArgs e)
        {
            if (e.Success)
            {
                _treeId = e.TreeId;
                if (AppStateManager.ActiveVariationTree != null)
                {
                    _node = AppStateManager.ActiveVariationTree.GetNodeFromNodeId(e.NodeId);
                }
                _moveNumberString = BuildMoveNumberString(_node);
                BuildFlowDocument();
            }
        }

        /// <summary>
        /// Builds the move number string to suffix the moves in the table with.
        /// </summary>
        /// <param name="nd"></param>
        /// <returns></returns>
        private string BuildMoveNumberString(TreeNode nd)
        {
            StringBuilder sb = new StringBuilder();
            if (nd.ColorToMove == ChessPosition.PieceColor.Black)
            {
                sb.Append(nd.MoveNumber.ToString() + "...");
            }
            else
            {
                sb.Append((nd.MoveNumber + 1).ToString() + ".");
            }

            return sb.ToString();
        }

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
            [STYLE_WORKBOOK_TITLE] = new RichTextPara(0, 10, 18, FontWeights.Bold, null, TextAlignment.Left),
            ["default"] = new RichTextPara(140, 5, 11, FontWeights.Normal, null, TextAlignment.Left),
        };

        /// <summary>
        /// Builds the flow for this view.
        /// Queries the Web Client for Openings Stats data to show in the 
        /// main table.
        /// </summary>
        public void BuildFlowDocument()
        {
            Document.Blocks.Clear();
            Document.PageWidth = 590;

            BuildOpeningNameTable();
            if (_openingNameTable != null)
            {
                Document.Blocks.Add(_openingNameTable);
            }

            BuildOpeningStatsTable();
            Document.Blocks.Add(_openingStatsTable);
        }

        /// <summary>
        /// Builds the header table for the main table
        /// </summary>
        private void BuildOpeningNameTable()
        {
            // get the data
            LichessOpeningsStats stats = WebAccess.OpeningExplorer.Stats;
            string eco;
            string openingName;
            if (stats.Opening == null)
            {
                eco = "-";
                openingName = "";
            }
            else
            {
                eco = stats.Opening.Eco;
                openingName = stats.Opening.Name;
            }

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

            TableCell cellEco = new TableCell(BuildEcoPara(eco));
            cellEco.FontSize = _baseFontSize + 1 + Configuration.FontSizeDiff;
            cellEco.FontWeight = FontWeights.Bold;
            cellEco.Foreground = Brushes.Black;
            cellEco.Background = ChessForgeColors.TABLE_HEADER_GREEN;
            row.Cells.Add(cellEco);

            TableCell cellOpeningName = new TableCell(BuildOpeningNamePara(openingName ?? ""));
            cellOpeningName.FontSize = _baseFontSize + 1 + Configuration.FontSizeDiff;
            cellOpeningName.Foreground = Brushes.Black;
            row.Cells.Add(cellOpeningName);
        }

        /// <summary>
        /// Builds the main table with opening stats
        /// </summary>
        private void BuildOpeningStatsTable()
        {
            _openingStatsTable = CreateTable(0);
            _openingStatsTable.FontSize = _baseFontSize + 1 + Configuration.FontSizeDiff;
            _openingStatsTable.CellSpacing = 0;
            _openingStatsTable.RowGroups.Add(new TableRowGroup());

            // get the data
            LichessOpeningsStats stats = WebAccess.OpeningExplorer.Stats;

            CreateStatsTableColumns(scaleFactor);

            foreach (WebAccess.LichessMoveStats move in stats.Moves)
            {
                TableRow row = new TableRow();
                _openingStatsTable.RowGroups[0].Rows.Add(row);
                PopulateCellsInRow(row, move, scaleFactor);
            }

        }

        /// <summary>
        /// Populates cells in the passed row, using LichessMoveStats data
        /// </summary>
        /// <param name="row"></param>
        /// <param name="move"></param>
        /// <param name="scaleFactor"></param>
        private void PopulateCellsInRow(TableRow row, LichessMoveStats move, double scaleFactor)
        {
            try
            {
                TableCell cellMove = new TableCell(new Paragraph(new Run(_moveNumberString + move.San)));
                row.Cells.Add(cellMove);

                int whiteWins = int.Parse(move.White);
                int draws = int.Parse(move.Draws);
                int blackWins = int.Parse(move.Black);

                int totalGames = whiteWins + draws + blackWins;

                int whiteWinsPercent = (int)Math.Round((double)(whiteWins * 100) / (double)totalGames);
                int blackWinsPercent = (int)Math.Round((double)(blackWins * 100) / (double)totalGames);
                int drawsPercent = 100 - (whiteWinsPercent + blackWinsPercent);

                TableCell cellTotal = new TableCell(BuildTotalGamesPara(totalGames));
                cellTotal.FontSize = _baseFontSize + 1 + Configuration.FontSizeDiff;
                row.Cells.Add(cellTotal);

                TableCell cellScoring = new TableCell(CreatePercentBarToParagraph(whiteWinsPercent, drawsPercent, blackWinsPercent, scaleFactor));
                row.Cells.Add(cellScoring);
            }
            catch (Exception ex)
            {
                AppLog.Message("PopulateCellsInRow", ex);
            }
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
        /// Creates a label showing the percentage value
        /// </summary>
        /// <param name="pct"></param>
        /// <param name="scaleFactor"></param>
        /// <returns></returns>
        private Label BuildPercentLabel(int pct, double scaleFactor)
        {
            Label lbl = new Label
            {
                Width = pct * scaleFactor,
                Height = 14 + Configuration.FontSizeDiff,
                FontSize = _baseFontSize + Configuration.FontSizeDiff,
                VerticalContentAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Content = pct.ToString() + "%",

                BorderThickness = new Thickness(0, 0, 0, 0),
                Padding = new Thickness(0, 0, 0, 0)
            };

            return lbl;
        }

        /// <summary>
        /// Combines percentage labels into one "bar".
        /// </summary>
        /// <param name="pctWhite"></param>
        /// <param name="pctDraws"></param>
        /// <param name="pctBlack"></param>
        /// <param name="scaleFactor"></param>
        /// <returns></returns>
        private Paragraph CreatePercentBarToParagraph(int pctWhite, int pctDraws, int pctBlack, double scaleFactor)
        {
            Paragraph para = new Paragraph();

            Canvas canvas = new Canvas
            {
                Width = scaleFactor * 110 + 2,
                Height = 20 + Configuration.FontSizeDiff,
                Background = Brushes.White
            };

            Label lblWhite = BuildPercentLabel(pctWhite, scaleFactor);
            Label lblDraws = BuildPercentLabel(pctDraws, scaleFactor);
            Label lblBlack = BuildPercentLabel(pctBlack, scaleFactor);

            lblWhite.Background = ChessForgeColors.WhiteWinLinearBrush;

            lblDraws.Background = ChessForgeColors.DrawLinearBrush;
            lblDraws.Foreground = Brushes.White;

            lblBlack.Background = ChessForgeColors.BlackWinLinearBrush;
            lblBlack.Foreground = Brushes.White;


            Border border = new Border
            {
                BorderBrush = ChessForgeColors.TABLE_ROW_LIGHT_GRAY,
                Width = (100 * scaleFactor + 2),
                Height = 16 + Configuration.FontSizeDiff,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
            };

            canvas.Children.Add(lblWhite);
            canvas.Children.Add(lblDraws);
            canvas.Children.Add(lblBlack);
            canvas.Children.Add(border);

            Canvas.SetLeft(border, 10 * scaleFactor - 1);
            Canvas.SetLeft(lblWhite, 10 * scaleFactor);
            Canvas.SetLeft(lblDraws, Canvas.GetLeft(lblWhite) + lblWhite.Width);
            Canvas.SetLeft(lblBlack, Canvas.GetLeft(lblDraws) + lblDraws.Width);

            Canvas.SetTop(border, 2);
            Canvas.SetTop(lblWhite, 3);
            Canvas.SetTop(lblDraws, 3);
            Canvas.SetTop(lblBlack, 3);

            InlineUIContainer uIContainer = new InlineUIContainer
            {
                Child = canvas
            };
            para.Inlines.Add(uIContainer);


            return para;
        }

        /// <summary>
        /// Builds Paragraph showing the total number of games.
        /// </summary>
        /// <param name="totalGames"></param>
        /// <returns></returns>
        private Paragraph BuildTotalGamesPara(int totalGames)
        {
            Paragraph para = new Paragraph();

            Canvas canvas = new Canvas
            {
                Width = scaleFactor * (_totalGamesColumnWidth),
                Height = 20 + Configuration.FontSizeDiff,
                Background = Brushes.White
            };

            Label lbl = new Label
            {
                Width = scaleFactor * _totalGamesColumnWidth,
                Height = 18 + Configuration.FontSizeDiff,
                FontSize = _baseFontSize + 1 + Configuration.FontSizeDiff,
                VerticalContentAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Right,
                Content = totalGames.ToString("N0"),

                BorderThickness = new Thickness(0, 0, 0, 0),
                Padding = new Thickness(0, 0, 0, 0)
            };

            lbl.Background = Brushes.White;

            canvas.Children.Add(lbl);

            Canvas.SetLeft(lbl, 0 * scaleFactor);

            InlineUIContainer uIContainer = new InlineUIContainer
            {
                Child = canvas
            };
            para.Inlines.Add(uIContainer);


            return para;
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

            Label lbl = new Label
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

            lbl.Foreground = Brushes.Black;
            lbl.Background = ChessForgeColors.TABLE_HEADER_GREEN;

            canvas.Children.Add(lbl);

            Canvas.SetLeft(lbl, 0 * scaleFactor);

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

            Label lbl = new Label
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

            lbl.Foreground = Brushes.Black;
            lbl.Background = ChessForgeColors.TABLE_HEADER_GREEN;

            canvas.Children.Add(lbl);

            Canvas.SetLeft(lbl, 0 * scaleFactor);

            InlineUIContainer uIContainer = new InlineUIContainer
            {
                Child = canvas
            };
            para.Inlines.Add(uIContainer);

            return para;
        }
    }
}
