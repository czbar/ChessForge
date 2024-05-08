using ChessPosition;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using WebAccess;

namespace ChessForge
{
    /// <summary>
    /// Encapsulates a single row in the OpeningStats view.
    /// </summary>
    public class OpeningStatsViewRow
    {
        // scale factor for table cell sizes
        private double scaleFactor = 3.5;

        // base font size for the control
        private int _baseFontSize = 11;

        // The move number string to prefix the moves in the table with
        private string _moveNumberString;

        // prefix for the Run with the move
        private readonly string MOVE_PREFIX = "_move_";

        // column widths in the stats table
        private readonly double _totalGamesColumnWidth = 20;

        // label for the total number of games
        private Label _lblTotalGames;

        private Border _pctBarBorder;
        private Label _lblPctWhite;
        private Label _lblPctDraws;
        private Label _lblPctBlack;

        private TableCell _cellMove;
        private TableCell _cellTotal;
        private TableCell _cellScoring;

        // a Run representing this move
        private Run _runMove;

        // View that is using objects of the class
        private OpeningStatsView _parentView;

        // the TableRow to use in the calling Table
        private TableRow _row;

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
        public OpeningStatsViewRow(OpeningStatsView parentView)
        {
            _parentView = parentView;
            BuildTableMoveRow();
        }

        /// <summary>
        /// This method is used by OpeningStatsView to update the Row.
        /// </summary>
        /// <param name="move"></param>
        /// <param name="moveNumberString"></param>
        public void SetLabels(LichessMoveStats move, string moveNumberString, PieceColor color)
        {
            _moveNumberString = moveNumberString;
            PopulateCellsInRow(move, color);
        }

        /// <summary>
        /// Builds this re-usable Row.
        /// </summary>
        private void BuildTableMoveRow()
        {
            _row = new TableRow();

            _row.Foreground = ChessForgeColors.CurrentTheme.RtbForeground;
            _row.Background = ChessForgeColors.CurrentTheme.RtbBackground;

            _runMove = new Run("");
            _runMove.Name = "";
            _runMove.Cursor = Cursors.Arrow;

            _cellMove = new TableCell(new Paragraph(_runMove));
            _row.Cells.Add(_cellMove);

            _cellTotal = new TableCell(BuildTotalGamesPara());
            _row.Cells.Add(_cellTotal);

            _cellScoring = new TableCell(CreatePercentBarToParagraph());
            _row.Cells.Add(_cellScoring);
        }

        /// <summary>
        /// Builds a Label representing result percentage. 
        /// </summary>
        /// <returns></returns>
        private Label BuildPercentLabel()
        {
            Label lbl = new Label
            {
                Width = 50,
                Height = 14 + Configuration.FontSizeDiff,
                FontSize = _baseFontSize + Configuration.FontSizeDiff,
                VerticalContentAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Content = "",

                BorderThickness = new Thickness(0, 0, 0, 0),
                Padding = new Thickness(0, 0, 0, 0)
            };

            return lbl;
        }

        /// <summary>
        /// Sets the width and content of the percentage label
        /// </summary>
        /// <param name="lbl"></param>
        /// <param name="pct"></param>
        private void SetPercentLabel(Label lbl, int pct)
        {
            lbl.Width = pct * scaleFactor;
            lbl.Content = pct.ToString() + "%";
        }

        /// <summary>
        /// Builds Paragraph showing the total number of games.
        /// </summary>
        /// <returns></returns>
        private Paragraph BuildTotalGamesPara()
        {
            Paragraph para = new Paragraph();

            Canvas canvas = new Canvas
            {
                Width = scaleFactor * (_totalGamesColumnWidth),
                Height = 20 + Configuration.FontSizeDiff,
                Background = ChessForgeColors.CurrentTheme.RtbBackground,
            };

            _lblTotalGames = new Label
            {
                Width = scaleFactor * _totalGamesColumnWidth,
                Height = 18 + Configuration.FontSizeDiff,
                FontSize = _baseFontSize + 1 + Configuration.FontSizeDiff,
                VerticalContentAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Right,
                Content = "",

                BorderThickness = new Thickness(0, 0, 0, 0),
                Padding = new Thickness(0, 0, 0, 0)
            };

            _lblTotalGames.Foreground = ChessForgeColors.CurrentTheme.RtbForeground;
            _lblTotalGames.Background = ChessForgeColors.CurrentTheme.RtbBackground;

            canvas.Children.Add(_lblTotalGames);

            Canvas.SetLeft(_lblTotalGames, 0 * scaleFactor);

            InlineUIContainer uIContainer = new InlineUIContainer
            {
                Child = canvas
            };
            para.Inlines.Add(uIContainer);


            return para;
        }

        /// <summary>
        /// Sets the text of the TotalGames label.
        /// </summary>
        /// <param name="totalGames"></param>
        private void SetTotalGames(int totalGames)
        {
            _lblTotalGames.Content = totalGames.ToString("N0");
        }

