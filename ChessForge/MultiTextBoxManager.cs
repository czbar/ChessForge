using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

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
        public static void ShowEvaluationChart(bool forceRefresh, ObservableCollection<TreeNode> nodeList = null)
        {
            MainWin.Dispatcher.Invoke(() =>
            {
                if (forceRefresh)
                {
                    MainWin.UiEvalChart.IsDirty = true;
                }

                if (nodeList == null)
                {
                    nodeList = AppState.MainWin.ActiveLine.GetNodeList();
                }

                if (AppState.CurrentLearningMode == LearningMode.Mode.IDLE)
                {
                    MainWin.UiEvalChart.Visibility = Visibility.Hidden;
                    MainWin.UiEvalChart.IsDirty = true;

                    MainWin.UiTbEngineLines.Visibility = Visibility.Hidden;
                }
                else
                {
                    if (IsChartTurnedOn() && CanShowEvaluationChart(false, out bool fullSize))
                    {
                        ResizeMultiBoxes(fullSize);
                        MainWin.UiEvalChart.Visibility = Visibility.Visible;
                        MainWin.UiEvalChart.Refresh(nodeList);
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
        /// Checks if the evaluation chart can be currently shown.
        /// If it can't and showReason == true, a flash announcement
        /// with the reason will be displayed.
        /// </summary>
        /// <returns></returns>
        public static bool CanShowEvaluationChart(bool showReason, out bool fullSize)
        {
            bool res = true;
            fullSize = true;

            if (Configuration.ShowEvaluationChart)
            {
                if (!IsValidTabForEvalChart())
                {
                    // report wrong tab
                    if (showReason)
                    {
                        AppState.MainWin.BoardCommentBox.ShowFlashAnnouncement(Properties.Resources.ChartErrorWrongTab, CommentBox.HintType.ERROR);
                    }
                    res = false;
                }

                if (res && EvaluationManager.IsRunning)
                {
                    fullSize = false;
                }

            }
            else
            {
                res = false;
            }

            return res;
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
            return MainWin.UiImgEngineOn.Visibility == Visibility.Visible || MainWin.UiImgEngineOnGray.Visibility == Visibility.Visible;
        }

        /// <summary>
        /// Checks if the current tab can show the Evaluation Chart.
        /// It can if this is a study, a game or an exercise in the view mode with the soluttion shown.
        /// </summary>
        /// <returns></returns>
        private static bool IsValidTabForEvalChart()
        {
            return AppState.ActiveTab == TabViewType.STUDY
                || AppState.ActiveTab == TabViewType.MODEL_GAME
                || (AppState.ActiveTab == TabViewType.EXERCISE && !AppState.IsUserSolving() && AppState.ActiveVariationTree != null && AppState.ActiveVariationTree.ShowTreeLines);
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
        private static void ResizeEngineLinesBox(bool fullSize)
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
