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
        private TextBox _textBox;
        private ProgressBar _progBar;
        private EvaluationState _evalState;

        /// <summary>
        /// Initialzes the object with GUI references.
        /// </summary>
        /// <param name="textBox"></param>
        /// <param name="progBar"></param>
        /// <param name="evalState"></param>
        public EngineEvaluationGUI(TextBox textBox, ProgressBar progBar, EvaluationState evalState)
        {
            _textBox = textBox;
            _progBar = progBar;
            _evalState = evalState;
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
                List<MoveEvaluation> lines = new List<MoveEvaluation>();
                lock (EngineMessageProcessor.MoveCandidatesLock)
                {
                    // make a copy of Move candidates so we can release the lock asap
                    foreach (MoveEvaluation me in EngineMessageProcessor.MoveCandidates)
                    {
                        lines.Add(new MoveEvaluation(me));
                    }
                }

                StringBuilder sb = new StringBuilder();
                _textBox.Dispatcher.Invoke(() =>
                {
                    for (int i = 0; i < lines.Count; i++)
                    {
                        sb.Append(BuildLineText(i, lines[i]));
                        sb.Append(Environment.NewLine);
                    }
                    _textBox.Text = sb.ToString();
                });

                if (lines.Count > 0)
                {
                    _evalState.PositionCpScore = lines[0].ScoreCp;
                }
            }

            _progBar.Dispatcher.Invoke(() =>
            {
                _progBar.Value = AppState.MainWin.Timers.GetElapsedTime(AppTimers.StopwatchId.EVALUATION_PROGRESS);
            });
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
                    return " ";

                int intEval = _evalState.Position.ColorToMove == PieceColor.White ? line.ScoreCp : -1 * line.ScoreCp;
                string eval = (((double)intEval) / 100.0).ToString("F2");
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
