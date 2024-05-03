using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using ChessPosition;
using GameTree;

namespace ChessForge
{
    /// <summary>
    /// Manages the state of an Evaluation process performed
    /// by the engine.
    /// Only one evaluation can be performed at any given time.
    /// </summary>
    public class EvaluationManager
    {
        /// <summary>
        /// Lock object to use when accessing this object's data
        /// </summary>
        public static object EvaluationLock = new object();

        /// <summary>
        /// There can only be no or one evaluation happening
        /// at any given time.
        /// The EvaluationMode defines whether the evaluation is
        /// running for a single move, as requested by the user, or
        /// for the entire Active Line, also requested by the user,
        /// or if it is happening during the practice game against
        /// the user.
        /// </summary>
        public enum Mode
        {
            // No evaluation currently in progress
            IDLE,
            // Evaluation of all moves in the Active Line is in progress, move by move automatically
            LINE,
            // Continuous (infinite) evaluation for the currently selected move in the Active Lines
            CONTINUOUS,
            // Working out engine's response during a game against the engine
            ENGINE_GAME,
        };

        /// <summary>
        /// What type of Line are we evaluating when if the LINE mode.
        /// </summary>
        public enum LineSource
        {
            NONE,
            ACTIVE_LINE,
            TRAINING_LINE,
            MODEL_GAME_LINE
        }

        // an object of the EvaluationLine-drived class for the line being evaluated
        private static EvaluationLine _evaluationLine;

        // Current evaluation mode
        private static Mode _currentMode = Mode.IDLE;

        // TreeNode being evaluated if the mode is not LINE
        private static TreeNode _evaluatedSingleNode;

        /// <summary>
        /// The current evaluation mode.
        /// This property is read only.
        /// Clients can set a new mode by calling ChangeCurrentMode
        /// </summary>
        public static Mode CurrentMode
        {
            get { return _currentMode; }
        }

        /// <summary>
        /// Returns the Node being evaluated and 
        /// the Tree id, the node belongs to.
        /// </summary>
        /// <returns></returns>
        public static TreeNode GetEvaluatedNode(out int treeId)
        {
            //TODO: implement tree id
            treeId = -1;

            if (_currentMode != Mode.LINE)
            {
                return _evaluatedSingleNode;
            }
            else
            {
                return _evaluationLine.GetCurrentEvaluatedNode();
            }
        }

        /// <summary>
        /// Returns the node being currently slated for evaluation.
        /// If we are evaluating a training line it will be taken from
        /// the TrainingLine list, if we are evaluating the Active Line
        /// it will be taken from there based on the current position index.
        /// </summary>
        /// <returns></returns>
        public static TreeNode GetCurrentEvaluatedLineNode()
        {
            return _evaluationLine.GetNextNodeToEvaluate();
        }

        /// <summary>
        /// Sets the index in the list of nodes from which to start evaluation.
        /// </summary>
        /// <param name="index"></param>
        public static void SetStartNodeIndex(int index)
        {
            _evaluationLine.SetStartNodeIndex(index);
        }

        public static int GetLineNodeIndex()
        {
            return _evaluationLine.GetNodeIndex();
        }

        public static bool IsLastPositionIndex()
        {
            return _evaluationLine.IsLastPositionIndex();
        }

        /// <summary>
        /// Gets next move to evaluate and makes it current.
        /// </summary>
        /// <returns></returns>
        public static TreeNode GetNextLineNodeToEvaluate()
        {
            return _evaluationLine.GetNextNodeToEvaluate();
        }

        /// <summary>
        /// Resets the lists of evaluated nodes. 
        /// </summary>
        public static void ResetLineNodesToEvaluate()
        {
            if (_evaluationLine != null)
            {
                _evaluationLine.ResetsNodesToEvaluate();
            }
        }

        /// <summary>
        /// Adds a run to the list of evaluated nodes and runs if
        /// we are evaluating a Training Line.
        /// </summary>
        /// <param name="r"></param>
        public static void AddLineRunToEvaluate(Run r)
        {
            _evaluationLine.AddRunToEvaluate(r);
        }

