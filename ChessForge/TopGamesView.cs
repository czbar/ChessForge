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

        /// <summary>
        /// Currently selected game.
        /// </summary>
        public string CurrentGameId
        {
            get => _clickedGameId;
        }

        /// <summary>
        /// The Table shown in this view
        /// </summary>
        private Table _gamesTable;

        // maximum allowed number of rows
        private readonly int MAX_GAME_ROW_COUNT = 15;

        // List of objects that encapsulate TableRows used in the _gamesTable.
        private List<TopGamesViewRow> _lstRows = new List<TopGamesViewRow>();

        // Id of the last clicked game
        private string _clickedGameId;

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

        // lisy of game Ids listed in this view
        private List<string> _gameIdList = new List<string>();

        // true if this view is hosted in the main windows, false if in the Game Preview dialog
        private bool _isMainWin;

        /// <summary>
        /// Creates the view and registers a listener with WebAccess
        /// </summary>
        /// <param name="doc"></param>
        public TopGamesView(FlowDocument doc, bool mainWin) : base(doc)
        {
            _isMainWin = mainWin;
            // listen to Data Received Errors events
            OpeningExplorer.OpeningStatsErrorReceived += TopGamesErrorReceived;
            TablebaseExplorer.TablebaseReceived += TablebaseDataReceived;

            CreateTopGamesTable();
        }

        /// <summary>
        /// Creates a Table object for Games.
        /// The rows are precreated and their content is modified
        /// per the received data.
        /// </summary>
        private void CreateTopGamesTable()
        {
            _gamesTable = new Table();
            _lstRows.Clear();


            _gamesTable = new Table();
            _gamesTable.FontSize = _baseFontSize + Configuration.FontSizeDiff;
            _gamesTable.CellSpacing = 0;
            _gamesTable.Margin = new Thickness(0);
            _gamesTable.RowGroups.Add(new TableRowGroup());

            CreateColumns(_gamesTable);

            for (int i = 0; i < MAX_GAME_ROW_COUNT; i++)
            {
                TopGamesViewRow row = new TopGamesViewRow();
                _lstRows.Add(row);
                _gamesTable.RowGroups[0].Rows.Add(row.Row);
            }
        }

        /// <summary>
        /// Event handlers requesting the build the view 
        /// when data is received from Lichess.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void TopGamesReceived(LichessOpeningsStats stats)
        {
            LichessGamesPreviewDialog.SetOpeningsData(stats);
            BuildFlowDocument(stats);
        }


        /// <summary>
        /// Event handlers requesting the build the view 
        /// when data is received from Lichess.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void TopGamesErrorReceived(object sender, WebAccessEventArgs e)
        {
            ClearDocument();
        }

        /// <summary>
        /// If Tablebase data was received there is nothing to show in this view
        /// and we want to clear it if anything was shown.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void TablebaseDataReceived(object sender, WebAccessEventArgs e)
        {
            ClearDocument();
        }

        /// <summary>
        /// Builds the Flow Document for this view.
        /// The main table.
        /// </summary>
        public void BuildFlowDocument(LichessOpeningsStats openingStats)
        {
            Document.Blocks.Clear();
            Document.PageWidth = 590;

            Document.Blocks.Add(BuildHeaderLabel());
            Document.Blocks.Add(BuildTopGamesTableEx(openingStats));
        }

        /// <summary>
        /// Sets Rows and their content.
        /// Note that Rows are not recreated.
        /// </summary>
        /// <param name="stats"></param>
        /// <returns></returns>
        private Table BuildTopGamesTableEx(LichessOpeningsStats stats)
        {
            int rowNo = 0;
            _gameIdList.Clear();
            AdjustGamesTableRowCount(stats.TopGames.Length);
            foreach (LichessTopGame game in stats.TopGames)
            {
                if (rowNo >= MAX_GAME_ROW_COUNT)
                {
                    break;
                }

                TableRow row = _lstRows[rowNo].Row;
                _lstRows[rowNo].SetLabels(game);

                if (!string.IsNullOrWhiteSpace(game.Id))
                {
                    _gameIdList.Add(game.Id);
                    row.Name = _rowNamePrefix + game.Id;
                    row.PreviewMouseDown += Row_PreviewMouseDown;
                    row.Cursor = Cursors.Arrow;
                }
                rowNo++;
            }

            return _gamesTable;
        }


        /// <summary>
        /// Builds an empty document.
        /// </summary>
        public void ClearDocument()
        {
            Document.Blocks.Clear();
        }

        /// <summary>
        /// Adjusts the number of TableRows in the table.
        /// We don't construct or reconstruct the objects here but rather
        /// remove or add them to the Table.
        /// They are always kept the _lstRows list.
        /// </summary>
        /// <param name="gameCount"></param>
        private void AdjustGamesTableRowCount(int gameCount)
        {
            gameCount = Math.Min(MAX_GAME_ROW_COUNT, gameCount);
            int currentRowCount = _gamesTable.RowGroups[0].Rows.Count;
            if (currentRowCount < gameCount)
            {
                for (int i = currentRowCount; i < gameCount; i++)
                {
                    _gamesTable.RowGroups[0].Rows.Add(_lstRows[i].Row);
                }
            }
            else
            {
                for (int i = currentRowCount - 1; i >= gameCount; i--)
                {
                    _gamesTable.RowGroups[0].Rows.Remove(_lstRows[i].Row);
                }
            }
        }

        /// <summary>
        /// Sets alternating background for the rows.
        /// Highlights the row with the selected game.
        /// </summary>
        /// <param name="highlightedGameId"></param>
        public void SetRowBackgorunds(string highlightedGameId)
        {
            for (int i = 0; i < _gamesTable.RowGroups[0].Rows.Count; i++)
            {
                TableRow row = _gamesTable.RowGroups[0].Rows[i];
                try
                {
                    string gameId = row.Name.Substring(_rowNamePrefix.Length);
                    if (gameId == highlightedGameId)
                    {
                        row.Background = ChessForgeColors.TABLE_HIGHLIGHT_GREEN;
                        continue;
                    }
                }
                catch
                {
                }

                if (i % 2 == 0)
                {
                    row.Background = Brushes.White;
                }
                else
                {
                    row.Background = ChessForgeColors.TABLE_ROW_LIGHT_GRAY;
                }
            }
        }

        /// <summary>
        /// Opens the Game Review dialog.
        /// </summary>
        public void OpenReplayDialog()
        {
            // pass ActiveTab so that we can add a reference if this is a Study Tree
            LichessGamesPreviewDialog dlg = new LichessGamesPreviewDialog(_clickedGameId, _gameIdList, AppState.ActiveTab)
            {
                Left = AppState.MainWin.ChessForgeMain.Left + 100,
                Top = AppState.MainWin.Top + 100,
                Topmost = false,
                Owner = AppState.MainWin
            };
            dlg.ShowDialog();

            switch (dlg.ActiveTabOnExit)
            {
                case WorkbookManager.TabViewType.STUDY:
                    AppState.MainWin.UiTabStudyTree.Focus();
                    break;
                case WorkbookManager.TabViewType.MODEL_GAME:
                    AppState.MainWin.UiTabModelGames.Focus();
                    break;
                case WorkbookManager.TabViewType.EXERCISE:
                    AppState.MainWin.UiTabExercises.Focus();
                    break;
            }
        }

        /// <summary>
        /// Handler for the clicked game event
        /// </summary>
        public event EventHandler<WebAccessEventArgs> TopGameClicked;

        /// <summary>
        /// Handler of the mouse click on the Row event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Row_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is TableRow)
            {
                string id = ((TableRow)sender).Name;
                id = id.Substring(_rowNamePrefix.Length);
                _clickedGameId = id;

                // we act depending on whether this view is hosted in the main window or the Game Preview dialog
                if (_isMainWin)
                {
                    if (e.ChangedButton == MouseButton.Left)
                    {
                        OpenReplayDialog();
                    }
                    else if (e.ChangedButton == MouseButton.Right)
                    {
                        if (!string.IsNullOrWhiteSpace(id))
                        {
                            AppState.MainWin.UiCmTopGames.IsOpen = true;
                            e.Handled = true;
                        }
                    }
                }
                else
                {
                    WebAccessEventArgs eventArgs = new WebAccessEventArgs();
                    eventArgs.GameId = _clickedGameId;
                    TopGameClicked?.Invoke(null, eventArgs);
                }
            }
            e.Handled = true;
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
                Content = "  " + Properties.Resources.TopGames,

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

