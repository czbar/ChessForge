using ChessPosition;
using ChessPosition.Utils;
using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;

namespace ChessForge
{
    /// <summary>
    /// Manages text and events in the main Workbook view.
    /// The view is built in a RichTextBox.
    /// </summary>
    public partial class VariationTreeView : RichTextBuilder
    {
        /// <summary>
        /// If the Comment run for the passed node already exists, it will be updated.
        /// If it does not exist, it will be created.
        /// This also updates the run's text itself and move NAGs may have changed
        /// before this method was called.
        /// </summary>
        /// <param name="nd"></param>
        public Inline InsertOrUpdateCommentRun(TreeNode nd)
        {
            Inline inlComment;

            if (nd == null)
            {
                return null;
            }

            try
            {
                Run r;
                _dictNodeToRun.TryGetValue(nd.NodeId, out r);

                if (r == null)
                {
                    // something seriously wrong
                    AppLog.Message("ERROR: InsertOrUpdateCommentRun()- Run " + nd.NodeId.ToString() + " not found in _dictNodeToRun");
                    return null;
                }

                // we are refreshing the move's text in case we have a change in NAG,
                UpdateRunText(r, nd, IsMoveTextWithNumber(r.Text));

                _dictNodeToCommentRun.TryGetValue(nd.NodeId, out inlComment);

                if (!IsCommentRunToShow(nd))
                {
                    // if the comment run existed, remove it
                    if (inlComment != null)
                    {
                        _dictNodeToCommentRun.Remove(nd.NodeId);
                        RemoveCommentRunsFromHostingParagraph(inlComment);
                    }
                }
                else
                {
                    _dictNodeToCommentRun.Remove(nd.NodeId);
                    RemoveCommentRunsFromHostingParagraph(inlComment);

                    Paragraph para = r.Parent as Paragraph;
                    AddCommentRunsToParagraph(nd, para, out bool isBlunder);
                    if (isBlunder)
                    {
                        TextUtils.RemoveBlunderNagFromText(r);
                    }
                }

                // the next move in para may need to be redrawn if it was black on move
                if (nd.ColorToMove == PieceColor.Black)
                {
                    UpdateNextMoveText(r, nd);
                }
            }
            catch
            {
                inlComment = null;
            }

            return inlComment;
        }

        /// <summary>
        /// If the Comment Before Run for the passed node already exists, it will be updated.
        /// If it does not exist, it will be created.
        /// </summary>
        /// <param name="nd"></param>
        public Inline InsertOrUpdateCommentBeforeMoveRun(TreeNode nd, bool? includeNumber = null)
        {
            Inline inlCommentBeforeMove;

            if (nd == null)
            {
                return null;
            }

            try
            {
                Run rMove;
                _dictNodeToRun.TryGetValue(nd.NodeId, out rMove);

                if (rMove == null)
                {
                    // something seriously wrong
                    AppLog.Message("ERROR: InsertOrUpdateCommentBeforeRun()- Run " + nd.NodeId.ToString() + " not found in _dictNodeToRun");
                    return null;
                }

                _dictNodeToCommentBeforeMoveRun.TryGetValue(nd.NodeId, out inlCommentBeforeMove);

                if (string.IsNullOrEmpty(nd.CommentBeforeMove))
                {
                    // if the comment run existed, remove it
                    if (inlCommentBeforeMove != null)
                    {
                        _dictNodeToCommentBeforeMoveRun.Remove(nd.NodeId);
                        RemoveRunFromHostingParagraph(inlCommentBeforeMove);
                    }
                }
                else
                {
                    _dictNodeToCommentBeforeMoveRun.Remove(nd.NodeId);
                    RemoveRunFromHostingParagraph(inlCommentBeforeMove);

                    Paragraph para = rMove.Parent as Paragraph;
                    AddCommentBeforeMoveRunToParagraph(nd, para);
                }

                // if the passed includeNumber was true, do not question it (it is part of first render)
                // otherwise, refresh the move's text if it is black's move as we may need a number in front.
                if (includeNumber != true && nd.ColorToMove == PieceColor.White)
                {
                    // we need the number if this is the first run in the paragraph or previous move has a Comment
                    // or this move has a CommentBeforeMove
                    bool includeNo = RichTextBoxUtilities.IsFirstNonEmptyRunInPara(rMove, rMove.Parent as Paragraph)
                                     || !string.IsNullOrWhiteSpace(nd.CommentBeforeMove)
                                     || (nd.Parent != null && (!string.IsNullOrEmpty(nd.Parent.Comment) || nd != nd.Parent.Children[0]));
                    UpdateRunText(rMove, nd, includeNo);
                }
            }
            catch
            {
                inlCommentBeforeMove = null;
            }

            return inlCommentBeforeMove;
        }

