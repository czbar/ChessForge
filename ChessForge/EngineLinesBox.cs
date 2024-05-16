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
        public static Dictionary<TreeNode, MoveCandidates> EvalLinesToProcess = new Dictionary<TreeNode, MoveCandidates>();

        // Application's Main Window
        private static MainWindow _mainWin;

        // A flag indicating whether ShowEngineLines() is currently running
        private static bool _isShowEngineLinesRunning = false;

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
        /// Shows the latest engine lines.
        /// a timer event.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        /// <param name="force">if true request the display of the lines even if the current one is in progress.</param>
        public static void ShowEngineLines(object source, ElapsedEventArgs e, bool force = false)
        {
            // prevent "choking"
            if (AppState.MainWin.ProcessingMouseUp || (!force && _isShowEngineLinesRunning && (source == null || source is Timer)))
            {
                return;
            }
            else
            {
                _isShowEngineLinesRunning = false;
                ShowEngineLinesEx(source, e);
            }
        }

        /// <summary>
        /// A counter that will be set if we encounter an excpetion while processing lines.
        /// Each invokation of ShowEngineLinesEx() will then decrease the counter.
        /// This is so that we don't get stuck in a tight loop handling exceptions.
        /// </summary>
        private static int _pauseCount = 0;

        /// <summary>
        /// Shows the latest engine lines in response to a timer event
        /// or when called from ShowEngineLines().
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        public static void ShowEngineLinesEx(object source, ElapsedEventArgs e)
        {
            // check if we are paused.
            if (AppState.MainWin.ProcessingMouseUp || _pauseCount > 0)
            {
                _pauseCount--;
                return;
            }

            try
            {
                _pbEngineEval.Dispatcher.Invoke(() =>
                {
                    if (_pbEngineEval.Visibility == Visibility.Visible)
                    {
                        _pbEngineEval.Value = _mainWin.Timers.GetElapsedTime(AppTimers.StopwatchId.EVALUATION_ELAPSED_TIME);
                    }
                });
            }
            catch
            {
            }


            if (_isShowEngineLinesRunning && (source == null || source is Timer))
            {
                return;
            }

            if (ShowSpecialText(source, e))
            {
                return;
            }

            _isShowEngineLinesRunning = true;

            try
            {
                if (AppState.ShowEvaluationLines())
                {
                    MoveCandidates moveCandidates = null;

                    EvalLinesToProcess.Clear();
                    lock (EngineMessageProcessor.MoveCandidatesLock)
                    {
                        //TODO: find a good way to establish that this is the correct move to prevent expensive exceptions
                        TreeNode evalNode = EngineMessageProcessor.EngineMoveCandidates.EvalNode;
                        if (evalNode != null)
                        {
                            if (!EvalLinesToProcess.ContainsKey(evalNode))
                            {
                                EvalLinesToProcess[evalNode] = new MoveCandidates();
                            }
                            moveCandidates = EvalLinesToProcess[evalNode];
                            EvalLinesToProcess[evalNode].EvalNode = EngineMessageProcessor.EngineMoveCandidates.EvalNode;
                            // make a copy of Move candidates so we can release the lock asap
                            foreach (MoveEvaluation me in EngineMessageProcessor.EngineMoveCandidates.Lines)
                            {
                                EvalLinesToProcess[evalNode].AddEvaluation(new MoveEvaluation(me, evalNode.Position.ColorToMove));
                            }
                        }
                    }

                    if (moveCandidates != null && moveCandidates.EvalNode != null)
                    {
                        StringBuilder sb = new StringBuilder();
                        moveCandidates.Lines.Sort();
                        _tbEvalLines.Dispatcher.Invoke(() =>
                        {
                            for (int i = 0; i < moveCandidates.Lines.Count; i++)
                            {
                                sb.Append(BuildLineText(moveCandidates.EvalNode, i, moveCandidates.Lines[i], out string eval));
                                // if we got eval string indicating exception, stop and "declare" pause
                                // otherwise we may end up blocking the app!
                                if (eval == Constants.EXCEPTION)
                                {
                                    _pauseCount = 4;
                                    break;
                                }

                                sb.Append(Environment.NewLine);

                                if (i == 0)
                                {
                                    AppState.MainWin.ActiveLine.SetEvaluation(moveCandidates.EvalNode, eval);
                                    moveCandidates.EvalNode.SetEngineEvaluation(eval);
                                    if (TrainingSession.IsTrainingInProgress)
                                    {
                                        AppState.MainWin.ShowEvaluationRunInTraining(moveCandidates.EvalNode);
                                    }
                                }

                            }
                            string txt = sb.ToString();
                            if (!string.IsNullOrWhiteSpace(txt))
                            {
                                _tbEvalLines.Text = sb.ToString();
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                if (Configuration.DebugLevel != 0)
                {
                    AppLog.Message("ShowEngineLines(): " + ex.Message);
                }
            }

            _isShowEngineLinesRunning = false;
        }

        /// <summary>
        /// Checks if there is a special text passed in the source parameters
        /// and if so displays it.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private static bool ShowSpecialText(object source, ElapsedEventArgs e)
        {
            bool handled = false;

            if (source is string && e == null)
            {
                ShowEmptyLines(source as string);
                handled = true;
            }
            else if (source is TreeNode)
            {
                TreeNode evalNode = (TreeNode)source;
                EngineMessageProcessor.EngineMoveCandidates.EvalNode = evalNode;
                bool isMate = evalNode.Position.IsCheckmate;
                bool isStalemate = evalNode.Position.IsStalemate;

                if (!isMate && !isStalemate)
                {
                    // if not handled but TreeNode was passed, let's check if it is a checkmate or stalemate after all
                    if (PositionUtils.IsCheckmate(evalNode.Position, out _))
                    {
                        evalNode.Position.IsCheckmate = true;
                    }
                    else if (PositionUtils.IsStalemate(evalNode.Position))
                    {
                        evalNode.Position.IsStalemate = true;
                    }
                }

                if (evalNode.Position.IsStalemate)
                {
                    ShowEmptyLines(Properties.Resources.Stalemate);
                    handled = true;
                }
                else if (evalNode.Position.IsCheckmate)
                {
                    ShowEmptyLines("# " + Properties.Resources.Checkmate);
                    handled = true;
                }
            }

            return handled;
        }


        /// <summary>
        /// Called when there are no lines to show and we 
        /// want to show some arbitrary text.
        /// </summary>
        /// <param name="dummy"></param>
        private static void ShowEmptyLines(string dummy)
        {
            _tbEvalLines.Dispatcher.Invoke(() =>
            {
                _tbEvalLines.Text = dummy;
            });
        }

        /// <summary>
        /// Builds text for an individual line. 
        /// </summary>
        /// <param name="lineNo"></param>
        /// <param name="line"></param>
        /// <returns></returns>
        private static string BuildLineText(TreeNode evalNode, int lineNo, MoveEvaluation line, out string eval)
        {
            eval = "";
            try
            {
                if (evalNode == null)
                {
                    return "";
                }

                BoardPosition position = evalNode.Position;
                if (line == null || position == null)
                {
                    return " ";
                }

                uint moveNumberOffset = 0;
                if (AppState.ActiveVariationTree != null)
                {
                    moveNumberOffset = AppState.ActiveVariationTree.MoveNumberOffset;
                }

                eval = EvaluationManager.BuildEvaluationText(line, position.ColorToMove);

                if (eval == "#")
                {
                    return "# " + Properties.Resources.Checkmate;
                }
                else
                {
                    uint moveNoToShow = position.ColorToMove == PieceColor.Black ?
                        position.MoveNumber : (position.MoveNumber + 1);

                    string sMoveNo = (moveNoToShow + moveNumberOffset).ToString() + (position.ColorToMove == PieceColor.White ? "." : "...");
                    if (string.IsNullOrEmpty(line.Line))
                    {
                        sMoveNo = "";
                    }

                    string moveSeq = BuildMoveSequence(evalNode, line.Line);
                    // check if BuildMoveSequence encounter an exception.
                    if (moveSeq == Constants.EXCEPTION)
                    {
                        eval = Constants.EXCEPTION;
                        return "";
                    }
                    else if (moveSeq.Length == 0)
                    {
                        return "";
                    }
                    else
                    {
                        return (lineNo + 1).ToString() + ". (" + eval + "): "
                            + sMoveNo
                            + moveSeq;
                    }
                }
            }
            catch
            {
                if (Configuration.DebugLevel != 0)
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
        private static string BuildMoveSequence(TreeNode evalNode, string line)
        {
            string[] moves = line.Split(' ');

            uint moveNumberOffset = 0;
            if (AppState.ActiveVariationTree != null)
            {
                moveNumberOffset = AppState.ActiveVariationTree.MoveNumberOffset;
            }

            StringBuilder sb = new StringBuilder();
            // make a copy of the position under evaluation
            if (evalNode != null)
            {
                BoardPosition workingPosition = new BoardPosition(evalNode.Position);
                workingPosition.InheritedEnPassantSquare = evalNode.Position.EnPassantSquare;

                bool firstMove = true;

                string debugMove = "";
                try
                {
                    foreach (string move in moves)
                    {
                        if (Configuration.DebugLevel != 0)
                        {
                            debugMove = move;
                        }
                        if (workingPosition.ColorToMove == PieceColor.White && !firstMove)
                        {

                            if (evalNode.Position.ColorToMove == PieceColor.White)
                            {
                                sb.Append((workingPosition.MoveNumber + moveNumberOffset + 1).ToString() + ".");
                            }
                            else
                            {
                                sb.Append((workingPosition.MoveNumber + moveNumberOffset).ToString() + ".");
                            }
                        }
                        firstMove = false;
                        bool isCastle;
                        string strMove = MoveUtils.EngineNotationToAlgebraic(move, ref workingPosition, out isCastle);
                        sb.Append(Languages.MapPieceSymbols(strMove, workingPosition.ColorToMove));
                        workingPosition.InheritedEnPassantSquare = workingPosition.EnPassantSquare;

                        // invert colors
                        workingPosition.ColorToMove = workingPosition.ColorToMove == PieceColor.White ? PieceColor.Black : PieceColor.White;
                        // after inversion
                        if (PositionUtils.IsKingInCheck(workingPosition, workingPosition.ColorToMove))
                        {
                            if (PositionUtils.IsCheckmate(workingPosition, out _))
                            {
                                sb.Append('#');
                            }
                            else
                            {
                                sb.Append('+');
                            }
                        }

                        sb.Append(" ");
                        if (workingPosition.ColorToMove == PieceColor.White)
                        {
                            workingPosition.MoveNumber++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (Configuration.DebugLevel != 0)
                    {
                        AppLog.Message("Exception in BuildMoveSequence(): " + ex.Message);
                        AppLog.LogPosition(workingPosition);
                        AppLog.Message("Move: " + debugMove);
                        AppLog.Message("Returned string: " + sb.ToString());
                    }

                    // indicate exception to the caller
                    sb.Clear();
                    sb.Append(Constants.EXCEPTION);
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
