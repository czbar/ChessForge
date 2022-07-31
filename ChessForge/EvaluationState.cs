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
    public class EvaluationState
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
        public enum EvaluationMode
        {
            IDLE,
            MANUAL_SINGLE_MOVE,
            MANUAL_LINE,

            TRAINING_SINGLE_MOVE,
            TRAINING_LINE,

            IN_GAME_PLAY,
        };

        /// <summary>
        /// The list of Runs to evaluate when we are evaluating a line
        /// in the Training mode.
        /// </summary>
        private List<Run> _runsToEvaluate = new List<Run>();

        private List<TreeNode> _nodesToEvaluate = new List<TreeNode>();

        private int _runToEvaluateIndex = -1;

        public void AddRunToEvaluate(Run r)
        {
            int nodeId = GuiUtilities.GetNodeIdFromPrefixedString(r.Name);
            TreeNode nd = AppState.MainWin.Workbook.GetNodeFromNodeId(nodeId);
            
            _nodesToEvaluate.Add(nd);
            _runsToEvaluate.Add(r);
        }

        public Run GetCurrentEvaluatedRun()
        {
            if (_runToEvaluateIndex < 0 || _runToEvaluateIndex >= _runsToEvaluate.Count)
            {
                return null;
            }

            return _runsToEvaluate[_runToEvaluateIndex];
        }

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
        /// evluation run.
        /// </summary>
        public void Reset()
        {
            lock (EvaluationLock)
            {
                CurrentMode = EvaluationMode.IDLE;
                Position = null;
                PositionEvaluation = "";
                PositionIndex = 0;

                AppState.MainWin.Timers.Stop(AppTimers.StopwatchId.EVALUATION_ELAPSED_TIME);
            }
        }

        /// <summary>
        /// This will be called when in LINE evaluation or GAME mode
        /// The progress bar timer must be reset. 
        /// </summary>
        public void PrepareToContinue()
        {
            AppState.MainWin.Timers.Stop(AppTimers.StopwatchId.EVALUATION_ELAPSED_TIME);
        }

        /// <summary>
        /// The current evaluation mode.
        /// </summary>
        public EvaluationMode CurrentMode = EvaluationMode.IDLE;

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
                    return CurrentMode != EvaluationMode.IDLE;
                }
            }
        }

        /// <summary>
        /// The position being evaluated.
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
            set
            {
                lock (EvaluationLock)
                {
                    _position = value;
                }
            }
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
                }
            }
        }

        /// <summary>
        /// The centipawn score evaluated for the position.
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

        private BoardPosition _position;
        private int _positionIndex;
        private string _positionEvaluation = "";
    }
}