        /// <summary>
        /// Populates cells in the passed row, using LichessMoveStats data
        /// </summary>
        /// <param name="_row"></param>
        /// <param name="move"></param>
        /// <param name="scaleFactor"></param>
        private void PopulateCellsInRow(LichessMoveStats move, PieceColor color)
        {
            try
            {
                _runMove.Text = _moveNumberString + Languages.MapPieceSymbols(move.San, color);
                _runMove.MouseLeftButtonDown += _parentView.EventMoveClicked;
                _runMove.Name = MOVE_PREFIX + move.Uci;
                _runMove.Cursor = Cursors.Arrow;


                int whiteWins = int.Parse(move.White);
                int draws = int.Parse(move.Draws);
                int blackWins = int.Parse(move.Black);

                int totalGames = whiteWins + draws + blackWins;

                int whiteWinsPercent = (int)Math.Round((double)(whiteWins * 100) / (double)totalGames);
                int blackWinsPercent = (int)Math.Round((double)(blackWins * 100) / (double)totalGames);
                int drawsPercent = 100 - (whiteWinsPercent + blackWinsPercent);

                Canvas.SetLeft(_lblPctWhite, 10 * scaleFactor);
                Canvas.SetLeft(_lblPctDraws, Canvas.GetLeft(_lblPctWhite) + _lblPctWhite.Width);
                Canvas.SetLeft(_lblPctBlack, Canvas.GetLeft(_lblPctDraws) + _lblPctDraws.Width);

                _cellTotal.FontSize = _baseFontSize + 1 + Configuration.FontSizeDiff;

                SetTotalGames(totalGames);
                SetPercentBarElements(whiteWinsPercent, drawsPercent, blackWinsPercent);
            }
            catch (Exception ex)
            {
                AppLog.Message("PopulateCellsInRow", ex);
            }
        }

        /// <summary>
        /// Combines percentage labels into one "bar".
        /// </summary>
        /// <returns></returns>
        private Paragraph CreatePercentBarToParagraph()
        {
            Paragraph para = new Paragraph();

            Canvas canvas = new Canvas
            {
                Width = scaleFactor * 110 + 2,
                Height = 20 + Configuration.FontSizeDiff,
                Background = ChessForgeColors.CurrentTheme.RtbBackground,
            };

            _lblPctWhite = BuildPercentLabel();
            _lblPctDraws = BuildPercentLabel();
            _lblPctBlack = BuildPercentLabel();

            _lblPctWhite.Background = ChessForgeColors.WhiteWinLinearBrush;

            _lblPctDraws.Background = ChessForgeColors.DrawLinearBrush;
            _lblPctDraws.Foreground = Brushes.White;

            _lblPctBlack.Background = ChessForgeColors.BlackWinLinearBrush;
            _lblPctBlack.Foreground = Brushes.White;


            _pctBarBorder = new Border
            {
                BorderBrush = ChessForgeColors.TABLE_ROW_LIGHT_GRAY,
                Width = (100 * scaleFactor + 2),
                Height = 16 + Configuration.FontSizeDiff,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
            };

            canvas.Children.Add(_lblPctWhite);
            canvas.Children.Add(_lblPctDraws);
            canvas.Children.Add(_lblPctBlack);
            canvas.Children.Add(_pctBarBorder);

            Canvas.SetLeft(_pctBarBorder, 10 * scaleFactor - 1);
            Canvas.SetLeft(_lblPctWhite, 10 * scaleFactor);
            Canvas.SetLeft(_lblPctDraws, Canvas.GetLeft(_lblPctWhite) + _lblPctWhite.Width);
            Canvas.SetLeft(_lblPctBlack, Canvas.GetLeft(_lblPctDraws) + _lblPctDraws.Width);

            Canvas.SetTop(_pctBarBorder, 2);
            Canvas.SetTop(_lblPctWhite, 3);
            Canvas.SetTop(_lblPctDraws, 3);
            Canvas.SetTop(_lblPctBlack, 3);

            InlineUIContainer uIContainer = new InlineUIContainer
            {
                Child = canvas
            };
            para.Inlines.Add(uIContainer);


            return para;
        }

        /// <summary>
        /// Sets the width and text of the labels in the percentages bar.
        /// </summary>
        /// <param name="pctWhite"></param>
        /// <param name="pctDraws"></param>
        /// <param name="pctBlack"></param>
        private void SetPercentBarElements(int pctWhite, int pctDraws, int pctBlack)
        {
            SetPercentLabel(_lblPctWhite, pctWhite);
            SetPercentLabel(_lblPctDraws, pctDraws);
            SetPercentLabel(_lblPctBlack, pctBlack);

            Canvas.SetLeft(_lblPctWhite, 10 * scaleFactor);
            Canvas.SetLeft(_lblPctDraws, Canvas.GetLeft(_lblPctWhite) + _lblPctWhite.Width);
            Canvas.SetLeft(_lblPctBlack, Canvas.GetLeft(_lblPctDraws) + _lblPctDraws.Width);
        }

    }
}
