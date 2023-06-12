using System;
using System.Collections.Generic;
using System.Windows;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace ChessForge
{
    /// <summary>
    /// Controls the Evaluation Bar lables at the border of the main chess board
    /// </summary>
    public class EvaluationBar
    {
        // total height available to show the bar
        private const double BAR_HEIGHT = 640;

        // top position of the bar
        private const double TOP = 20;

        // bottom bosition of the bar
        private const double BOTTOM = TOP + BAR_HEIGHT;

        // how many centipawns are represented by a single square's height
        private const double CENTI_PAWNS_PER_SQUARE = 150;

        // chessboard's square height
        private const double SQUARE_HEIGHT = 80;

        /// <summary>
        /// Show or hide the bars.
        /// </summary>
        /// <param name="showHide"></param>
        public static void Show(bool showHide)
        {
            AppState.MainWin.Dispatcher.Invoke(() =>
            {
                AppState.MainWin.UiLblEvalBarHost.Visibility = showHide ? Visibility.Visible : Visibility.Collapsed;
                AppState.MainWin.UiLblEvalBarWhite.Visibility = showHide ? Visibility.Visible : Visibility.Collapsed;
            });
        }

        /// <summary>
        /// Sets the "white bar's" position and height representing the passed
        /// centipawn value.
        /// </summary>
        /// <param name="centiPawns"></param>
        public static void ShowEvaluation(double centiPawns)
        {
            AppState.MainWin.Dispatcher.Invoke(() =>
            {
                double whiteBarLength;

                if (centiPawns >= 0)
                {
                    whiteBarLength = (centiPawns / CENTI_PAWNS_PER_SQUARE) * SQUARE_HEIGHT + (BAR_HEIGHT / 2);
                }
                else
                {
                    whiteBarLength = (BAR_HEIGHT / 2) - (-centiPawns / CENTI_PAWNS_PER_SQUARE) * SQUARE_HEIGHT;
                }

                AppState.MainWin.UiLblEvalBarWhite.Height = whiteBarLength;
                if (AppState.MainWin.MainChessBoard.IsFlipped)
                {
                    Canvas.SetTop(AppState.MainWin.UiLblEvalBarWhite, TOP);
                }
                else
                {
                    Canvas.SetTop(AppState.MainWin.UiLblEvalBarWhite, BOTTOM - whiteBarLength);
                }
            });
        }
    }
}
