using ChessPosition;
using GameTree;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using WebAccess;
using Label = System.Windows.Controls.Label;

namespace ChessForge
{
    /// <summary>
    /// The Rich Text Box view for the list of Top Games obtained from lichess.org
    /// </summary>
    public class TopGamesView : RichTextBuilder
    {
        /// <summary>
        /// RichTextPara dictionary accessor
        /// </summary>
        override internal Dictionary<string, RichTextPara> RichTextParas { get { return _richTextParas; } }

        /// <summary>
        /// Layout definitions for paragraphs at different levels.
        /// </summary>
        private readonly Dictionary<string, RichTextPara> _richTextParas = new Dictionary<string, RichTextPara>()
        {
        };

        // columns widths
        private int _ratingColumnWidth = 30;
        private int _namesColumnWidth = 150;
        private int _resultColumnWidth = 35;
        private int _dateColumnWidth = 40;

        private int _tableWidth = 260;

        // base font size for the control
        private int _baseFontSize = 11;

        // prefix for the Rows' names
        private readonly string _rowNamePrefix = "name_";

        /// <summary>
        /// Creates the view and registers a listener with WebAccess
        /// </summary>
        /// <param name="doc"></param>
        public TopGamesView(FlowDocument doc) : base(doc)
        {
            // listen to Data Received events
            OpeningExplorer.DataReceived += TopGamesReceived;
            TablebaseExplorer.DataReceived += TablebaseDataReceived;
        }

        /// <summary>
        /// Event handlers requesting the build the view 
        /// when data is received from Lichess.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void TopGamesReceived(object sender, WebAccessEventArgs e)
        {
            if (e.Success)
            {
                BuildFlowDocument();
            }
            else
            {
                ClearDocument();
            }
        }

        /// <summary>
        /// If Tablebase data was received there is nothing to show in this view
        /// and we want to cleare if anything was shown.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void TablebaseDataReceived(object sender, WebAccessEventArgs e)
        {
            ClearDocument();
        }

        /// <summary>
        /// Builds the Flow Document for this view.
        /// main table.
        /// </summary>
        public void BuildFlowDocument()
        {
            Document.Blocks.Clear();
            Document.PageWidth = 590;

            Document.Blocks.Add(BuildHeaderLabel());
            Document.Blocks.Add(BuildTopGamesTable());
        }

        /// <summary>
        /// Builds an empty document.
        /// </summary>
        public void ClearDocument()
        {
            Document.Blocks.Clear();
        }

        /// <summary>
        /// Builds the main Top Games table.
        /// </summary>
        /// <returns></returns>
        private Table BuildTopGamesTable()
        {
            Table gamesTable = new Table();
            gamesTable.FontSize = _baseFontSize + Configuration.FontSizeDiff;
            gamesTable.CellSpacing = 0;
            gamesTable.Margin = new Thickness(0);
            gamesTable.RowGroups.Add(new TableRowGroup());

            CreateColumns(gamesTable);
            LichessOpeningsStats stats = WebAccess.OpeningExplorer.Stats;
            int rowNo = 0;
            foreach (LichessTopGame game in stats.TopGames)
            {
                TableRow row = BuildGameRow(gamesTable, game, rowNo);
                gamesTable.RowGroups[0].Rows.Add(row);
                if (!string.IsNullOrWhiteSpace(game.Id))
                {
                    row.Name = _rowNamePrefix + game.Id;
                    row.PreviewMouseDown += Row_PreviewMouseDown;
                    row.Cursor = Cursors.Hand;
                }
                rowNo++;
            }

            return gamesTable;
        }

        /// <summary>
        /// Handler of the mouse click on the Row event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Row_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is TableRow)
            {
                string id = ((TableRow)sender).Name;
                System.Diagnostics.Process.Start("https://lichess.org/" + id.Substring(_rowNamePrefix.Length));
            }
        }

        /// <summary>
        /// Builds a single Row.
        /// </summary>
        /// <param name="gamesTable"></param>
        /// <param name="game"></param>
        /// <param name="rowNo"></param>
        /// <returns></returns>
        private TableRow BuildGameRow(Table gamesTable, LichessTopGame game, int rowNo)
        {
            TableRow row = new TableRow();
            if (rowNo % 2 == 0)
            {
                row.Background = Brushes.White;
            }
            else
            {
                row.Background = ChessForgeColors.TABLE_ROW_LIGHT_GRAY;
            }

            TableCell cellRatings = new TableCell(BuildRatingsPara(gamesTable, game));
            row.Cells.Add(cellRatings);


            TableCell cellNames = new TableCell(BuildNamesPara(gamesTable, game));
            row.Cells.Add(cellNames);

            TableCell cellResult = new TableCell(BuildResultPara(gamesTable, game));
            row.Cells.Add(cellResult);

            TableCell cellDate = new TableCell(BuildDatePara(gamesTable, game));
            row.Cells.Add(cellDate);

            return row;
        }