#if false
        /// <summary>
        /// Builds the main Top Games table.
        /// </summary>
        /// <returns></returns>
        private Table BuildTopGamesTable(LichessOpeningsStats openingStats)
        {
            _gamesTable = new Table();
            _gamesTable.FontSize = _baseFontSize + Configuration.FontSizeDiff;
            _gamesTable.CellSpacing = 0;
            _gamesTable.Margin = new Thickness(0);
            _gamesTable.RowGroups.Add(new TableRowGroup());

            CreateColumns(_gamesTable);
            LichessOpeningsStats stats = openingStats;
            int rowNo = 0;
            _gameIdList.Clear();
            foreach (LichessTopGame game in stats.TopGames)
            {
                TableRow row = BuildGameRow(_gamesTable, game, rowNo);
                _gamesTable.RowGroups[0].Rows.Add(row);
                if (!string.IsNullOrWhiteSpace(game.Id))
                {
                    _gameIdList.Add(game.Id);
                    row.Name = _rowNamePrefix + game.Id;
                    row.PreviewMouseDown += Row_PreviewMouseDown;
                    row.Cursor = Cursors.Arrow;
                }
                rowNo++;
            }
            
            return _gamesTable;
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

            Style style = AppState.MainWin.FindResource("RoundedBorder") as Style;
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
                    lblResult.Content = Constants.CharHalfPoint.ToString() + "-" + Constants.CharHalfPoint.ToString();
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
#endif

}
