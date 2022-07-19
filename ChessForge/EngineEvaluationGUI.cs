using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Timers;
using System.Text;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using ChessForge;
using ChessPosition;
using GameTree;

namespace ChessForge
{
    /// <summary>
    /// Handles GUI elements visualizing engine's real-time calculations
    /// i.e. displaying the dynamic lines, progrress bar and handling the
    /// stop request.
    /// </summary>
    public class EngineEvaluationGUI
    {
        // Text box showing engine lines during evaluation
        private TextBox _tbEvalLines;

        // progress bar for engine evaluation
        private ProgressBar _pbEngineEval;

        // reference to EvaluationState object.
        private EvaluationState _evalState;

        public List<MoveEvaluation> Lines = new List<MoveEvaluation>();

        /// <summary>
        /// Initialzes the object with GUI references.
        /// </summary>
        /// <param name="textBox"></param>
        /// <param name="progBar"></param>
        /// <param name="evalState"></param>
        public EngineEvaluationGUI(TextBox textBox, ProgressBar progBar, EvaluationState evalState)
        {
            _tbEvalLines = textBox;
            _pbEngineEval = progBar;
            _evalState = evalState;
        }

        /// <summary>
        /// Resets the evaluation progress bar to 0
        /// </summary>
        public void ResetEvaluationProgressBar()
        {
            _pbEngineEval.Dispatcher.Invoke(() =>
            {
                _pbEngineEval.Value = 0;
            });
        }

        /// <summary>
        /// Shows the latest engine lines in response to 
        /// a timer event.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        public void ShowEngineLines(object source, ElapsedEventArgs e)
        {
            if (_evalState.Mode != EvaluationState.EvaluationMode.IN_GAME_PLAY)
            {
                Lines.Clear();
                lock (EngineMessageProcessor.MoveCandidatesLock)
                {
                    // make a copy of Move candidates so we can release the lock asap
                    foreach (MoveEvaluation me in EngineMessageProcessor.MoveCandidates)
                    {
                        Lines.Add(new MoveEvaluation(me));
                    }
                }

                StringBuilder sb = new StringBuilder();
                _tbEvalLines.Dispatcher.Invoke(() =>
                {
                    for (int i = 0; i < Lines.Count; i++)
                    {
                        sb.Append(BuildLineText(i, Lines[i]));
                        sb.Append(Environment.NewLine);
                    }
                    _tbEvalLines.Text = sb.ToString();
                });

                if (Lines.Count > 0 && _evalState.Position != null)
                {
                    _evalState.PositionEvaluation = BuildEvaluationText(Lines[0], _evalState.Position.ColorToMove);
                }
            }

            _pbEngineEval.Dispatcher.Invoke(() =>
            {
                _pbEngineEval.Value = AppState.MainWin.Timers.GetElapsedTime(AppTimers.StopwatchId.EVALUATION_PROGRESS);
            });
        }

        /// <summary>
        /// Builds evaluation text ready to be included in a GUI element.
        /// It will produce a double value with 2 decimal digits or an
        /// indication of mate in a specified number of moves.
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private string BuildEvaluationText(MoveEvaluation line, PieceColor colorToMove)
        {
            string eval;

            if (!line.IsMateDetected)
            {
                int intEval = colorToMove == PieceColor.White ? line.ScoreCp : -1 * line.ScoreCp;
                eval = (((double)intEval) / 100.0).ToString("F2");
            }
            else
            {
                int movesToMate = colorToMove == PieceColor.White ? line.MovesToMate : -1 * line.MovesToMate;
                string sign = Math.Sign(movesToMate) > 0 ? "+" : "-";
                eval = sign + "#" + (Math.Abs(line.MovesToMate)).ToString();
            }

            return eval;
        }

        /// <summary>
        /// Builds text for an individual line. 
        /// </summary>
        /// <param name="lineNo"></param>
        /// <param name="line"></param>
        /// <returns></returns>
        private string BuildLineText(int lineNo, MoveEvaluation line)
        {
            try
            {
                if (line == null || _evalState.Position == null)
                {
                    return " ";
                }

                string eval = BuildEvaluationText(line, _evalState.Position.ColorToMove);

                uint moveNoToShow = _evalState.Position.ColorToMove == PieceColor.Black ? 
                    _evalState.Position.MoveNumber : (_evalState.Position.MoveNumber + 1);
                
                return (lineNo + 1).ToString() + ". (" + eval + "): "
                    + moveNoToShow.ToString()
                    + (_evalState.Position.ColorToMove == PieceColor.White ? "." : "...")
                    + BuildMoveSequence(line.Line);
            }
            catch
            {
                // this will indicate to us in the GUI that something went wrong.
                return "******";
            }
        }

        /// <summary>
        /// Builds the algebraic notation for the move sequence in the line.
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private string BuildMoveSequence(string line)
        {
            string[] moves = line.Split(' ');

            StringBuilder sb = new StringBuilder();
            // make a copy of the position under evaluation
            BoardPosition workingPosition = new BoardPosition(_evalState.Position);
            bool firstMove = true;
            foreach (string move in moves)
            {
                if (workingPosition.ColorToMove == PieceColor.White && !firstMove)
                {
                    sb.Append(workingPosition.MoveNumber.ToString() + ".");
                }
                firstMove = false;
                bool isCastle;
                sb.Append(MoveUtils.EngineNotationToAlgebraic(move, ref workingPosition, out isCastle));
                // invert colors
                workingPosition.ColorToMove = workingPosition.ColorToMove == PieceColor.White ? PieceColor.Black : PieceColor.White;
                sb.Append(" ");
                if (workingPosition.ColorToMove == PieceColor.White)
                {
                    workingPosition.MoveNumber++;
                }
            }

            return sb.ToString();
        }
    }
}