        /// <summary>
        /// Builds a Paragraph with the Players' ratings.
        /// </summary>
        /// <param name="gamesTable"></param>
        /// <param name="game"></param>
        /// <returns></returns>
        private Paragraph BuildRatingsPara(Table gamesTable, LichessTopGame game)
        {
            Paragraph para = new Paragraph();

            Canvas canvas = new Canvas
            {
                Width = _namesColumnWidth,
                Height = 44 + Configuration.FontSizeDiff,
            };

            string whiteRating = game.White.Rating ?? "";
            string blackRating = game.Black.Rating ?? "";

            Label lblWhite = new Label
            {
                Width = _namesColumnWidth,
                Height = 20 + Configuration.FontSizeDiff,
                FontSize = _baseFontSize + Configuration.FontSizeDiff,
                VerticalContentAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Content = whiteRating,

                BorderThickness = new Thickness(0, 0, 0, 0),
                Padding = new Thickness(0, 0, 0, 0)
            };
            canvas.Children.Add(lblWhite);
            Canvas.SetLeft(lblWhite, 0);
            Canvas.SetTop(lblWhite, 2);

            Label lblBlack = new Label
            {
                Width = _namesColumnWidth,
                Height = 20 + Configuration.FontSizeDiff,
                FontSize = _baseFontSize + Configuration.FontSizeDiff,
                VerticalContentAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Content = blackRating,

                BorderThickness = new Thickness(0, 0, 0, 0),
                Padding = new Thickness(0, 0, 0, 0)
            };
            canvas.Children.Add(lblBlack);
            Canvas.SetLeft(lblBlack, 0);
            Canvas.SetTop(lblBlack, 20);

            InlineUIContainer uIContainer = new InlineUIContainer
            {
                Child = canvas
            };
            para.Inlines.Add(uIContainer);

            return para;
        }

        /// <summary>
        /// Builds a Paragraph with Players' names
        /// </summary>
        /// <param name="gamesTable"></param>
        /// <param name="game"></param>
        /// <returns></returns>
        private Paragraph BuildNamesPara(Table gamesTable, LichessTopGame game)
        {
            Paragraph para = new Paragraph();

            Canvas canvas = new Canvas
            {
                Width = _namesColumnWidth,
                Height = 44 + Configuration.FontSizeDiff,
            };

            Label lblWhite = new Label
            {
                Width = _namesColumnWidth - 8,
                Height = 20 + Configuration.FontSizeDiff,
                FontSize = _baseFontSize + Configuration.FontSizeDiff,
                VerticalContentAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Content = game.White.Name,
                ToolTip = game.White.Name,

                BorderThickness = new Thickness(0, 0, 0, 0),
                Padding = new Thickness(0, 0, 0, 0)
            };
            canvas.Children.Add(lblWhite);
            Canvas.SetLeft(lblWhite, 4);
            Canvas.SetTop(lblWhite, 2);

            Label lblBlack = new Label
            {
                Width = _namesColumnWidth - 8,
                Height = 20 + Configuration.FontSizeDiff,
                FontSize = _baseFontSize + Configuration.FontSizeDiff,
                VerticalContentAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Content = game.Black.Name,
                ToolTip = game.Black.Name,

                BorderThickness = new Thickness(0, 0, 0, 0),
                Padding = new Thickness(0, 0, 0, 0)
            };
            canvas.Children.Add(lblBlack);
            Canvas.SetLeft(lblBlack, 4);
            Canvas.SetTop(lblBlack, 20);

            InlineUIContainer uIContainer = new InlineUIContainer
            {
                Child = canvas
            };
            para.Inlines.Add(uIContainer);

            return para;
        }

