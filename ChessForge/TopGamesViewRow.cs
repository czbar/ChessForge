using ChessPosition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows;
using WebAccess;

namespace ChessForge
{
    /// <summary>
    /// Encapsulates a single row in the TopGames table.
    /// Builds the UI elements so that they do not have to be rebuilt
    /// and exposes methods to set Labels' Content dynamically.
    /// </summary>
    public class TopGamesViewRow
    {
        // columns widths
        private int _namesColumnWidth = 150;
        private int _resultColumnWidth = 35;
        private int _dateColumnWidth = 40;

        // base font size for the control
        private int _baseFontSize = 11;

        private Paragraph _paraRatings;
        private Label _lblWhiteRating;
        private Label _lblBlackRating;

        private Paragraph _paraPlayerNames;
        private Label _lblWhitePlayer;
        private Label _lblBlackPlayer;

        private Paragraph _paraResult;
        private Label _lblResult;

        private Paragraph _paraDate;
        private Label _lblDate;

        // the TableRow to use in the calling Table
        private TableRow _row;

        // whether this is in the context of main window rather than a dialog
        private bool _isMainWin;

        /// <summary>
        /// The TableRow to use in the calling Table.
        /// </summary>
        public TableRow Row
        {
            get { return _row; }
        }

        /// <summary>
        /// Constructs the TopGamesViewRow object. 
        /// </summary>
        public TopGamesViewRow(bool isMainWin)
        {
            _isMainWin = isMainWin;
            BuildTableGameRow();
        }

        /// <summary>
        /// Sets contents of the labels with the data.
        /// </summary>
        /// <param name="game"></param>
        public void SetLabels(LichessTopGame game)
        {
            SetRatingLabels(game);
            SetPlayerNameLabels(game);
            SetResultLabel(game);
            SetDateLabel(game);
        }

        /// <summary>
        /// Sets the Result labels per the content of the game argument.
        /// </summary>
        /// <param name="game"></param>
        private void SetResultLabel(LichessTopGame game)
        {
            switch (game.Winner)
            {
                case "white":
                    _lblResult.Content = "1-0";
                    _lblResult.Background = ChessForgeColors.WhiteWinLinearBrush;
                    _lblResult.Foreground = Brushes.Black;
                    break;
                case "black":
                    _lblResult.Content = "0-1";
                    _lblResult.Background = ChessForgeColors.BlackWinLinearBrush;
                    _lblResult.Foreground = Brushes.White;
                    break;
                default:
                    _lblResult.Content = Constants.CharHalfPoint.ToString() + "-" + Constants.CharHalfPoint.ToString();
                    _lblResult.Background = ChessForgeColors.DrawLinearBrush;
                    _lblResult.Foreground = Brushes.White;
                    break;
            }
        }

        /// <summary>
        /// Sets the Ratings labels per the content of the game argument.
        /// </summary>
        /// <param name="game"></param>
        private void SetRatingLabels(LichessTopGame game)
        {
            _lblWhiteRating.Content = game.White.Rating ?? "";
            _lblBlackRating.Content = game.Black.Rating ?? "";
        }

        /// <summary>
        /// Sets the Player Name labels per the content of the game argument.
        /// </summary>
        /// <param name="game"></param>
        private void SetPlayerNameLabels(LichessTopGame game)
        {
            _lblWhitePlayer.Content = game.White.Name;
            _lblWhitePlayer.ToolTip = game.White.Name;

            _lblBlackPlayer.Content = game.Black.Name;
            _lblBlackPlayer.ToolTip = game.Black.Name;
        }

        /// <summary>
        /// Sets the Date label per the content of the game argument.
        /// </summary>
        /// <param name="game"></param>
        private void SetDateLabel(LichessTopGame game)
        {
            _lblDate.Content = game.Year ?? "";
        }

        /// <summary>
        /// Builds the TableRow object.
        /// </summary>
        /// <param name="game"></param>
        private TableRow BuildTableGameRow()
        {
            _row = new TableRow();

            if (_isMainWin)
            {
                _row.Foreground = ChessForgeColors.CurrentTheme.RtbForeground;
            }

            TableCell cellRatings = new TableCell(BuildRatingsPara());
            _row.Cells.Add(cellRatings);

            TableCell cellNames = new TableCell(BuildNamesPara());
            _row.Cells.Add(cellNames);

            TableCell cellResult = new TableCell(BuildResultPara());
            _row.Cells.Add(cellResult);

            TableCell cellDate = new TableCell(BuildDatePara());
            _row.Cells.Add(cellDate);

            return _row;
        }

        /// <summary>
        /// Builds a Paragraph with the Players' ratings.
        /// </summary>
        /// <param name="gamesTable"></param>
        /// <param name="game"></param>
        /// <returns></returns>
        private Paragraph BuildRatingsPara()
        {
            _paraRatings = new Paragraph();

            Canvas canvas = new Canvas
            {
                Width = _namesColumnWidth,
                Height = 44 + Configuration.FontSizeDiff,
            };

            _lblWhiteRating = new Label
            {                
                Width = _namesColumnWidth,
                Height = 20 + Configuration.FontSizeDiff,
                FontSize = _baseFontSize + Configuration.FontSizeDiff,
                VerticalContentAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Content = "",

                BorderThickness = new Thickness(0, 0, 0, 0),
                Padding = new Thickness(0, 0, 0, 0)
            };
            if (_isMainWin)
            {
                _lblWhiteRating.Foreground = ChessForgeColors.CurrentTheme.RtbForeground;
            }
            canvas.Children.Add(_lblWhiteRating);
            Canvas.SetLeft(_lblWhiteRating, 0);
            Canvas.SetTop(_lblWhiteRating, 2);

            _lblBlackRating = new Label
            {
                Width = _namesColumnWidth,
                Height = 20 + Configuration.FontSizeDiff,
                FontSize = _baseFontSize + Configuration.FontSizeDiff,
                VerticalContentAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Content = "",

                BorderThickness = new Thickness(0, 0, 0, 0),
                Padding = new Thickness(0, 0, 0, 0)
            };
            if (_isMainWin)
            {
                _lblBlackRating.Foreground = ChessForgeColors.CurrentTheme.RtbForeground;
            }
            canvas.Children.Add(_lblBlackRating);
            Canvas.SetLeft(_lblBlackRating, 0);
            Canvas.SetTop(_lblBlackRating, 20);

            InlineUIContainer uIContainer = new InlineUIContainer
            {
                Child = canvas
            };
            _paraRatings.Inlines.Add(uIContainer);

            return _paraRatings;
        }

