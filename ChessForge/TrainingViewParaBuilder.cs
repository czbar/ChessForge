using ChessPosition;
using GameTree;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace ChessForge
{
    public partial class TrainingView
    {
        // Runs acting as buttons for changing training lines
        private Run _rPreviousLine = null;
        private Run _rNextLine = null;
        private Run _rRandomLine = null;


        /// <summary>
        /// Builds paragraphs for a training line that was not
        /// actually played but provides a starting point for a random line training.
        /// </summary>
        /// <param name="lstLine"></param>
        public void BuildTrainingLineParas(List<TreeNode> lstLine)
        {
            try
            {
                RemoveIntroParas();

                for (int i = 0; i < lstLine.Count; i++)
                {
                    TreeNode nd = lstLine[i];
                    EngineGame.AddPlyToGame(nd);

                    bool isUserMove = nd.ColorToMove != TrainingSession.ActualTrainingSide;
                    if (isUserMove)
                    {
                        // user move
                        _otherMovesInWorkbook.Clear();
                        foreach (TreeNode sibling in GetWorkbookNonNullLeafSiblings(nd))
                        {
                            _otherMovesInWorkbook.Add(sibling);
                        }
                        BuildMoveParagraph(nd, true);
                        InsertCommentIntoUserMovePara(true, nd, false);
                    }
                    else
                    {
                        // engine move
                        BuildMoveParagraph(nd, false);
                    }
                }

                if (lstLine.Count > 0)
                {
                    _mainWin.DisplayPosition(lstLine.Last());
                    BuildSecondPromptParagraph();
                }

                EnableChangeLineRuns();
                TrainingSession.SyncTrainingLine();
            }
            catch (System.Exception ex)
            {
                AppLog.Message("BuildTrainingLineParas()", ex);
            }
        }

        /// <summary>
        /// Builds a paragraph containing just one move with NAGs and Comments/Commands if any. 
        /// </summary>
        private void BuildMoveParagraph(TreeNode nd, bool userMove)
        {
            string paraName = _par_line_moves_ + nd.NodeId.ToString();
            string runName = _run_line_move_ + nd.NodeId.ToString();

            // check if already exists. Due to timing issues it may be called multiple times
            if (FindParagraphByName(HostRtb.Document, paraName, false) == null)
            {
                Paragraph para = AddNewParagraphToDoc(HostRtb.Document, STYLE_MOVES_MAIN, "");
                para.Name = paraName;

                Run r_prefix = new Run();
                if (userMove)
                {
                    r_prefix.FontWeight = FontWeights.Normal;
                    para.Inlines.Add(r_prefix);
                }
                else
                {
                    switch (_sourceType)
                    {
                        case GameData.ContentType.STUDY_TREE:
                            r_prefix.Text = Properties.Resources.TrnStudyResponse + ": ";
                            break;
                        case GameData.ContentType.MODEL_GAME:
                            r_prefix.Text = Properties.Resources.TrnGameResponse + ": ";
                            break;
                        case GameData.ContentType.EXERCISE:
                            r_prefix.Text = Properties.Resources.TrnExerciseResponse + ": ";
                            break;
                    }
                    r_prefix.FontWeight = FontWeights.Normal;
                    para.Inlines.Add(r_prefix);
                }

                Run r = CreateButtonRun(MoveUtils.BuildSingleMoveText(nd, true, true,
                    _moveNumberOffset) + " ",
                    runName,
                    ChessForgeColors.CurrentTheme.RtbForeground);
                para.Inlines.Add(r);

                if (!userMove)
                {
                    InsertCommentIntoWorkbookMovePara(para, nd);
                }

                _mainWin.UiRtbTrainingProgress.ScrollToEnd();
            }
        }

        /// <summary>
        /// Builds the paragraph prompting the user to make a move
        /// after the program responded.
        /// </summary>
        private void BuildSecondPromptParagraph()
        {
            TreeNode nd = EngineGame.GetLastGameNode();

            bool isMateCf = PositionUtils.IsCheckmate(nd.Position, out _);

            bool isStalemate = false;
            if (!isMateCf)
            {
                isStalemate = PositionUtils.IsStalemate(nd.Position);
            }

            bool isInsufficientMaterial = false;
            if (!isMateCf && !isStalemate)
            {
                isInsufficientMaterial = PositionUtils.IsInsufficientMaterial(nd.Position);
            }

            if (isMateCf)
            {
                BuildMoveParagraph(nd, false);
                BuildCheckmateParagraph(nd, false);
                HostRtb.Document.Blocks.Remove(_dictParas[ParaType.PROMPT_TO_MOVE]);
                _mainWin.BoardCommentBox.ReportCheckmate(false);
            }
            else if (isStalemate)
            {
                BuildMoveParagraph(nd, false);
                BuildStalemateParagraph(nd);
                HostRtb.Document.Blocks.Remove(_dictParas[ParaType.PROMPT_TO_MOVE]);
                _mainWin.BoardCommentBox.ReportStalemate();
            }
            else if (isInsufficientMaterial)
            {
                BuildMoveParagraph(nd, false);
                BuildInsufficientMaterialParagraph(nd);
                HostRtb.Document.Blocks.Remove(_dictParas[ParaType.PROMPT_TO_MOVE]);
                _mainWin.BoardCommentBox.ReportInsufficientMaterial();
            }
            else
            {
                if (nd.NodeId != TrainingSession.StartPosition.NodeId)
                {
                    BuildMoveParagraph(nd, false);
                }

                HostRtb.Document.Blocks.Remove(_dictParas[ParaType.PROMPT_TO_MOVE]);
                Paragraph para = AddNewParagraphToDoc(HostRtb.Document, STYLE_SECOND_PROMPT, "\n   " + Properties.Resources.YourTurn + "...");
                _dictParas[ParaType.PROMPT_TO_MOVE] = para;
                para.Foreground = ChessForgeColors.GetHintForeground(CommentBox.HintType.INFO);
                para.Name = _par_move_prompt_;

                _mainWin.BoardCommentBox.GameMoveMade(nd, false);
            }
            _mainWin.UiRtbTrainingProgress.ScrollToEnd();
            if (TrainingSession.IsContinuousEvaluation)
            {
                RequestMoveEvaluation(_mainWin.ActiveVariationTreeId, true);
            }
        }

        /// <summary>
        /// Adds plies from _otherMovesInWorkbook to the
        /// passed paragraph.
        /// </summary>
        /// <param name="para"></param>
        private void BuildOtherWorkbookMovesRun(Paragraph para, List<TreeNode> moves, bool isUserMove)
        {
            foreach (TreeNode nd in moves)
            {
                Brush brush = isUserMove ? _userBrush : _workbookBrush;
                para.Inlines.Add(CreateButtonRun(MoveUtils.BuildSingleMoveText(nd, true, true, _moveNumberOffset), _run_wb_move_ + nd.NodeId.ToString(), brush));
                Run r_semi = new Run("; ");
                para.Inlines.Add(r_semi);
            }
        }

        /// <summary>
        /// Rebuilds the Engine Game paragraph up to 
        /// a specified Node.
        /// </summary>
        /// <param name="toNode"></param>
        private void RebuildEngineGamePara(TreeNode toNode)
        {
            if (_paraCurrentEngineGame == null)
            {
                _paraCurrentEngineGame = AddNewParagraphToDoc(HostRtb.Document, STYLE_ENGINE_GAME, "");
            }

            Dictionary<int, Run> dictEvalRunsToKeep = new Dictionary<int, Run>();
            foreach (Inline inline in _paraCurrentEngineGame.Inlines)
            {
                if (inline is Run)
                {
                    Run r = inline as Run;
                    if (r.Name.StartsWith(_run_move_eval_))
                    {
                        int nodeId = TextUtils.GetIdFromPrefixedString(r.Name);
                        dictEvalRunsToKeep[nodeId] = r;
                    }
                }
            }
            _paraCurrentEngineGame.Inlines.Clear();
            _currentEngineGameMoveCount = 0;

            // in the EngineMoveReplacement scenario _engineGameRootNode.Children.Count can be zero
            // (if we are replacing the very first engine move)
            if (_engineGameRootNode.Children.Count > 0)
            {
                TreeNode nd = _engineGameRootNode.Children[0];

                // preserve note about the workbook line ending if there was one
                Run rWbEnded = GetWorkbookEndedRun();

                if (rWbEnded != null)
                {
                    _paraCurrentEngineGame.Inlines.Add(rWbEnded);
                }

                Brush moveColor = ChessForgeColors.CurrentTheme.TrainingEngineGameForeground;
                _paraCurrentEngineGame.Inlines.Add(new Run("\n" + Properties.Resources.TrnGameInProgress + "\n"));
                string text = "          " + MoveUtils.BuildSingleMoveText(nd, true, false, _moveNumberOffset) + " ";
                Run r_root = CreateButtonRun(text, _run_engine_game_move_ + nd.NodeId.ToString(), moveColor);
                _paraCurrentEngineGame.Inlines.Add(r_root);
                if (dictEvalRunsToKeep.ContainsKey(nd.NodeId))
                {
                    _paraCurrentEngineGame.Inlines.Add(dictEvalRunsToKeep[nd.NodeId]);
                }

                _currentEngineGameMoveCount++;

                while (nd.Children.Count > 0)
                {
                    nd = nd.Children[0];
                    _currentEngineGameMoveCount++;
                    text = MoveUtils.BuildSingleMoveText(nd, false, false, _moveNumberOffset) + " ";
                    Run gm = CreateButtonRun(text, _run_engine_game_move_ + nd.NodeId.ToString(), moveColor);
                    _paraCurrentEngineGame.Inlines.Add(gm);
                    if (dictEvalRunsToKeep.ContainsKey(nd.NodeId))
                    {
                        _paraCurrentEngineGame.Inlines.Add(dictEvalRunsToKeep[nd.NodeId]);
                    }
                }
            }
        }

        /// <summary>
        /// Adds a node/ply to the Engine Game paragraph.
        /// </summary>
        /// <param name="nd"></param>
        private void AddMoveToEngineGamePara(TreeNode nd, bool isUserMove)
        {
            if (_paraCurrentEngineGame == null)
            {
                // should never be null here so this is just defensive
                _paraCurrentEngineGame = AddNewParagraphToDoc(HostRtb.Document, STYLE_ENGINE_GAME, "");
            }

            string text = "";
            if (_currentEngineGameMoveCount == 0)
            {
                // preserve note about the workbook line ending if there was one
                Run rWbEnded = GetWorkbookEndedRun();
                _paraCurrentEngineGame.Inlines.Clear();
                if (rWbEnded != null)
                {
                    _paraCurrentEngineGame.Inlines.Add(rWbEnded);
                }
                _paraCurrentEngineGame.Inlines.Add(new Run("\n" + Properties.Resources.TrnGameInProgress + "\n"));
                text = "          " + MoveUtils.BuildSingleMoveText(nd, true, false, _moveNumberOffset) + " ";
            }
            else
            {
                text = MoveUtils.BuildSingleMoveText(nd, false, false, _moveNumberOffset) + " ";
            }

            Brush moveColor = ChessForgeColors.CurrentTheme.TrainingEngineGameForeground;
            Run gm = CreateButtonRun(text, _run_engine_game_move_ + nd.NodeId.ToString(), moveColor);
            _paraCurrentEngineGame.Inlines.Add(gm);

            HostRtb.Document.Blocks.Remove(_dictParas[ParaType.PROMPT_TO_MOVE]);
            _dictParas[ParaType.PROMPT_TO_MOVE] = null;

            if (nd.Position.IsCheckmate)
            {
                BuildCheckmateParagraph(nd, isUserMove);
            }
            else if (nd.Position.IsStalemate)
            {
                BuildStalemateParagraph(nd);
            }
            else if (nd.Position.IsInsufficientMaterial)
            {
                BuildInsufficientMaterialParagraph(nd);
            }
            else
            {
                if (nd.ColorToMove == TrainingSession.ActualTrainingSide)
                {
                    Paragraph para = AddNewParagraphToDoc(HostRtb.Document, STYLE_SECOND_PROMPT, "\n   " + Properties.Resources.YourTurn + "...");
                    _dictParas[ParaType.PROMPT_TO_MOVE] = para;
                    para.Name = _par_move_prompt_;
                    para.Foreground = ChessForgeColors.GetHintForeground(CommentBox.HintType.INFO);
                }
                else
                {
                    Paragraph para = AddNewParagraphToDoc(HostRtb.Document, STYLE_SECOND_PROMPT, "\n   " + Properties.Resources.WaitForEngineResponse);
                    _dictParas[ParaType.PROMPT_TO_MOVE] = para;
                    para.Name = _par_move_prompt_;
                    para.Foreground = ChessForgeColors.GetHintForeground(CommentBox.HintType.INFO);
                }
            }

            _currentEngineGameMoveCount++;

        }

        /// <summary>
        /// Inserts a comment run into the user move paragraph.
        /// </summary>
        /// <param name="isWorkbookMove"></param>
        private void InsertCommentIntoUserMovePara(bool isWorkbookMove, TreeNode userMove, bool showCheckMark = true)
        {
            Paragraph para = FindUserMoveParagraph(userMove);
            if (para != null)
            {
                string wbAlignmentRunName = _run_user_wb_alignment_ + userMove.NodeId.ToString();

                // do not build if already built
                if (FindRunByName(wbAlignmentRunName, para) == null)
                {
                    para.FontWeight = FontWeights.Normal;

                    if (showCheckMark && (isWorkbookMove || userMove.Parent.Children.Count > 1))
                    {
                        InsertCheckmarkRun(para, isWorkbookMove);
                    }
                    InsertWorkbookCommentRun(para, userMove);

                    Run wbAlignmentNoteRun = new Run();
                    wbAlignmentNoteRun.Name = wbAlignmentRunName;
                    wbAlignmentNoteRun.FontSize = para.FontSize - 1;

                    StringBuilder sbAlignmentNote = new StringBuilder();
                    if (_otherMovesInWorkbook.Count == 0)
                    {
                        if (!isWorkbookMove)
                        {
                            // if the parent has only this move as a child, we already announced end-of-training-line on previous move
                            // unless this is the very first training move
                            if (userMove.Parent.Children.Count > 1 || userMove.Parent == TrainingSession.StartPosition)
                            {
                                sbAlignmentNote.Append(Properties.Resources.TrnLineEnded + ". ");
                                SoundPlayer.PlayTrainingSound(SoundPlayer.Sound.END_OF_LINE);
                            }
                        }
                        wbAlignmentNoteRun.Text = sbAlignmentNote.ToString();
                        para.Inlines.Add(wbAlignmentNoteRun);
                    }
                    else
                    {
                        if (!isWorkbookMove)
                        {
                            SoundPlayer.PlayTrainingSound(SoundPlayer.Sound.NOT_IN_WORKBOOK);
                            TrainingSession.IsTakebackAvailable = true;

                            BuildTakebackParagraph();

                            string note = "";
                            switch (_sourceType)
                            {
                                case GameData.ContentType.STUDY_TREE:
                                    note = Properties.Resources.TrnStudyMoveNotInSource;
                                    break;
                                case GameData.ContentType.MODEL_GAME:
                                    note = Properties.Resources.TrnGameMoveNotInSource;
                                    break;
                                case GameData.ContentType.EXERCISE:
                                    note = Properties.Resources.TrnExerciseMoveNotInSource;
                                    break;
                            }
                            sbAlignmentNote.Append(note + ". ");
                        }

                        Run rAlternativeNote = null;
                        if (!isWorkbookMove)
                        {
                            string note = "";
                            bool single = _otherMovesInWorkbook.Count == 1;
                            switch (_sourceType)
                            {
                                case GameData.ContentType.STUDY_TREE:
                                    note = single ? Properties.Resources.TrnStudyOnlyMove : Properties.Resources.TrnStudySourceMoves;
                                    break;
                                case GameData.ContentType.MODEL_GAME:
                                    note = single ? Properties.Resources.TrnGameOnlyMove : Properties.Resources.TrnGameSourceMoves;
                                    break;
                                case GameData.ContentType.EXERCISE:
                                    note = single ? Properties.Resources.TrnExerciseOnlyMove : Properties.Resources.TrnExerciseSourceMoves;
                                    break;
                            }
                            sbAlignmentNote.Append(note + " ");
                        }
                        else
                        {
                            rAlternativeNote = new Run();
                            if (_otherMovesInWorkbook.Count == 1)
                            {
                                rAlternativeNote.Text = "  " + Properties.Resources.TrnAlternativeIs + " ";
                            }
                            else
                            {
                                rAlternativeNote.Text = "  " + Properties.Resources.TrnAlternativesAre + " ";
                            }
                            rAlternativeNote.FontSize = para.FontSize - 1;
                            rAlternativeNote.Foreground = _userBrush;
                        }

                        wbAlignmentNoteRun.Text = sbAlignmentNote.ToString();
                        para.Inlines.Add(wbAlignmentNoteRun);

                        if (rAlternativeNote != null)
                        {
                            para.Inlines.Add(rAlternativeNote);
                        }

                        BuildOtherWorkbookMovesRun(para, _otherMovesInWorkbook, true);
                    }
                    _mainWin.UiRtbTrainingProgress.ScrollToEnd();
                }
            }
        }

        /// <summary>
        /// Inserts a comment run into the Workbook move paragraph.
        /// It will include any comment found in the workbook and clickable
        /// other workbook moves
        /// </summary>
        /// <param name="moveNode"></param>
        private void InsertCommentIntoWorkbookMovePara(Paragraph para, TreeNode moveNode)
        {
            string wbAlternativesRunName = _run_wb_alternatives_ + moveNode.NodeId.ToString();
            string wbCommentRunName = _run_wb_comment_ + moveNode.NodeId.ToString();

            // do not build if already built
            if (FindRunByName(wbAlternativesRunName, para) != null)
            {
                return;
            }

            InsertWorkbookCommentRun(para, moveNode);

            para.FontWeight = FontWeights.Normal;

            Run wbAlternativesRun = new Run();
            wbAlternativesRun.FontSize = para.FontSize - 1;
            wbAlternativesRun.Name = wbAlternativesRunName;
            wbAlternativesRun.Foreground = _workbookBrush;

            StringBuilder sbWbAlternatives = new StringBuilder();

            List<TreeNode> lstAlternatives = GetWorkbookNonNullLeafSiblings(moveNode);

            if (lstAlternatives.Count == 0)
            {
                //wbAlternativesRun.Text = sbWbAlternatives.ToString();
                //para.Inlines.Add(wbAlternativesRun);
            }
            else
            {
                if (lstAlternatives.Count == 1)
                {
                    sbWbAlternatives.Append("  " + Properties.Resources.TrnAlternative + ": ");
                }
                else
                {
                    sbWbAlternatives.Append("  " + Properties.Resources.TrnAlternatives + ": ");
                }

                wbAlternativesRun.Text += sbWbAlternatives;
                para.Inlines.Add(wbAlternativesRun);

                BuildOtherWorkbookMovesRun(para, lstAlternatives, false);
            }

            StringBuilder sbAlignmentNote = new StringBuilder();
            if (moveNode.Children.Count == 0)
            {
                Run wbAlignmentNoteRun = new Run();
                string wbAlignmentRunName = _run_wb_response_alignment_ + moveNode.NodeId.ToString();
                wbAlignmentNoteRun.Name = wbAlignmentRunName;
                wbAlignmentNoteRun.FontSize = para.FontSize - 1;

                sbAlignmentNote.Append(Properties.Resources.TrnLineEnded + ". ");
                SoundPlayer.PlayTrainingSound(SoundPlayer.Sound.END_OF_LINE);
                wbAlignmentNoteRun.Text = sbAlignmentNote.ToString();
                para.Inlines.Add(wbAlignmentNoteRun);
            }

            _mainWin.UiRtbTrainingProgress.ScrollToEnd();
        }

        /// <summary>
        /// Builds and inserts a run with a comment from the Workbook if present.
        /// </summary>
        /// <param name="para"></param>
        /// <param name="moveNode"></param>
        private void InsertWorkbookCommentRun(Paragraph para, TreeNode moveNode)
        {
            if (string.IsNullOrWhiteSpace(moveNode.Comment))
            {
                return;
            }

            Run r = new Run("[" + moveNode.Comment + "] ");
            r.FontSize = para.FontSize - 1;
            r.FontStyle = FontStyles.Italic;
            para.Inlines.Add(r);
        }

        /// <summary>
        /// Inserts check mark indicating whether the move was in the Workbook or not.
        /// </summary>
        /// <param name="para"></param>
        /// <param name="isWorkbookMove"></param>
        private void InsertCheckmarkRun(Paragraph para, bool isWorkbookMove)
        {
            Run r = new Run((isWorkbookMove ? Constants.CharCheckMark : Constants.CharCrossMark) + " ");
            para.Inlines.Add(r);
        }

        /// <summary>
        /// Builds a paragraph reporting stalemate
        /// </summary>
        /// <param name="nd"></param>
        private void BuildStalemateParagraph(TreeNode nd)
        {
            string paraName = _par_stalemate_;

            Paragraph para = AddNewParagraphToDoc(HostRtb.Document, STYLE_CHECKMATE, "");
            para.Foreground = ChessForgeColors.CurrentTheme.TrainingCheckmateForeground;
            para.Name = paraName;

            Run r_prefix = new Run();
            r_prefix.Text = "\n" + Properties.Resources.TrnGameStalemate;

            para.Inlines.Add(r_prefix);
            _mainWin.UiRtbTrainingProgress.ScrollToEnd();
        }

        /// <summary>
        /// Builds a paragraph reporting insufficient material.
        /// </summary>
        /// <param name="nd"></param>
        private void BuildInsufficientMaterialParagraph(TreeNode nd)
        {
            string paraName = _par_insufficientmaterial_;

            Paragraph para = AddNewParagraphToDoc(HostRtb.Document, STYLE_CHECKMATE, "");
            para.Foreground = ChessForgeColors.CurrentTheme.TrainingCheckmateForeground;
            para.Name = paraName;

            Run r_prefix = new Run();
            r_prefix.Text = "\n" + Properties.Resources.TrnGameInsufficientMaterial;

            para.Inlines.Add(r_prefix);
            _mainWin.UiRtbTrainingProgress.ScrollToEnd();
        }


        /// <summary>
        /// Initial prompt to advise the user make their move.
        /// This paragraph is removed later on to reduce clutter.
        /// </summary>
        private void BuildInstructionsText()
        {
            StringBuilder sbInstruction = new StringBuilder();
            if (TrainingSession.ActualTrainingSide == PieceColor.White)
            {
                sbInstruction.Append(Properties.Resources.TrnUserPlaysWhite);
            }
            else
            {
                sbInstruction.Append(Properties.Resources.TrnUserPlaysBlack);
            }

            sbInstruction.AppendLine("");
            sbInstruction.AppendLine("");

            sbInstruction.AppendLine(Properties.Resources.TrnClickMoveBelow);
            sbInstruction.AppendLine(Properties.Resources.TrnRightClickMove);
            sbInstruction.AppendLine("");

            if (TrainingSession.IsRandomLinesMode)
            {
                sbInstruction.Append(Properties.Resources.TrnSelectNextRandomLine + " ");
            }
            else
            {
                sbInstruction.AppendLine(Properties.Resources.TrnChangeLineInfo);
            }

            Paragraph para = AddNewParagraphToDoc(HostRtb.Document, STYLE_INTRO, sbInstruction.ToString());
            _dictParas[ParaType.INSTRUCTIONS] = para;

            SolidColorBrush brush = ChessForgeColors.GetHintForeground(CommentBox.HintType.PROGRESS);

            if (TrainingSession.IsRandomLinesMode)
            {
                _rNextLine = null;
                _rPreviousLine = null;

                _rRandomLine = BuildChangeLineRun(Properties.Resources.TrnNextRandomLine + ".\n", brush);
                _rRandomLine.MouseDown += EventRandomLineClicked;
                para.Inlines.Add(_rRandomLine);
            }
            else
            {
                para.Inlines.Add(new Run("  "));

                _rNextLine = BuildChangeLineRun(Properties.Resources.TrnNextLine, brush);
                _rNextLine.MouseDown += EventNextLineClicked;
                para.Inlines.Add(_rNextLine);

                para.Inlines.Add(new Run("  "));

                _rPreviousLine = BuildChangeLineRun(Properties.Resources.TrnPreviousLine, brush);
                _rPreviousLine.MouseDown += EventPreviousLineClicked;
                para.Inlines.Add(_rPreviousLine);

                para.Inlines.Add(new Run("  "));

                _rRandomLine = BuildChangeLineRun(Properties.Resources.TrnRandomLine, brush);
                _rRandomLine.MouseDown += EventRandomLineClicked;
                para.Inlines.Add(_rRandomLine);

                para.Inlines.Add(new LineBreak());

                Paragraph paraPrompt = AddNewParagraphToDoc(HostRtb.Document, STYLE_FIRST_PROMPT, Properties.Resources.TrnMakeFirstMove);
                _dictParas[ParaType.PROMPT_TO_MOVE] = paraPrompt;
                paraPrompt.Foreground = ChessForgeColors.GetHintForeground(CommentBox.HintType.INFO);
                paraPrompt.Name = _par_move_prompt_;
            }

            EnableChangeLineRuns();
        }

        /// <summary>
        /// Builds a run for one of the change line options
        /// </summary>
        /// <param name="text"></param>
        /// <param name="brush"></param>
        /// <returns></returns>
        private Run BuildChangeLineRun(string text, SolidColorBrush brush)
        {
            Run run = new Run(text);
            run.Foreground = brush;
            run.TextDecorations = TextDecorations.Underline;
            run.Cursor = Cursors.Hand;

            return run;
        }

        /// <summary>
        /// Enables/disables Runs representing clicks to change lines.
        /// </summary>
        private void EnableChangeLineRuns()
        {
            bool enableNextLine = AppState.GetTrainingLineMenuItemStatus(true, out string nextMoveTxt);
            bool enablePrevLine = AppState.GetTrainingLineMenuItemStatus(false, out string prevMoveTxt);

            bool hasRandomLine = TrainingSession.HasRandomLines();

            if (_rNextLine != null)
            {
                _rNextLine.Foreground = enableNextLine ? 
                    ChessForgeColors.GetHintForeground(CommentBox.HintType.PROGRESS) :
                    ChessForgeColors.CurrentTheme.DisabledItemForeground;
                _rNextLine.Cursor = enableNextLine ? Cursors.Hand : Cursors.Arrow;
                _rNextLine.Name = enableNextLine ? _run_change_line_ : _run_disabled_;

                _rNextLine.Text = Properties.Resources.TrnNextLine;
                if (enableNextLine)
                {
                    _rNextLine.Text += "  (" + nextMoveTxt + ")";
                }
            }

            if (_rPreviousLine != null)
            {
                _rPreviousLine.Foreground = enablePrevLine ?
                    ChessForgeColors.GetHintForeground(CommentBox.HintType.PROGRESS) :
                    ChessForgeColors.CurrentTheme.DisabledItemForeground;
                _rPreviousLine.Cursor = enablePrevLine ? Cursors.Hand : Cursors.Arrow;
                _rPreviousLine.Name = enablePrevLine ? _run_change_line_ : _run_disabled_;

                _rPreviousLine.Text = Properties.Resources.TrnPreviousLine;
                if (enablePrevLine)
                {
                    _rPreviousLine.Text += "  (" + prevMoveTxt + ")";
                }
            }

            if (_rRandomLine != null)
            {
                _rRandomLine.Foreground = hasRandomLine ?
                    ChessForgeColors.GetHintForeground(CommentBox.HintType.PROGRESS) :
                    ChessForgeColors.CurrentTheme.DisabledItemForeground;
                _rRandomLine.Cursor = hasRandomLine ? Cursors.Hand : Cursors.Arrow;
                _rRandomLine.Name = hasRandomLine ? _run_change_line_ : _run_disabled_;
            }
        }

        /// <summary>
        /// Builds the "stem line" paragraphs that is always visible at the top
        /// of the view.
        /// </summary>
        /// <param name="node"></param>
        private void CreateStemParagraph(TreeNode node)
        {
            _dictParas[ParaType.STEM] = AddNewParagraphToDoc(HostRtb.Document, STYLE_STEM_LINE, null);

            string sPrefix;
            if (node.NodeId != 0)
            {
                sPrefix = "\n" + Properties.Resources.TrnSessionStartsAfter + " \n";
            }
            else
            {
                sPrefix = "\n" + Properties.Resources.TrnSessionStart + " \n";
            }
            Run r_prefix = new Run(sPrefix);

            r_prefix.FontWeight = FontWeights.Normal;
            _dictParas[ParaType.STEM].Inlines.Add(r_prefix);

            if (node.NodeId != 0)
            {
                InsertPrefixRuns(_dictParas[ParaType.STEM], node);
            }
        }

        /// <summary>
        /// Builds a paragraph reporting checkmate
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="userMove"></param>
        private void BuildCheckmateParagraph(TreeNode nd, bool userMove)
        {
            string paraName = _par_checkmate_;

            Paragraph para = AddNewParagraphToDoc(HostRtb.Document, STYLE_CHECKMATE, "");
            para.Foreground = ChessForgeColors.CurrentTheme.TrainingCheckmateForeground;
            para.Name = paraName;

            Run r_prefix = new Run();
            if (userMove)
            {
                r_prefix.Text = "\n" + Properties.Resources.TrnUserCheckmatedEngine;
            }
            else
            {
                r_prefix.Text = "\n" + Properties.Resources.TrnEngineCheckmatedUser;
            }

            para.Inlines.Add(r_prefix);
            _mainWin.UiRtbTrainingProgress.ScrollToEnd();
        }

        /// <summary>
        /// Adds a Paragraph with a Run to click if the user wants to take their move back.
        /// </summary>
        private void BuildTakebackParagraph()
        {
            // first check if exsists
            _dictParas.TryGetValue(ParaType.TAKEBACK, out Paragraph para);
            if (para == null)
            {
                para = AddNewParagraphToDoc(HostRtb.Document, STYLE_TAKEBACK, "");
                _dictParas[ParaType.TAKEBACK] = para;
            }

            para.Inlines.Clear();

            para.Foreground = ChessForgeColors.CurrentTheme.TrainingTakebackForeground;

            Run rTakeback = new Run("\n " + Properties.Resources.MsgTakebackWanted);
            rTakeback.MouseDown += EventTakebackParaClicked;
            rTakeback.Cursor = Cursors.Hand;
            para.Inlines.Add(rTakeback);

            Run note = new Run();
            note.FontSize = para.FontSize - 2;
            note.FontStyle = FontStyles.Italic;
            note.FontWeight = FontWeights.Normal;
            note.Foreground = ChessForgeColors.CurrentTheme.RtbForeground;
            note.MouseDown += EventTakebackParaClicked;
            note.Cursor = Cursors.Hand;

            note.Text = "  " + Properties.Resources.MsgTakebackInfo;
            para.Inlines.Add(note);
        }

        /// <summary>
        /// Removes takeback paragraph if exists
        /// </summary>
        private void RemoveTakebackParagraph()
        {
            if (_dictParas[ParaType.TAKEBACK] != null)
            {
                HostRtb.Document.Blocks.Remove(_dictParas[ParaType.TAKEBACK]);
                _dictParas[ParaType.TAKEBACK] = null;
            }
        }

        /// <summary>
        /// Removes the paragraph for the ply
        /// with the move number and color-to-move same
        /// as in the passed Node.
        /// </summary>
        /// <param name="move"></param>
        private void RemoveParagraphsFromMove(TreeNode move)
        {
            List<Block> parasToRemove = new List<Block>();

            // there is a special case where we are going back to the _startingNode in which case
            // we need to remove all paras after INSTRUCTIONS (we will not find a separate para for this move)
            bool isStartingNode = move.NodeId == TrainingSession.StartPosition.NodeId;
            bool found = false;
            foreach (var block in HostRtb.Document.Blocks)
            {
                if (found)
                {
                    parasToRemove.Add(block);
                }
                else if (block is Paragraph)
                {
                    if (isStartingNode)
                    {
                        if (block == _dictParas[ParaType.INSTRUCTIONS])
                        {
                            found = true;
                        }
                    }
                    else
                    {
                        int nodeId = TextUtils.GetIdFromPrefixedString(((Paragraph)block).Name);
                        TreeNode nd = _mainWin.ActiveVariationTree.GetNodeFromNodeId(nodeId);
                        if (nd != null && nd.MoveNumber == move.MoveNumber && nd.ColorToMove == move.ColorToMove)
                        {
                            found = true;
                            parasToRemove.Add(block);
                        }
                    }
                }
            }

            foreach (var block in parasToRemove)
            {
                HostRtb.Document.Blocks.Remove(block);
            }

            // if anything was removed then the position was indeed rolled back
            if (parasToRemove.Count > 0)
            {
                // remove a possible checkmate/stalemate paragraph if exits
                RemoveCheckmatePara();
                RemoveStalematePara();
                RemoveInsufficientMaterialPara();
            }
        }

        /// <summary>
        /// Removes a checkmate para if exists.
        /// </summary>
        private void RemoveCheckmatePara()
        {
            Paragraph paraToRemove = null;

            foreach (var block in HostRtb.Document.Blocks)
            {
                if (block is Paragraph)
                {
                    if (((Paragraph)block).Name == _par_checkmate_)
                    {
                        paraToRemove = block as Paragraph;
                        break;
                    }
                }
            }

            if (paraToRemove != null)
            {
                HostRtb.Document.Blocks.Remove(paraToRemove);
            }
        }

        /// <summary>
        /// Removes a stalemate para if exists.
        /// </summary>
        private void RemoveStalematePara()
        {
            Paragraph paraToRemove = null;

            foreach (var block in HostRtb.Document.Blocks)
            {
                if (block is Paragraph)
                {
                    if (((Paragraph)block).Name == _par_stalemate_)
                    {
                        paraToRemove = block as Paragraph;
                        break;
                    }
                }
            }

            if (paraToRemove != null)
            {
                HostRtb.Document.Blocks.Remove(paraToRemove);
            }
        }

        /// <summary>
        /// Removes a insufficient material para if exists.
        /// </summary>
        private void RemoveInsufficientMaterialPara()
        {
            Paragraph paraToRemove = null;

            foreach (var block in HostRtb.Document.Blocks)
            {
                if (block is Paragraph)
                {
                    if (((Paragraph)block).Name == _par_insufficientmaterial_)
                    {
                        paraToRemove = block as Paragraph;
                        break;
                    }
                }
            }

            if (paraToRemove != null)
            {
                HostRtb.Document.Blocks.Remove(paraToRemove);
            }
        }

    }
}