        /// <summary>
        /// Builds a Paragraph with a string representing the result.
        /// </summary>
        /// <param name="gamesTable"></param>
        /// <param name="game"></param>
        /// <returns></returns>
        private Paragraph BuildResultPara(Table gamesTable, LichessTopGame game)
        {
            Paragraph para = new Paragraph();

            Canvas canvas = new Canvas
            {
                Width = _resultColumnWidth,
                Height = 44 + Configuration.FontSizeDiff,
            };

            Label lblResult = new Label
            {
                Width = _resultColumnWidth,
                Height = 20 + Configuration.FontSizeDiff,
                FontSize = _baseFontSize - 1 + Configuration.FontSizeDiff,
                VerticalContentAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center,

                BorderThickness = new Thickness(0, 0, 0, 0),
                Padding = new Thickness(0, 0, 0, 0)
            };

            Style style = AppStateManager.MainWin.FindResource("RoundedBorder") as Style;
            lblResult.Style = style;

            switch (game.Winner)
            {
                case "white":
                    lblResult.Content = "1-0";
                    lblResult.Background = ChessForgeColors.WhiteWinLinearBrush;
                    break;
                case "black":
                    lblResult.Content = "0-1";
                    lblResult.Background = ChessForgeColors.BlackWinLinearBrush;
                    lblResult.Foreground = Brushes.White;
                    break;
                default:
                    lblResult.Content = Constants.HalfPoint.ToString() + "-" + Constants.HalfPoint.ToString();
                    lblResult.Background = ChessForgeColors.DrawLinearBrush;
                    lblResult.Foreground = Brushes.White;
                    break;
            }

            canvas.Children.Add(lblResult);
            Canvas.SetLeft(lblResult, 0);
            Canvas.SetTop(lblResult, (canvas.Height - 20) / 2);

            InlineUIContainer uIContainer = new InlineUIContainer
            {
                Child = canvas
            };
            para.Inlines.Add(uIContainer);

            return para;
        }

        /// <summary>
        /// Builds a Paragraph with a string representing the date.
        /// </summary>
        /// <param name="gamesTable"></param>
        /// <param name="game"></param>
        /// <returns></returns>
        private Paragraph BuildDatePara(Table gamesTable, LichessTopGame game)
        {
            Paragraph para = new Paragraph();

            Canvas canvas = new Canvas
            {
                Width = _resultColumnWidth,
                Height = 44 + Configuration.FontSizeDiff,
            };

            Label lblDate = new Label
            {
                Width = _dateColumnWidth - 6,
                Height = 20 + Configuration.FontSizeDiff,
                FontSize = _baseFontSize - 1 + Configuration.FontSizeDiff,
                VerticalContentAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Content = game.Year ?? "",

                BorderThickness = new Thickness(0, 0, 0, 0),
                Padding = new Thickness(0, 0, 0, 0)
            };

            canvas.Children.Add(lblDate);
            Canvas.SetLeft(lblDate, 0);
            Canvas.SetTop(lblDate, (canvas.Height - 20) / 2);

            InlineUIContainer uIContainer = new InlineUIContainer
            {
                Child = canvas
            };
            para.Inlines.Add(uIContainer);

            return para;
        }

        /// <summary>
        /// Creates columns for the main Top Games table
        /// </summary>
        /// <param name="gamesTable"></param>
        private void CreateColumns(Table gamesTable)
        {
            // ratings
            gamesTable.Columns.Add(new TableColumn());
            gamesTable.Columns[0].Width = new GridLength(_ratingColumnWidth);

            // names
            gamesTable.Columns.Add(new TableColumn());
            gamesTable.Columns[1].Width = new GridLength(_namesColumnWidth);

            // result
            gamesTable.Columns.Add(new TableColumn());
            gamesTable.Columns[2].Width = new GridLength(_resultColumnWidth);

            // date
            gamesTable.Columns.Add(new TableColumn());
            gamesTable.Columns[3].Width = new GridLength(_dateColumnWidth);
        }

        /// <summary>
        /// Builds the header Paragraph.
        /// </summary>
        /// <returns></returns>
        private Paragraph BuildHeaderLabel()
        {
            Paragraph para = new Paragraph();
            para.Margin = new Thickness(0, 0, 0, 0);

            Canvas canvas = new Canvas
            {
                Width = 260,
                Height = 22 + Configuration.FontSizeDiff,
                Background = Brushes.White
            };

            Label lbl = new Label
            {
                Width = _tableWidth,
                Height = 22 + Configuration.FontSizeDiff,
                FontSize = _baseFontSize + 1 + Configuration.FontSizeDiff,
                VerticalContentAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Content = "  Top Games",

                BorderThickness = new Thickness(0, 0, 0, 0),
                Padding = new Thickness(0, 0, 0, 0)
            };

            lbl.Background = ChessForgeColors.TABLE_HEADER_GREEN;

            canvas.Children.Add(lbl);

            Canvas.SetLeft(lbl, 0);

            InlineUIContainer uIContainer = new InlineUIContainer
            {
                Child = canvas
            };
            para.Inlines.Add(uIContainer);

            return para;
        }
    }

}
