using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ChessForge
{
    /// <summary>
    /// Manages HintBox, Engine Lines and Evaluation Chart
    /// that are shown in the same place under the main chessboard.
    /// </summary>
    public class MultiTextBoxManager
    {
        // full size of Eval Chart
        private static double EVAL_CHART_FULL_HEIGHT = 150;

        // shortcut reference to the Main Window
        private static MainWindow MainWin = AppState.MainWin;

        // whether the MultiBox is now in full or half size mode
        private static bool IsFullSize = true;

        /// <summary>
        /// Show or hide the evaluation chart depending on 
        /// the current state and config.
        /// </summary>
        public static void ShowEvaluationChart()
        {
            MainWin.Dispatcher.Invoke(() =>
            {
                if (AppState.CurrentLearningMode == LearningMode.Mode.IDLE)
                {
                    MainWin.UiEvalChart.Visibility = Visibility.Hidden;
                    MainWin.UiEvalChart.IsDirty = true;

                    MainWin.UiTbEngineLines.Visibility = Visibility.Hidden;
                }
                else
                {
                    if (IsChartTurnedOn() && MainWin.UiEvalChart.CanShowChart(false, out bool fullSize))
                    {
                        ResizeMultiBoxes(fullSize);
                        MainWin.UiEvalChart.Visibility = Visibility.Visible;
                        MainWin.UiEvalChart.Update();
                    }
                    else
                    {
                        if (MainWin.UiTbEngineLines.Visibility == Visibility.Visible && IsEngineLinesTurnedOn())
                        {
                            ResizeEngineLinesBox(true);
                        }
                        MainWin.UiEvalChart.Visibility = Visibility.Hidden;
                    }
                }
            });
        }

        /// <summary>
        /// Checks the state of the Chart toggle.
        /// </summary>
        /// <returns></returns>
        public static bool IsChartTurnedOn()
        {
            return MainWin.UiImgChartOn.Visibility == Visibility.Visible;
        }

        /// <summary>
        /// Checks the state of the Engine toggle.
        /// </summary>
        /// <returns></returns>
        public static bool IsEngineLinesTurnedOn()
        {
            return MainWin.UiImgEngineOn.Visibility == Visibility.Visible;
        }

        /// <summary>
        /// Resizes the Evaluation Chart and the Engine Lines box
        /// to full or half size.
        /// </summary>
        /// <param name="fullSize">full size if thrue, half size otherwise</param>
        private static void ResizeMultiBoxes(bool fullSize)
        {
            ResizeEvaluationChart(fullSize);
            ResizeEngineLinesBox(fullSize);
        }

        /// <summary>
        /// Resizes the height of the Evaluation Chart and sets
        /// the affected attributes accordingly.
        /// </summary>
        /// <param name="fullSize"></param>
        private static void ResizeEvaluationChart(bool fullSize)
        {
            if (fullSize != IsFullSize)
            {
                if (fullSize)
                {
                    MainWin.UiEvalChart.Height = EVAL_CHART_FULL_HEIGHT;
                    MainWin.UiEvalChart.CanvasHeight = (EVAL_CHART_FULL_HEIGHT - 2) / 2;
                    MainWin.UiEvalChart.MarkerSize = 8;
                    MainWin.UiEvalChart.BaseFontSize = 12;
                }
                else
                {
                    MainWin.UiEvalChart.Height = (EVAL_CHART_FULL_HEIGHT / 2);
                    MainWin.UiEvalChart.CanvasHeight = (EVAL_CHART_FULL_HEIGHT - 2) / 4;
                    MainWin.UiEvalChart.MarkerSize = 5;
                    MainWin.UiEvalChart.BaseFontSize = 10;
                }

                IsFullSize = fullSize;

                MainWin.UiEvalChart.IsFullSize = fullSize;
                MainWin.UiEvalChart.InitSizes();
            }
        }

        /// <summary>
        /// Resizes the height of the Engine Lines box and sets
        /// the affected attributes accordingly.
        /// </summary>
        /// <param name="fullSize"></param>
        public static void ResizeEngineLinesBox(bool fullSize)
        {
            if (fullSize)
            {
                MainWin.UiTbEngineLines.Height = EVAL_CHART_FULL_HEIGHT;
                MainWin.UiTbEngineLines.Margin = new Thickness(0, 10, 0, 0);
                MainWin.UiTbEngineLines.FontSize = 12;
            }
            else
            {
                MainWin.UiTbEngineLines.Height = EVAL_CHART_FULL_HEIGHT / 2;
                MainWin.UiTbEngineLines.Margin = new Thickness(0, 10 + (EVAL_CHART_FULL_HEIGHT / 2), 0, 0);
                MainWin.UiTbEngineLines.FontSize = 10;
            }
        }
    }
}
