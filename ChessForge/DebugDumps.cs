using ChessPosition;
using EngineService;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;

namespace ChessForge
{
    public class DebugDumps
    {
        /// <summary>
        /// Logs details of the dragged piece
        /// </summary>
        [Conditional("DEBUG")]
        public static void LogDraggedPiece()
        {
            StringBuilder sb = new StringBuilder("Dragged Piece: ");
            {
                sb.Append("IsDragInProgress = " + DraggedPiece.isDragInProgress);
                sb.Append("  originX = " + DraggedPiece.OriginSquare.Xcoord.ToString());
                sb.Append("  originY = " + DraggedPiece.OriginSquare.Ycoord.ToString());
            }
            AppLog.Message(2, sb.ToString());
        }

        /// <summary>
        /// Writes out all logs.
        /// If userRequested == true, this was requested via the menu
        /// and we dump everything with distinct file names.
        /// Otherwise we only dump app and engine logs, ovewriting previous
        /// logs.
        /// </summary>
        /// <param name="userRequested"></param>
        [Conditional("DEBUG")]
        public static void DumpDebugLogs(bool userRequested)
        {
            string distinct = null;
            AppLog.LogAvailableThreadsCounts();

            if (userRequested)
            {
                distinct = "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                AppLog.DumpVariationTree(DebugUtils.BuildLogFileName(App.AppPath, "wktree", distinct), AppState.ActiveVariationTree);
                if (AppState.MainWin.StudyTreeView != null)
                {
                    AppLog.DumpLineSectorTree(DebugUtils.BuildLogFileName(App.AppPath, "lstree", distinct), AppState.MainWin.StudyTreeView.LineManager.LineSectors);
                }
                DumpStatesAndTimers(DebugUtils.BuildLogFileName(App.AppPath, "timest", distinct));
            }

            try
            {
                AppLog.Dump(DebugUtils.BuildLogFileName(App.AppPath, "applog", distinct));
                EngineLog.Dump(DebugUtils.BuildLogFileName(App.AppPath, "engine", distinct));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Dump logs exception: " + ex.Message, "DEBUG", MessageBoxButton.OK, MessageBoxImage.Stop);
            }
        }

        /// <summary>
        /// Creates the file name and writes out states and timers.
        /// </summary>
        [Conditional("DEBUG")]
        public static void DumpDebugStates()
        {
            string distinct = "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
            DumpStatesAndTimers(DebugUtils.BuildLogFileName(App.AppPath, "timest", distinct));
        }


        /// <summary>
        /// Writes out states and timers.
        /// </summary>
        /// <param name="filePath"></param>
        [Conditional("DEBUG")]
        private static void DumpStatesAndTimers(string filePath)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("Application States" + Environment.NewLine);
            sb.Append("==================" + Environment.NewLine);
            sb.Append("Learning mode = " + AppState.CurrentLearningMode.ToString() + Environment.NewLine);
            sb.Append("IsTrainingInProgress = " + TrainingSession.IsTrainingInProgress.ToString() + Environment.NewLine);
            sb.Append("IsTrainingContinuousEval = " + TrainingSession.IsContinuousEvaluation.ToString() + Environment.NewLine);
            sb.Append("TrainingMode = " + TrainingSession.CurrentState.ToString() + Environment.NewLine);
            sb.Append("EvaluationMode = " + AppState.CurrentEvaluationMode.ToString() + Environment.NewLine);
            sb.Append("GameState = " + EngineGame.CurrentState.ToString() + Environment.NewLine);

            sb.Append(Environment.NewLine);

            sb.Append("Timer States" + Environment.NewLine);
            sb.Append("============" + Environment.NewLine);

            AppTimers timers = AppState.MainWin.Timers;

            bool isMessagePollEnabled = EngineMessageProcessor.ChessEngineService.IsMessageRxLoopEnabled();
            sb.Append("ENGINE MESSAGE POLL" + ": IsEnabled = "
                + isMessagePollEnabled.ToString() + Environment.NewLine);

            sb.Append(AppTimers.TimerId.AUTO_SAVE.ToString() + ": IsEnabled = "
                + timers.IsEnabled(AppTimers.TimerId.AUTO_SAVE).ToString() + Environment.NewLine);
            sb.Append(Environment.NewLine);
            sb.Append(AppTimers.TimerId.EVALUATION_LINE_DISPLAY.ToString() + ": IsEnabled = "
                + timers.IsEnabled(AppTimers.TimerId.EVALUATION_LINE_DISPLAY).ToString() + Environment.NewLine);
            sb.Append(Environment.NewLine);
            sb.Append(AppTimers.TimerId.CHECK_FOR_USER_MOVE.ToString() + ": IsEnabled = "
                + timers.IsEnabled(AppTimers.TimerId.CHECK_FOR_USER_MOVE).ToString() + Environment.NewLine);
            sb.Append(AppTimers.TimerId.CHECK_FOR_TRAINING_WORKBOOK_MOVE_MADE.ToString() + ": IsEnabled = "
                + timers.IsEnabled(AppTimers.TimerId.CHECK_FOR_TRAINING_WORKBOOK_MOVE_MADE).ToString() + Environment.NewLine);
            sb.Append(AppTimers.TimerId.REQUEST_WORKBOOK_MOVE.ToString() + ": IsEnabled = "
                + timers.IsEnabled(AppTimers.TimerId.REQUEST_WORKBOOK_MOVE).ToString() + Environment.NewLine);
            sb.Append(Environment.NewLine);

            sb.Append(AppTimers.TimerId.SHOW_TRAINING_PROGRESS_POPUP_MENU.ToString() + ": IsEnabled = "
                + timers.IsEnabled(AppTimers.TimerId.SHOW_TRAINING_PROGRESS_POPUP_MENU).ToString() + Environment.NewLine);
            sb.Append(AppTimers.TimerId.FLASH_ANNOUNCEMENT.ToString() + ": IsEnabled = "
                + timers.IsEnabled(AppTimers.TimerId.FLASH_ANNOUNCEMENT).ToString() + Environment.NewLine);
            sb.Append(AppTimers.TimerId.APP_START.ToString() + ": IsEnabled = "
                + timers.IsEnabled(AppTimers.TimerId.APP_START).ToString() + Environment.NewLine);

            sb.Append(AppTimers.StopwatchId.EVALUATION_ELAPSED_TIME.ToString() + ": IsRunning = "
                + timers.IsRunning(AppTimers.StopwatchId.EVALUATION_ELAPSED_TIME).ToString() + Environment.NewLine);

            try
            {
                File.WriteAllText(filePath, sb.ToString());
            }
            catch
            {
                MessageBox.Show("DEBUG", "Error writing out Workbook Tree to " + filePath, MessageBoxButton.OK, MessageBoxImage.Error);
            };
        }
    }
}
