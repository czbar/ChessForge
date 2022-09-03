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
        private Mode _currentMode = Mode.IDLE;

        // Position being evaluated
        private BoardPosition _position;

        // Index of the position being evaluated in the ActiveLine
        private int _positionIndex;

        // Text value of the evaluation
        private string _positionEvaluation = "";

        // The list of Runs to evaluate when we are evaluating a line
        // in the Training mode.
        private List<Run> _runsToEvaluate = new List<Run>();

        // The lists of Nodes corresponding to the Runs in _runsToEvaluate
        private List<TreeNode> _nodesToEvaluate = new List<TreeNode>();

        // Current index of the run to evaluate.
        // If the evaluation has noit started yet it is set to -1.
        private int _runToEvaluateIndex = -1;

        /// <summary>
        /// Adds a Run to the list of _runsToEvaluate.
        /// At the same time, adds the corresponding Node
        /// to the list of Nodes.
        /// </summary>
        /// <param name="r"></param>
        public void AddRunToEvaluate(Run r)
        {
            int nodeId = GuiUtilities.GetNodeIdFromPrefixedString(r.Name);
            TreeNode nd = AppStateManager.Workbook.GetNodeFromNodeId(nodeId);
            
            _nodesToEvaluate.Add(nd);
            _runsToEvaluate.Add(r);
        }

        /// <summary>
        /// Returns the Run currently being evaluated.
        /// </summary>
        /// <returns></returns>
        public Run GetCurrentEvaluatedRun()
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
        public TreeNode GetCurrentEvaluatedNode()
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
        public TreeNode GetNextNodeToEvaluate()
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
        /// Clears the list of Nodes and Runs.
        /// Resets the evaluation index.
        /// </summary>
        public void ClearRunsToEvaluate()
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
        public void Reset()
        {
            lock (EvaluationLock)
            {
                _currentMode = Mode.IDLE;
                _position = null;
                _positionEvaluation = "";
                _positionIndex = 0;

                AppStateManager.MainWin.Timers.Stop(AppTimers.StopwatchId.EVALUATION_ELAPSED_TIME);
            }
            AppStateManager.ShowEvaluationProgressControlsForCurrentStates();
        }

        /// <summary>
        /// The current evaluation mode.
        /// </summary>
        public Mode CurrentMode
        {
            get { return _currentMode; }
            set {_currentMode = value; }
        }

        /// <summary>
        /// Indicates whether any kind of evaluation is happening
        /// at the moment.
        /// </summary>
        public bool IsRunning
        {
            get
            {
                lock (EvaluationLock)
                {
                    return CurrentMode != Mode.IDLE;
                }
            }
        }

        /// <summary>
        /// The position being evaluated.
        /// This property is read only.
        /// </summary>
        public BoardPosition Position
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
        public void SetPositionToEvaluate(BoardPosition Position)
        {
            _position = Position;
            _positionIndex = -1;
        }

        /// <summary>
        /// Evaluated position's index in the Active Line,
        /// if applicable.
        /// </summary>
        public int PositionIndex
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
        public string PositionEvaluation
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

    }
}
