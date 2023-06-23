using System;
using System.Collections.Generic;
using System.Windows;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Net;
using GameTree;
using System.Diagnostics.Eventing.Reader;
using System.Xml;

namespace ChessForge
{
    /// <summary>
    /// Controls the Evaluation Bar lables at the border of the main chess board
    /// </summary>
    public class EvaluationBar
    {
        // total height available to show the bar
        private const double BAR_HEIGHT = 640;

        // max allowed height of the white part of the bar
        private const double MAX_WHITE_BAR_HEIGHT = 640;

        // min allowed height of the white part of the bar
        private const double MIN_WHITE_BAR_HEIGHT = 0;

        // max allowed height of the white part of the bar when showing CP eval (as opposed to checkmate)
        private const double MAX_CPEVAL_WHITE_BAR_HEIGHT = 615;

        // min allowed height of the white part of the bar when showing CP eval (as opposed to checkmate)
        private const double MIN_CPEVAL_WHITE_BAR_HEIGHT = 25;

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
        /// <param name="nd"></param>
        public static void ShowEvaluation(TreeNode nd)
        {
            AppState.MainWin.Dispatcher.Invoke(() =>
            {
                bool advantageWhite = true;
                bool show = true;

                if (nd != null && 
                      (!string.IsNullOrEmpty(nd.EngineEvaluation) || nd.Position.IsCheckmate || nd.Position.IsStalemate || EvaluationManager.IsRunning)
                   )
                {
                    bool res = double.TryParse(nd.EngineEvaluation, out double dVal);
                    if (res)
                    {
                        // parsed successfully so format the result
                        ShowCentipawns(dVal);
                    }
                    else
                    {
                        // check if this is checkmate or stalemate evaluation
                        if (nd.Position.IsCheckmate)
                        {
                            advantageWhite = nd.ColorToMove == ChessPosition.PieceColor.Black;
                            ShowCheckmate(advantageWhite);
                        }
                        else if (nd.Position.IsStalemate)
                        {
                            advantageWhite = true;
                            ShowCentipawns(0);
                        }
                        else
                        {
                            // check if eval indicates upcoming mate
                            if (!string.IsNullOrEmpty(nd.EngineEvaluation) && (nd.EngineEvaluation.StartsWith("+#") || (nd.EngineEvaluation.StartsWith("-#"))))
                            {
                                try
                                {
                                    if (int.TryParse(nd.EngineEvaluation.Substring(2), out int movesToMate))
                                    {
                                        ShowMovesToMate(movesToMate, nd.EngineEvaluation[0] == '+');
                                    }
                                    else
                                    {
                                        show = false;
                                    }
                                }
                                catch
                                {
                                    show = false;
                                }
                            }
                            else
                            {
                                show = EvaluationManager.IsRunning;
                            }
                        }
                    }
                }
                else
                {
                    show = false;
                }

                Show(show);
            });
        }

        /// <summary>
        /// Adjust bars' lengths and labels
        /// </summary>
        /// <param name="whiteBarLength"></param>
        /// <param name="labelText"></param>
        /// <param name="advantageWhite"></param>
        private static void SetBars(double whiteBarLength, string labelText, bool advantageWhite)
        {
            bool isFlipped = AppState.MainWin.MainChessBoard.IsFlipped;

            if (advantageWhite)
            {
                AppState.MainWin.UiLblEvalBarWhite.Content = labelText;
                AppState.MainWin.UiLblEvalBarHost.Content = "";
                AppState.MainWin.UiLblEvalBarWhite.VerticalContentAlignment = isFlipped ? VerticalAlignment.Top : VerticalAlignment.Bottom;
            }
            else
            {
                AppState.MainWin.UiLblEvalBarWhite.Content = "";
                AppState.MainWin.UiLblEvalBarHost.Content = labelText;
                AppState.MainWin.UiLblEvalBarHost.VerticalContentAlignment = isFlipped ? VerticalAlignment.Bottom : VerticalAlignment.Top;
            }

            AppState.MainWin.UiLblEvalBarWhite.Height = whiteBarLength;
            if (isFlipped)
            {
                Canvas.SetTop(AppState.MainWin.UiLblEvalBarWhite, TOP);
            }
            else
            {
                Canvas.SetTop(AppState.MainWin.UiLblEvalBarWhite, BOTTOM - whiteBarLength);
            }
        }

        /// <summary>
        /// Reflect the passed evaluation value in the evaluation bar.
        /// </summary>
        /// <param name="dVal"></param>
        private static void ShowCentipawns(double dVal)
        {
            double centiPawns = dVal * 100;

            double whiteBarLength;

            if (centiPawns >= 0)
            {
                whiteBarLength = (centiPawns / CENTI_PAWNS_PER_SQUARE) * SQUARE_HEIGHT + (BAR_HEIGHT / 2);
                if (whiteBarLength > MAX_CPEVAL_WHITE_BAR_HEIGHT)
                {
                    whiteBarLength = MAX_CPEVAL_WHITE_BAR_HEIGHT;
                }
            }
            else
            {
                whiteBarLength = (BAR_HEIGHT / 2) - (-centiPawns / CENTI_PAWNS_PER_SQUARE) * SQUARE_HEIGHT;
                if (whiteBarLength < MIN_CPEVAL_WHITE_BAR_HEIGHT)
                {
                    whiteBarLength = MIN_CPEVAL_WHITE_BAR_HEIGHT;
                }
            }

            double absDval = Math.Abs(dVal);
            if (absDval >= 10)
            {
                SetBars(whiteBarLength, Math.Abs(dVal).ToString("F0"), dVal >= 0);
            }
            else
            {
                SetBars(whiteBarLength, Math.Abs(dVal).ToString("F1"), dVal >= 0);
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
        }

        /// <summary>
        /// Reflect a checkmate position in the evaluation bar
        /// </summary>
        /// <param name="advantageWhite"></param>
        private static void ShowCheckmate(bool advantageWhite)
        {
            double whiteBarLength;

            if (advantageWhite)
            {
                whiteBarLength = MAX_WHITE_BAR_HEIGHT;
            }
            else
            {
                whiteBarLength = MIN_WHITE_BAR_HEIGHT;
            }

            SetBars(whiteBarLength, "M0", advantageWhite);
        }

        /// <summary>
        /// Reflect the forced checkmate in the evaluation bar
        /// </summary>
        /// <param name="movesToMate"></param>
        /// <param name="advantageWhite"></param>
        private static void ShowMovesToMate(int movesToMate, bool advantageWhite)
        {
            double whiteBarLength;

            if (advantageWhite)
            {
                whiteBarLength = MAX_WHITE_BAR_HEIGHT;
            }
            else
            {
                whiteBarLength = MIN_WHITE_BAR_HEIGHT;
            }

            // no room for 2 digits
            string labelText = movesToMate > 9 ? "M*" : ("M" + movesToMate.ToString());
            SetBars(whiteBarLength, labelText, advantageWhite);
        }

    }
}