        /// <summary>
        /// Gets the next Run and its TreeNode and changes its text if the
        /// passed run has a textual comment.
        /// This is to inssert or remove the move number on the black move
        /// depending on whether there is or isn't a comment in the current move.
        /// </summary>
        /// <param name="run"></param>
        protected void UpdateNextMoveText(Run currRun, TreeNode currNode)
        {
            Run nextMoveRun = GetNextMoveRunInPara(currRun);
            if (nextMoveRun != null)
            {
                int nodeId = TextUtils.GetIdFromPrefixedString(nextMoveRun.Name);
                TreeNode nextNode = ShownVariationTree.GetNodeFromNodeId(nodeId);
                if (nextNode != null)
                {
                    // take care of the special case where node 0 may have a comment
                    bool includeNumber = currNode.NodeId == 0 || !string.IsNullOrWhiteSpace(currNode.Comment) || !string.IsNullOrEmpty(nextNode.CommentBeforeMove);
                    UpdateRunText(nextMoveRun, nextNode, includeNumber);
                }
            }
        }

        /// <summary>
        /// Updates text of the move run.
        /// </summary>
        /// <param name="run"></param>
        /// <param name="node"></param>
        /// <param name="includeNumber"></param>
        protected void UpdateRunText(Run run, TreeNode node, bool includeNumber)
        {
            // be sure to keep any leading spaces
            string spaces = TextUtils.GetLeadingSpaces(run.Text);

            run.Text = BuildNodeText(node, includeNumber);
            run.Text = spaces + run.Text.TrimStart();
        }

