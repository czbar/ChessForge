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

        // Current evaluation mode
        private static Mode _currentMode = Mode.IDLE;

        // Position being evaluated
        private static BoardPosition _position;

        // Index of the position being evaluated in the ActiveLine
        private static int _positionIndex;

        // Text value of the evaluation
        private static string _positionEvaluation = "";

        // The list of Runs to evaluate when we are evaluating a line
        // in the Training mode.
        private static List<Run> _runsToEvaluate = new List<Run>();

        // The lists of Nodes corresponding to the Runs in _runsToEvaluate
        private static List<TreeNode> _nodesToEvaluate = new List<TreeNode>();

        // Current index of the run to evaluate.
        // If the evaluation has not started yet it is set to -1.
        private static int _runToEvaluateIndex = -1;

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
        /// Switches to a new Evaluation mode.
        /// Makes sure that app timers are appropriately
        /// set of reset.
        /// </summary>
        /// <param name="mode"></param>
        public static void ChangeCurrentMode(Mode mode)
        {
            _currentMode = mode;
            switch (_currentMode)
            {
                case Mode.IDLE:
                    AppStateManager.MainWin.Timers.Stop(AppTimers.StopwatchId.EVALUATION_ELAPSED_TIME);
                    AppStateManager.MainWin.Timers.Stop(AppTimers.TimerId.EVALUATION_LINE_DISPLAY);
                    break;
                case Mode.CONTINUOUS:
                    AppStateManager.MainWin.Timers.Stop(AppTimers.StopwatchId.EVALUATION_ELAPSED_TIME);
                    AppStateManager.MainWin.Timers.Start(AppTimers.TimerId.EVALUATION_LINE_DISPLAY);
                    break;
                case Mode.LINE:
                    AppStateManager.MainWin.Timers.Start(AppTimers.StopwatchId.EVALUATION_ELAPSED_TIME);
                    AppStateManager.MainWin.Timers.Start(AppTimers.TimerId.EVALUATION_LINE_DISPLAY);
                    break;
                case Mode.ENGINE_GAME:
                    AppStateManager.MainWin.Timers.Start(AppTimers.StopwatchId.EVALUATION_ELAPSED_TIME);
                    AppStateManager.MainWin.Timers.Stop(AppTimers.TimerId.EVALUATION_LINE_DISPLAY);
                    break;
            }

            AppStateManager.SetupGuiForCurrentStates();
        }

        /// <summary>
        /// Returns the Run currently being evaluated.
        /// </summary>
        /// <returns></returns>
        public static Run GetCurrentEvaluatedRun()
        {
            if (_runToEvaluateIndex < 0 || _runToEvaluateIndex >= _runsToEvaluate.Count)
            {
                return null;
            }

            return _runsToEvaluate[_runToEvaluateIndex];
        }

        /// <summary>
        /// Returns the Node currently being evaluated.
        /// </summary>
        /// <returns></returns>
        public static TreeNode GetCurrentEvaluatedNode()
        {
            if (_runToEvaluateIndex < 0 || _runToEvaluateIndex >= _runsToEvaluate.Count)
            {
                return null;
            }

            return _nodesToEvaluate[_runToEvaluateIndex];
        }

        /// <summary>
        /// Returns the next node to evaluate
        /// or null if we reached the end of the list.
        /// If there are no more Nodes to evaluate,
        /// the lists are reset.
        /// </summary>
        /// <returns></returns>
        public static TreeNode GetNextNodeToEvaluate()
        {
            _runToEvaluateIndex++;
            if (_runToEvaluateIndex < _nodesToEvaluate.Count)
            {
                return _nodesToEvaluate[_runToEvaluateIndex];
            }
            else
            {
                ClearRunsToEvaluate();
                return null;
            }
        }

        /// <summary>
        /// Adds a Run to the list of _runsToEvaluate.
        /// At the same time, adds the corresponding Node
        /// to the list of Nodes.
        /// </summary>
        /// <param name="r"></param>
        public static void AddRunToEvaluate(Run r)
        {
            int nodeId = TextUtils.GetNodeIdFromPrefixedString(r.Name);
            TreeNode nd = AppStateManager.Workbook.GetNodeFromNodeId(nodeId);

            _nodesToEvaluate.Add(nd);
            _runsToEvaluate.Add(r);
        }

        /// <summary>
        /// Clears the list of Nodes and Runs.
        /// Resets the evaluation index.
        /// </summary>
        public static void ClearRunsToEvaluate()
        {
            _runsToEvaluate.Clear();
            _nodesToEvaluate.Clear();
            _runToEvaluateIndex = -1;
        }

        /// <summary>
        /// Lock object to use when accessing this object's data
        /// </summary>
        public static object EvaluationLock = new object();

        /// <summary>
        /// Reset the state to get ready for another
        /// evaluation run.
        /// </summary>
        public static void Reset()
        {
            lock (EvaluationLock)
            {
                _position = null;
                _positionEvaluation = "";
                _positionIndex = 0;

                ChangeCurrentMode(Mode.IDLE);
            }
            AppStateManager.ShowEvaluationProgressControlsForCurrentStates();
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
        /// The position being evaluated.
        /// This property is read only.
        /// </summary>
        public static BoardPosition Position
        {
            get
            {
                lock (EvaluationLock)
                {
                    return _position;
                }
            }
        }

        /// <summary>
        /// Set position to evaluate.
        /// We want to force the client to invoke SetPositionToEvaluate()
        /// stating the intent that it is not part of the Active Line.
        /// explicitly which is why the Position property is readonly.
        /// The position index is therefore set to -1.
        /// </summary>
        /// <param name="Position"></param>
        public static void SetPositionToEvaluate(BoardPosition Position)
        {
            _position = Position;
            _positionIndex = -1;
        }

        /// <summary>
        /// Evaluated position's index in the Active Line,
        /// if applicable.
        /// </summary>
        public static int PositionIndex
        {
            get
            {
                lock (EvaluationLock)
                {
                    return _positionIndex;
                }
            }
            set
            {
                lock (EvaluationLock)
                {
                    _positionIndex = value;
                    if (_positionIndex >= 0)
                    {
                        _position = AppStateManager.MainWin.ActiveLine.GetNodeAtIndex(_positionIndex).Position;
                    }
                }
            }
        }

        /// <summary>
        /// Accessor for the position evaluation text value.
        /// </summary>
        public static string PositionEvaluation
        {
            get
            {
                lock (EvaluationLock)
                {
                    return _positionEvaluation;
                }
            }
            set
            {
                lock (EvaluationLock)
                {
                    _positionEvaluation = value;
                }
            }
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
                eval = (((double)intEval) / 100.0).ToString("F2");
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
