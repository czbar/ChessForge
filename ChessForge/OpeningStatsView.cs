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
using WebAccess;

namespace ChessForge
{
    public class OpeningStatsView : RichTextBuilder
    {
        // The main table holding the stats
        private Table _openingStatsTable;

        /// <summary>
        /// Creates the view and registers a listener with WebAccess
        /// </summary>
        /// <param name="doc"></param>
        public OpeningStatsView(FlowDocument doc) : base(doc)
        {
            // listen to Data Received events
            OpeningExplorer.DataReceived += OpeningStatsReceived;
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
                BuildFlowDocument();
            }
        }

        /// <summary>
        /// RichTextPara dictionary accessor
        /// </summary>
        override internal Dictionary<string, RichTextPara> RichTextParas { get { return _richTextParas; } }

        private static readonly string STYLE_WORKBOOK_TITLE = "workbook_title";

        /// <summary>
        /// Layout definitions for paragraphs at different levels.
        /// </summary>
        private Dictionary<string, RichTextPara> _richTextParas = new Dictionary<string, RichTextPara>()
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

            BuildOpeningStatsTable();

            Document.Blocks.Add(_openingStatsTable);
        }

        /// <summary>
        /// Builds the main table with opening stats
        /// </summary>
        private void BuildOpeningStatsTable()
        {
            _openingStatsTable = CreateTable(0);
            _openingStatsTable.FontSize = 14 + Configuration.FontSizeDiff;
            _openingStatsTable.CellSpacing = 2;
            _openingStatsTable.RowGroups.Add(new TableRowGroup());

            // get the data
            LichessOpeningsStats stats = WebAccess.OpeningExplorer.Stats;

            double scaleFactor = 3.5;
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
                TableCell cellMove = new TableCell(new Paragraph(new Run(move.San)));
                row.Cells.Add(cellMove);

                int whiteWins = int.Parse(move.White);
                int draws = int.Parse(move.Draws);
                int blackWins = int.Parse(move.Black);

                int totalGames = whiteWins + draws + blackWins;

                int whiteWinsPercent = (int)Math.Round((double)(whiteWins * 100) / (double)totalGames);
                int blackWinsPercent = (int)Math.Round((double)(blackWins * 100) / (double)totalGames);
                int drawsPercent = 100 - (whiteWinsPercent + blackWinsPercent);

                TableCell cellTotal = new TableCell(new Paragraph(new Run((int.Parse(move.White) + int.Parse(move.Draws) + int.Parse(move.Black)).ToString("N0"))));
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
            _openingStatsTable.Columns[0].Width = new GridLength(20 * scaleFactor);

            // Total games
            _openingStatsTable.Columns.Add(new TableColumn());
            _openingStatsTable.Columns[1].Width = new GridLength(20 * scaleFactor);

            // Scoring
            _openingStatsTable.Columns.Add(new TableColumn());
            _openingStatsTable.Columns[2].Width = new GridLength(110 * scaleFactor);
        }

        /// <summary>
        /// Creates a label showing the percentage value
        /// </summary>
        /// <param name="pct"></param>
        /// <param name="scaleFactor"></param>
        /// <returns></returns>
        private Label BuildPercentLabel(int pct, double scaleFactor)
        {
            Label lbl = new Label();
            lbl.Width = pct * scaleFactor;
            lbl.Height = 20;
            lbl.FontSize = 12;
            lbl.VerticalContentAlignment = VerticalAlignment.Center;
            lbl.HorizontalContentAlignment = HorizontalAlignment.Center;
            lbl.Content = pct.ToString() + "%";

            lbl.BorderThickness = new Thickness(0, 0, 0, 0);
            lbl.Padding = new Thickness(0, 0, 0, 0);

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

            Canvas canvas = new Canvas();
            canvas.Width = scaleFactor * 110;
            canvas.Height = 20;
            canvas.Background = Brushes.White;

            Label lblWhite = BuildPercentLabel(pctWhite, scaleFactor);
            Label lblDraws = BuildPercentLabel(pctDraws, scaleFactor);
            Label lblBlack = BuildPercentLabel(pctBlack, scaleFactor);

            lblWhite.Background = Brushes.LightGray;

            lblDraws.Background = Brushes.Gray;
            lblDraws.Foreground = Brushes.Black;

            lblBlack.Background = Brushes.Black;
            lblBlack.Foreground = Brushes.White;

            canvas.Children.Add(lblWhite);
            canvas.Children.Add(lblDraws);
            canvas.Children.Add(lblBlack);

            Canvas.SetLeft(lblWhite, 10 * scaleFactor);
            Canvas.SetLeft(lblDraws, Canvas.GetLeft(lblWhite) + lblWhite.Width);
            Canvas.SetLeft(lblBlack, Canvas.GetLeft(lblDraws) + lblDraws.Width);

            InlineUIContainer uIContainer = new InlineUIContainer();
            uIContainer.Child = canvas;
            para.Inlines.Add(uIContainer);


            return para;
        }
    }
}
