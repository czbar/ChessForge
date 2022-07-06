using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using ChessPosition;

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
            NONE,
            SINGLE_MOVE,
            FULL_LINE,
            IN_GAME_PLAY
        };

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
                Mode = EvaluationMode.NONE;
                Position = null;
                PositionCpScore = 0;
                PositionIndex = 0;

                if (ProgressTimer != null)
                {
                    ProgressTimer.Stop();
                    ProgressTimer.Reset();
                }
            }
        }

        /// <summary>
        /// This will be called when in LINE evaluation or GAME mode
        /// The progress bar timer must be reset. 
        /// </summary>
        public void PrepareToContinue()
        {
            if (ProgressTimer != null)
            {
                ProgressTimer.Stop();
                ProgressTimer.Reset();
            }
        }

        /// <summary>
        /// Timer used by the evaluation progress bar to set
        /// its position.
        /// </summary>
        public Stopwatch ProgressTimer = new Stopwatch();

        /// <summary>
        /// The current evaluation mode.
        /// </summary>
        public EvaluationMode Mode = EvaluationMode.NONE;

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
                    return Mode != EvaluationMode.NONE;
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
        public int PositionCpScore
        {
            get
            {
                lock (EvaluationLock)
                {
                    return _positionCpScore;
                }
            }
            set
            {
                lock (EvaluationLock)
                {
                    _positionCpScore = value;
                }
            }
        }

        private BoardPosition _position;
        private int _positionIndex;
        private int _positionCpScore;
    }
}