        /// <summary>
        /// Switches to a new Evaluation mode.
        /// Makes sure that the app timers are appropriately set or reset.
        /// If the mode is LINE, the lineSource argument must be set to a value
        /// other then LineSource.NONE
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="lineSource"></param>
        public static void ChangeCurrentMode(Mode mode, bool updateGui = true, LineSource lineSource = LineSource.NONE)
        {
            Mode previousMode = _currentMode;

            _currentMode = mode;
            switch (_currentMode)
            {
                case Mode.IDLE:
                    AppState.MainWin.Timers.Stop(AppTimers.StopwatchId.EVALUATION_ELAPSED_TIME);
                    AppState.MainWin.Timers.Stop(AppTimers.TimerId.EVALUATION_LINE_DISPLAY);
                    EngineMessageProcessor.StopEngineEvaluation();
                    AppState.SwapCommentBoxForEngineLines(TrainingSession.IsContinuousEvaluation);

                    AppState.MainWin.UiEvalChart.IsDirty = true;
                    AppState.MainWin.UiEvalChart.Update();
                    break;
                case Mode.CONTINUOUS:
                    AppState.MainWin.Timers.Stop(AppTimers.StopwatchId.EVALUATION_ELAPSED_TIME);
                    AppState.MainWin.Timers.Start(AppTimers.TimerId.EVALUATION_LINE_DISPLAY);
                    break;
                case Mode.LINE:
                    switch (lineSource)
                    {
                        case LineSource.TRAINING_LINE:
                            _evaluationLine = new EvaluationTrainingLine();
                            break;
                        case LineSource.ACTIVE_LINE:
                            _evaluationLine = new EvaluationActiveLine();
                            break;
                        default:
                            DebugUtils.ShowDebugMessage("ERROR: Line evaluation requested with no Line Type provided");
                            break;
                    }
                    AppState.MainWin.Timers.Start(AppTimers.StopwatchId.EVALUATION_ELAPSED_TIME);
                    AppState.MainWin.Timers.Start(AppTimers.TimerId.EVALUATION_LINE_DISPLAY);
                    break;
                case Mode.ENGINE_GAME:
                    AppState.MainWin.Timers.Start(AppTimers.StopwatchId.EVALUATION_ELAPSED_TIME);
                    AppState.MainWin.Timers.Stop(AppTimers.TimerId.EVALUATION_LINE_DISPLAY);
                    break;
            }

            AppLog.Message(LogLevel.DETAIL, "EvaluationManager:ChangeCurrentMode() to " + mode.ToString());
            if (updateGui)
            {
                AppState.SetupGuiForCurrentStates();
            }
        }

        /// <summary>
        /// Reset the state to get ready for another evaluation run.
        /// </summary>
        public static void Reset(bool updateGui = true)
        {
            lock (EvaluationLock)
            {
                ChangeCurrentMode(Mode.IDLE, false);
            }
            if (updateGui)
            {
                AppState.SetupGuiForCurrentStates();
            }
        }

        /// <summary>
        /// Indicates whether any kind of evaluation is happening
        /// at the moment.
        /// </summary>
        public static bool IsRunning
        {
            get
            {
                return CurrentMode != Mode.IDLE;
            }
        }

        /// <summary>
        /// Sets position to evaluate that is not part of LINE evaluation.
        /// </summary>
        /// <param name="Position"></param>
        public static void SetSingleNodeToEvaluate(TreeNode nd)
        {
            _evaluatedSingleNode = nd;
        }

        /// <summary>
        /// Builds evaluation text ready to be included in a GUI element.
        /// It will produce a double value with 2 decimal digits or an
        /// indication of mate in a specified number of moves.
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public static string BuildEvaluationText(MoveEvaluation line, PieceColor colorToMove)
        {
            string eval;

            if (!line.IsMateDetected)
            {
                int intEval = colorToMove == PieceColor.White ? line.ScoreCp : -1 * line.ScoreCp;
                if (intEval > 0)
                {
                    eval = "+" + (((double)intEval) / 100.0).ToString("F2");
                }
                else
                {
                    eval = (((double)intEval) / 100.0).ToString("F2");
                }
            }
            else
            {
                if (line.MovesToMate == 0)
                {
                    eval = "#";
                }
                else
                {
                    int movesToMate = colorToMove == PieceColor.White ? line.MovesToMate : -1 * line.MovesToMate;
                    string sign = Math.Sign(movesToMate) > 0 ? "+" : "-";
                    eval = sign + "#" + (Math.Abs(line.MovesToMate)).ToString();
                }
            }

            return eval;
        }
    }
}