        /// <summary>
        /// Finds the Run follwoing the passed Run
        /// in its parent paragraph.
        /// </summary>
        /// <param name="currRun"></param>
        /// <returns></returns>
        protected Run GetNextMoveRunInPara(Run currRun)
        {
            Run nextRun = null;

            if (currRun != null)
            {
                Paragraph para = currRun.Parent as Paragraph;
                if ((para != null))
                {
                    bool runFound = false;
                    foreach (Inline inline in para.Inlines)
                    {
                        if (inline == currRun)
                        {
                            runFound = true;
                        }
                        else if (runFound)
                        {
                            if (inline is Run r)
                            {
                                string[] tokens = r.Name.Split('_');

                                // has to be exactly one '_' so we don't get 'run_coment_'!
                                if (tokens.Length == 2 && (tokens[0] + "_") == _run_)
                                {
                                    nextRun = (Run)inline;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            return nextRun;
        }

        /// <summary>
        /// Inserts a position diagram (if flagged on the node in the paragraph).  
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="para"></param>
        private bool AddDiagramToParagraph(TreeNode nd, Paragraph para)
        {
            bool diagramInserted = false;

            if (nd.IsDiagram)
            {
                InlineUIContainer iuc = VariationTreeViewDiagram.CreateDiagram(nd, out ChessBoardSmall chessboard, IsLargeDiagram(nd));
                if (iuc != null)
                {
                    Run preDiagRun = new Run("\n");
                    preDiagRun.Name = RichTextBoxUtilities.PreInlineDiagramRunPrefix + nd.NodeId.ToString();
                    para.Inlines.Add(preDiagRun);

                    para.Inlines.Add(iuc);
                    iuc.MouseDown += EventRunClicked;

                    // TODO: the following needs resolving in some other way.
                    // e.g. perhaps always add the post diag run and remove in post-processing
                    // if it is found to be the last run in a paragraph.

                    // if there are moves further in the same para, insert a new line.
                    if (nd.Children.Count <= 1 || nd.Parent.Children.Count <= 1) // || nd.Parent.Children[nd.Parent.Children.Count - 1] != nd)
                    {
                        Run postDiagRun = new Run("\n");
                        postDiagRun.Name = RichTextBoxUtilities.PostInlineDiagramRunPrefix + nd.NodeId.ToString();
                        para.Inlines.Add(postDiagRun);
                    }

                    chessboard.DisplayPosition(nd, false);
                    diagramInserted = true;
                }
            }

            return diagramInserted;
        }

        /// <summary>
        /// Creates the comment Run or Runs if there is a comment with the move.
        /// Adds the runs to the paragraph.
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="para"></param>
        private void AddCommentRunsToParagraph(TreeNode nd, Paragraph para, out bool isAssessmentBlunderShown)
        {
            isAssessmentBlunderShown = false;

            if (!IsCommentRunToShow(nd))
            {
                return;
            }

            try
            {
                List<CommentPart> parts = CommentProcessor.SplitCommentTextAtUrls(nd.Comment);
                if (nd.QuizPoints != 0)
                {
                    parts.Add(new CommentPart(CommentPartType.QUIZ_POINTS, " *" + Properties.Resources.QuizPoints + ": " + nd.QuizPoints.ToString() + "* "));
                }

                // only add the thumbnail part if we are not printing/exporting
                if (nd.IsThumbnail && !_isPrinting)
                {
                    CommentPart thumb = new CommentPart(CommentPartType.THUMBNAIL_SYMBOL, "");
                    parts.Insert(0, thumb);
                }

                if (!string.IsNullOrEmpty(nd.ArticleRefs))
                {
                    List<Article> articles = GuiUtilities.BuildReferencedArticlesList(nd);
                    bool first = true;
                    foreach (Article article in articles)
                    {
                        string title;
                        if (article.ContentType == GameData.ContentType.STUDY_TREE)
                        {
                            title = Properties.Resources.Chapter + " " + article.Tree.Header.GetChapterTitle();
                        }
                        else
                        {
                            title = article.Tree.Header.BuildGameReferenceTitle();
                        }
                        if (!first)
                        {
                            title = "; " + title;
                        }
                        parts.Add(new CommentPart(CommentPartType.REFERENCE, " " + title, article.Guid));
                        first = false;
                    }
                }

                // check only here as we may have quiz points
                if (parts == null)
                {
                    return;
                }

                CommentPart startPart = new CommentPart(CommentPartType.TEXT, " ");
                parts.Insert(0, startPart);

                CommentPart endPart = new CommentPart(CommentPartType.TEXT, " ");
                parts.Add(endPart);

                if (HandleBlunders && nd.Assessment != (uint)ChfCommands.Assessment.NONE && nd.IsMainLine())
                {
                    // if we only have start and end parts so far, delete them.
                    // We don't need brackets if all we have ASSESSMENT
                    if (parts.Count == 2)
                    {
                        parts.Clear();
                        // but we need a space after assessment
                        //parts.Add(new CommentPart(CommentPartType.TEXT, " "));
                    }

                    CommentPart ass = new CommentPart(CommentPartType.ASSESSMENT, "");
                    parts.Insert(0, ass);
                }


                Inline inlPrevious = null;
                for (int i = 0; i < parts.Count; i++)
                {
                    CommentPart part = parts[i];
                    if (part.Type == CommentPartType.ASSESSMENT)
                    {
                        isAssessmentBlunderShown = true;
                    }

                    Inline inl = CreateInlineForCommentPart(part, nd, i, parts.Count);

                    // the above may or may not have set the name.
                    if (string.IsNullOrEmpty(inl.Name))
                    {
                        inl.Name = _run_comment_ + nd.NodeId.ToString();
                    }

                    if (inlPrevious == null)
                    {
                        // insert after the reference run or immediately after the move run if no reference run
                        Run rNode;
                        rNode = _dictNodeToRun[nd.NodeId];
                        para.Inlines.InsertAfter(rNode, inl);

                        _dictNodeToCommentRun[nd.NodeId] = inl;
                        _dictCommentRunToParagraph[inl] = para;
                    }
                    else
                    {
                        para.Inlines.InsertAfter(inlPrevious, inl);
                    }

                    inlPrevious = inl;
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("AddCommentRunsToParagraph()", ex);
            }
        }

        /// <summary>
        /// Creates an Inline for a single part of the comment.
        /// </summary>
        /// <param name="part"></param>
        /// <param name="nd"></param>
        /// <param name="i"></param>
        /// <param name="partsCount"></param>
        /// <returns></returns>
        private Inline CreateInlineForCommentPart(CommentPart part, TreeNode nd, int i, int partsCount)
        {
            Inline inl;

            switch (part.Type)
            {
                case CommentPartType.ASSESSMENT:
                    string assString = GuiUtilities.BuildAssessmentComment(nd);
                    inl = new Run(assString);
                    inl.ToolTip = Properties.Resources.TooltipEngineBlunderDetect;
                    inl.FontStyle = FontStyles.Normal;
                    inl.FontWeight = FontWeights.Normal;
                    inl.PreviewMouseDown += EventCommentRunClicked;
                    break;
                case CommentPartType.THUMBNAIL_SYMBOL:
                    // if this is not the second last part, insert extra space
                    string thmb;
                    if (i < partsCount - 2)
                    {
                        thmb = Constants.CHAR_SQUARED_SQUARE.ToString() + " ";
                    }
                    else
                    {
                        thmb = Constants.CHAR_SQUARED_SQUARE.ToString();
                    }
                    _lastThumbnailNode = nd;
                    inl = new Run(thmb);
                    string toolTip = BuildThumbnailToolTip(nd);
                    inl.ToolTip = toolTip;
                    inl.FontStyle = FontStyles.Normal;
                    inl.Foreground = ChessForgeColors.CurrentTheme.RtbForeground;
                    inl.FontWeight = FontWeights.Normal;
                    inl.PreviewMouseDown += EventCommentRunClicked;
                    break;
                case CommentPartType.URL:
                    inl = new Hyperlink(new Run(part.Text));
                    (inl as Hyperlink).NavigateUri = new Uri(part.Text);
                    inl.FontWeight = FontWeights.Normal;
                    inl.PreviewMouseDown += EventHyperlinkMouseLeftButtonDown;
                    inl.MouseEnter += EventHyperlinkMouseEnter;
                    inl.MouseLeave += EventHyperlinkMouseLeave;
                    inl.Foreground = ChessForgeColors.CurrentTheme.HyperlinkForeground;
                    inl.Cursor = Cursors.Hand;
                    break;
                case CommentPartType.REFERENCE:
                    inl = new Run(part.Text);
                    inl.FontWeight = FontWeights.Normal;
                    inl.Name = _run_comment_article_ref + nd.NodeId.ToString() + "_" + (part.Guid ?? "");
                    inl.PreviewMouseDown += EventReferenceMouseLeftButtonDown;
                    inl.MouseEnter += EventReferenceMouseEnter;
                    inl.MouseLeave += EventReferenceMouseLeave;
                    inl.Foreground = ChessForgeColors.CurrentTheme.ReferenceForeground;
                    inl.Cursor = Cursors.Hand;
                    break;
                default:
                    inl = new Run(part.Text);
                    inl.FontStyle = FontStyles.Normal;
                    inl.Foreground = ChessForgeColors.CurrentTheme.RtbForeground;
                    inl.FontWeight = FontWeights.Normal;
                    inl.PreviewMouseDown += EventCommentRunClicked;
                    break;
            }

            return inl;
        }

        /// <summary>
        /// Builds an appropriate tooltip for the node if applicable.
        /// </summary>
        /// <param name="nd"></param>
        /// <returns></returns>
        private string BuildThumbnailToolTip(TreeNode nd)
        {
            string toolTip = null;

            if (nd != null && nd.IsThumbnail)
            {
                if (_mainVariationTree.ContentType == GameData.ContentType.MODEL_GAME)
                {
                    toolTip = Properties.Resources.GameThumbnail;
                }
                else if (_mainVariationTree.ContentType == GameData.ContentType.EXERCISE)
                {
                    toolTip = Properties.Resources.ExerciseThumbnail;
                }
                else
                {
                    toolTip = Properties.Resources.ChapterThumbnail;
                }
            }

            return toolTip;
        }

        /// <summary>
        /// Creates the comment-before-move Run if there is a CommentBeforeRun with the move.
        /// Adds the run to the paragraph.
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="para"></param>
        private void AddCommentBeforeMoveRunToParagraph(TreeNode nd, Paragraph para)
        {
            if (string.IsNullOrEmpty(nd.CommentBeforeMove))
            {
                return;
            }

            try
            {
                string commentText = " " + (nd.CommentBeforeMove ?? "") + " ";

                Run run = new Run(commentText);
                run.FontStyle = FontStyles.Normal;
                run.Foreground = ChessForgeColors.CurrentTheme.RtbForeground;
                run.FontWeight = FontWeights.Normal;
                run.PreviewMouseDown += EventCommentBeforeMoveRunClicked;
                run.Name = _run_comment_ + nd.NodeId.ToString();

                run.Name = _run_comment_before_move_ + nd.NodeId.ToString();

                _dictNodeToCommentBeforeMoveRun[nd.NodeId] = run;
                _dictCommentBeforeMoveRunToParagraph[run] = para;

                Run rNode = _dictNodeToRun[nd.NodeId];
                if (RichTextBoxUtilities.IsFirstNonEmptyRunInPara(rNode, para))
                {
                    // if this is the first run in the para remove leading space to eliminate spurious indent.
                    run.Text = run.Text.Substring(1);
                }
                para.Inlines.InsertBefore(rNode, run);
            }
            catch (Exception ex)
            {
                AppLog.Message("AddCommentBeforeMoveRunToParagraph()", ex);
            }
        }

        /// <summary>
        /// Checks if the last run in the paragraph is a comment.
        /// NOTE: we rely on the fact the both pre- and post-move comment run names
        /// begin with the _run_comment_ constant.
        /// This method is used to determine whether we need to have move number included
        /// when rendering the next move.
        /// </summary>
        /// <param name="para"></param>
        /// <returns></returns>
        protected bool IsLastRunComment(Paragraph para, TreeNode nd)
        {
            bool res = false;

            if (para != null && para.Inlines.Last().Name.StartsWith(_run_comment_))
            {
                // finally check if this is actual textual comment
                if (nd.Parent != null && !string.IsNullOrEmpty(nd.Parent.Comment))
                {
                    res = true;
                }
            }

            return res;
        }

        /// <summary>
        /// Checks if there is anything to show in the comment run i.e.
        /// non-empty comment text, a thumbnail indicator or quiz points if the tree is in exercise editing mode.
        /// Note: if we only have a thumbnail and we building a printing/exporting view,
        /// we consider that there is nothing to show.
        /// </summary>
        /// <param name="nd"></param>
        /// <returns></returns>
        private bool IsCommentRunToShow(TreeNode nd)
        {
            return !string.IsNullOrEmpty(nd.Comment)
                   || !string.IsNullOrEmpty(nd.ArticleRefs)
                   || (nd.IsThumbnail && !_isPrinting)
                   || HandleBlunders && nd.Assessment != 0 && nd.IsMainLine()
                   || (_mainVariationTree.CurrentSolvingMode == VariationTree.SolvingMode.EDITING && nd.QuizPoints != 0);
        }

    }
}