        /// <summary>
        /// Builds a Paragraph with Players' names
        /// </summary>
        /// <param name="gamesTable"></param>
        /// <param name="game"></param>
        /// <returns></returns>
        private Paragraph BuildNamesPara()
        {
            _paraPlayerNames = new Paragraph();

            Canvas canvas = new Canvas
            {
                Width = _namesColumnWidth,
                Height = 44 + Configuration.FontSizeDiff,
            };

            _lblWhitePlayer = new Label
            {
                Width = _namesColumnWidth - 8,
                Height = 20 + Configuration.FontSizeDiff,
                FontSize = _baseFontSize + Configuration.FontSizeDiff,
                VerticalContentAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Content = "",
                ToolTip = "",

                BorderThickness = new Thickness(0, 0, 0, 0),
                Padding = new Thickness(0, 0, 0, 0)
            };
            if (_isMainWin)
            {
                _lblWhitePlayer.Foreground = ChessForgeColors.CurrentTheme.RtbForeground;
            }
            canvas.Children.Add(_lblWhitePlayer);
            Canvas.SetLeft(_lblWhitePlayer, 4);
            Canvas.SetTop(_lblWhitePlayer, 2);

            _lblBlackPlayer = new Label
            {
                Width = _namesColumnWidth - 8,
                Height = 20 + Configuration.FontSizeDiff,
                FontSize = _baseFontSize + Configuration.FontSizeDiff,
                VerticalContentAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Content = "",
                ToolTip = "",

                BorderThickness = new Thickness(0, 0, 0, 0),
                Padding = new Thickness(0, 0, 0, 0)
            };
            if (_isMainWin)
            {
                _lblBlackPlayer.Foreground = ChessForgeColors.CurrentTheme.RtbForeground;
            }
            canvas.Children.Add(_lblBlackPlayer);
            Canvas.SetLeft(_lblBlackPlayer, 4);
            Canvas.SetTop(_lblBlackPlayer, 20);

            InlineUIContainer uIContainer = new InlineUIContainer
            {
                Child = canvas
            };
            _paraPlayerNames.Inlines.Add(uIContainer);

            return _paraPlayerNames;
        }

        /// <summary>
        /// Builds a Paragraph with a string representing the result.
        /// </summary>
        /// <param name="gamesTable"></param>
        /// <param name="game"></param>
        /// <returns></returns>
        private Paragraph BuildResultPara()
        {
            _paraResult = new Paragraph();

            Canvas canvas = new Canvas
            {
                Width = _resultColumnWidth,
                Height = 44 + Configuration.FontSizeDiff,
            };

            _lblResult = new Label
            {
                Width = _resultColumnWidth,
                Height = 20 + Configuration.FontSizeDiff,
                FontSize = _baseFontSize - 1 + Configuration.FontSizeDiff,
                VerticalContentAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center,

                BorderThickness = new Thickness(0, 0, 0, 0),
                Padding = new Thickness(0, 0, 0, 0)
            };
            if (_isMainWin)
            {
                _lblResult.Foreground = ChessForgeColors.CurrentTheme.RtbForeground;
            }

            Style style = AppState.MainWin.FindResource("RoundedBorder") as Style;
            _lblResult.Style = style;

            canvas.Children.Add(_lblResult);
            Canvas.SetLeft(_lblResult, 0);
            Canvas.SetTop(_lblResult, (canvas.Height - 20) / 2);

            InlineUIContainer uIContainer = new InlineUIContainer
            {
                Child = canvas
            };
            _paraResult.Inlines.Add(uIContainer);

            return _paraResult;
        }

        /// <summary>
        /// Builds a Paragraph with a string representing the date.
        /// </summary>
        /// <param name="gamesTable"></param>
        /// <param name="game"></param>
        /// <returns></returns>
        private Paragraph BuildDatePara()
        {
            _paraDate = new Paragraph();

            Canvas canvas = new Canvas
            {
                Width = _resultColumnWidth,
                Height = 44 + Configuration.FontSizeDiff,
            };

            _lblDate = new Label
            {
                Width = _dateColumnWidth - 6,
                Height = 20 + Configuration.FontSizeDiff,
                FontSize = _baseFontSize - 1 + Configuration.FontSizeDiff,
                VerticalContentAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Content = "",

                BorderThickness = new Thickness(0, 0, 0, 0),
                Padding = new Thickness(0, 0, 0, 0)
            };
            if (_isMainWin)
            {
                _lblDate.Foreground = ChessForgeColors.CurrentTheme.RtbForeground;
            }

            canvas.Children.Add(_lblDate);
            Canvas.SetLeft(_lblDate, 0);
            Canvas.SetTop(_lblDate, (canvas.Height - 20) / 2);

            InlineUIContainer uIContainer = new InlineUIContainer
            {
                Child = canvas
            };
            _paraDate.Inlines.Add(uIContainer);

            return _paraDate;
        }

    }
}
