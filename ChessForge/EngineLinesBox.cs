using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Timers;
using System.Text;
using ChessPosition;
using GameTree;
using System.Windows.Shapes;

namespace ChessForge
{
    /// <summary>
    /// Handles GUI elements visualizing engine's real-time calculations
    /// i.e. displaying the dynamic lines, progress bar and handling the
    /// stop request.
    /// </summary>
    public class EngineLinesBox
    {
        // Text box showing engine lines during evaluation
        private static TextBox _tbEvalLines;

        // progress bar for engine evaluation
        private static ProgressBar _pbEngineEval;

        /// <summary>
        /// Evaluation lines obtained from the engine.
        /// </summary>
        public static List<MoveEvaluation> Lines = new List<MoveEvaluation>();

        // Application's Main Window
        private static MainWindow _mainWin;

        /// <summary>
        /// Initializes the object with GUI references.
        /// </summary>
        /// <param name="textBox"></param>
        /// <param name="progBar"></param>
        /// <param name="evalState"></param>
        public static void Initialize(MainWindow mainWin, TextBox textBox, ProgressBar progBar)
        {
            _mainWin = mainWin;

            _tbEvalLines = textBox;
            _pbEngineEval = progBar;
        }

        /// <summary>
        /// Resets the evaluation progress bar to 0
        /// </summary>
        public static void ResetEvaluationProgressBar()
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
        public static void ShowEngineLines(object source, ElapsedEventArgs e)
        {
            try
            {
                if (EvaluationManager.CurrentMode != EvaluationManager.Mode.ENGINE_GAME && EvaluationManager.GetEvaluatedNode() != null)
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
                        string txt = sb.ToString();
                        if (!string.IsNullOrWhiteSpace(txt))
                        {
                            _tbEvalLines.Text = sb.ToString();
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                if (Configuration.DebugMode != 0)
                {
                    AppLog.Message("ShowEngineLines(): " + ex.Message);
                }
            }

            _pbEngineEval.Dispatcher.Invoke(() =>
            {
                _pbEngineEval.Value = _mainWin.Timers.GetElapsedTime(AppTimers.StopwatchId.EVALUATION_ELAPSED_TIME);
            });
        }

        /// <summary>
        /// Builds text for an individual line. 
        /// </summary>
        /// <param name="lineNo"></param>
        /// <param name="line"></param>
        /// <returns></returns>
        private static string BuildLineText(int lineNo, MoveEvaluation line)
        {
            try
            {
                BoardPosition position = EvaluationManager.GetEvaluatedNode().Position;
                if (line == null || position == null)
                {
                    return " ";
                }

                string eval = EvaluationManager.BuildEvaluationText(line, position.ColorToMove);

                if (eval == "#")
                {
                    return "# checkmate";
                }
                else
                {

                    uint moveNoToShow = position.ColorToMove == PieceColor.Black ?
                        position.MoveNumber : (position.MoveNumber + 1);

                    string sMoveNo = moveNoToShow.ToString() + (position.ColorToMove == PieceColor.White ? "." : "...");
                    if (string.IsNullOrEmpty(line.Line))
                    {
                        sMoveNo = "";
                    }

                    string moveSeq = BuildMoveSequence(line.Line);
                    if (moveSeq.Length == 0)
                    {
                        return "";
                    }
                    else
                    {
                        return (lineNo + 1).ToString() + ". (" + eval + "): "
                            + sMoveNo
                            + BuildMoveSequence(line.Line);
                    }
                }
            }
            catch
            {
                if (Configuration.DebugMode != 0)
                {
                    AppLog.Message("BuildTextLine() exception: LineNo=" + lineNo.ToString() + ". LineText: " + line.Line);
                }
                // this will indicate to us in the GUI that something went wrong.
                return "******";
            }
        }

        /// <summary>
        /// Builds the algebraic notation for the move sequence in the line.
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private static string BuildMoveSequence(string line)
        {
            string[] moves = line.Split(' ');

            StringBuilder sb = new StringBuilder();
            // make a copy of the position under evaluation
            TreeNode nd = EvaluationManager.GetEvaluatedNode();
            if (nd != null)
            {
                BoardPosition workingPosition = new BoardPosition(nd.Position);
                bool firstMove = true;
                try
                {
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
                }
                catch (Exception ex)
                {
                    if (Configuration.DebugMode != 0)
                    {
                        AppLog.Message("Exception in BuildMoveSequence(): " + ex.Message);
                    }
                }

                return sb.ToString();
            }
            else
            {
                AppLog.Message("Error in BuildMoveSequence(): EvaluationManager.GetEvaluatedNode() returned null.");
                return "";
            }
        }
    }
}
