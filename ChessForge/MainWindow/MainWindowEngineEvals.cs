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
    public partial class MainWindow : Window
    {
        public void tbMoveEvaluation_ShowEngineLines(object source, ElapsedEventArgs e)
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
            _tbEngineLines.Dispatcher.Invoke(() =>
            {
                for (int i = 0; i < lines.Count; i++)
                {
                    sb.Append(BuildRunText(i, lines[i]));
                    sb.Append(Environment.NewLine);
                }
                _tbEngineLines.Text = sb.ToString();
            });

            if (lines.Count > 0)
            {
                Evaluation.PositionCpScore = lines[0].ScoreCp;
            }
            progEvaluation.Dispatcher.Invoke(() =>
            {
                progEvaluation.Value = Evaluation.Timer.ElapsedMilliseconds;
            });
        }

        private string BuildRunText(int lineNo, MoveEvaluation line)
        {
            try
            {
                if (line == null || Evaluation.Position == null)
                    return " ";
                int intEval = Evaluation.Position.ColorToMove == PieceColor.White ? line.ScoreCp : -1 * line.ScoreCp;
                string eval = (((double)intEval) / 100.0).ToString("F2");
                uint moveNoToShow = Evaluation.Position.ColorToMove == PieceColor.Black ? Evaluation.Position.MoveNumber : (Evaluation.Position.MoveNumber + 1);
                return (lineNo + 1).ToString() + ". (" + eval + "): "
                    + moveNoToShow.ToString()
                    + (Evaluation.Position.ColorToMove == PieceColor.White ? "." : "...")
                    + BuildLineText(line.Line);
            }
            catch
            {
                return "******";
            }
        }

        private string BuildLineText(string line)
        {
            string[] moves = line.Split(' ');

            StringBuilder sb = new StringBuilder();
            // make a copy of the position under evaluation
            BoardPosition workingPosition = new BoardPosition(Evaluation.Position);
            bool firstMove = true;
            foreach (string move in moves)
            {
                if (workingPosition.ColorToMove == PieceColor.White && !firstMove)
                {
                    sb.Append(workingPosition.MoveNumber.ToString() + ".");
                }
                firstMove = false;
                sb.Append(MoveUtils.EngineNotationToAlgebraic(move, ref workingPosition));
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
