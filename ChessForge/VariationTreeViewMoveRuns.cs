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
        /// Removes Runs/Inlines associated with the passed node from dictionaries.
        /// </summary>
        /// <param name="nd"></param>
        public void RemoveNodeFromDictionaries(TreeNode nd)
        {
            if (_dictNodeToRun.Keys.Contains(nd.NodeId))
            {
                Run run = _dictNodeToRun[nd.NodeId];
                _dictNodeToRun.Remove(nd.NodeId);
                _dictRunToParagraph.Remove(run);

                if (_dictNodeToCommentRun.Keys.Contains(nd.NodeId))
                {
                    Inline commentRun = _dictNodeToCommentRun[nd.NodeId];
                    _dictNodeToCommentRun.Remove(nd.NodeId);
                    _dictCommentRunToParagraph.Remove(commentRun);
                }

                if (_dictNodeToCommentBeforeMoveRun.Keys.Contains(nd.NodeId))
                {
                    Inline commentBeforeMoveRun = _dictNodeToCommentBeforeMoveRun[nd.NodeId];
                    _dictNodeToCommentBeforeMoveRun.Remove(nd.NodeId);
                    _dictCommentBeforeMoveRunToParagraph.Remove(commentBeforeMoveRun);
                }
            }
        }

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
                        RemoveCommentRunsFromHostingParagraph(inlComment, nd.NodeId);
                    }
                }
                else
                {
                    _dictNodeToCommentRun.Remove(nd.NodeId);
                    RemoveCommentRunsFromHostingParagraph(inlComment, nd.NodeId);

                    Paragraph para = r.Parent as Paragraph;
                    AddCommentRunsToParagraph(nd, para, out bool isBlunder, out _);
                    if (isBlunder)
                    {
                        TextUtils.RemoveBlunderNagFromText(r);
                    }
                    RemoveTrailingNewLinesInPara(para);
                }

                // the next move in para may need to be redrawn if it was black on move
                if (nd.ColorToMove == PieceColor.Black)
                {
                    if (nd.Children.Count > 0)
                    {
                        UpdateNextMoveText(r, nd, nd.Children[0]);
                    }
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
        /// NOTE: the Comment Before Run used to be just one text run but now we can have 
        /// diagram before move as well.
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

                bool isFirstInPara = RichTextBoxUtilities.IsFirstNonEmptyRunInPara(rMove, rMove.Parent as Paragraph);

                _dictNodeToCommentBeforeMoveRun.TryGetValue(nd.NodeId, out inlCommentBeforeMove);

                // if there is no comment or diagram just clear all
                if (string.IsNullOrEmpty(nd.CommentBeforeMove) && (!nd.IsDiagram || !nd.IsDiagramBeforeMove))
                {
                    if (inlCommentBeforeMove != null)
                    {
                        _dictNodeToCommentBeforeMoveRun.Remove(nd.NodeId);
                        RemoveCommentBeforeMoveRunsFromHostingParagraph(inlCommentBeforeMove, nd.NodeId);
                    }
                }
                else
                {
                    _dictNodeToCommentBeforeMoveRun.Remove(nd.NodeId);
                    RemoveCommentBeforeMoveRunsFromHostingParagraph(inlCommentBeforeMove, nd.NodeId);

                    Paragraph para = rMove.Parent as Paragraph;
                    AddCommentBeforeMoveRunsToParagraph(nd, para, isFirstInPara, out bool diagram);
                }

                // if the passed includeNumber was true, do not question it (it is part of first render)
                // otherwise, refresh the move's text if it is black's move as we may need a number in front.
                if (includeNumber != true && nd.ColorToMove == PieceColor.White)
                {
                    // we need the number if this is the first run in the paragraph or previous move has a Comment
                    // or this move has a CommentBeforeMove
                    bool includeNo = isFirstInPara
                                     || !string.IsNullOrWhiteSpace(nd.CommentBeforeMove)
                                     || (nd.IsDiagram && nd.IsDiagramBeforeMove)
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
        /// This is to insert or remove the move number from the next move's run 
        /// if it is a black's move depending on whether there is or isn't a comment 
        /// in the current move.
        /// </summary>
        /// <param name="run"></param>
        protected void UpdateNextMoveText(Run currRun, TreeNode currNode, TreeNode nextMoveNode)
        {
            // if there is a next move and it is in the same para.
            // if it is in the next para, we know there is no need to change
            // in particular, we do not want to remove the move number for the move
            // in the next para whether or not the current move has a comment.
            if (_dictNodeToRun.TryGetValue(nextMoveNode.NodeId, out Run nextMoveRun))
            {
                if (nextMoveRun != null && currRun.Parent == nextMoveRun.Parent)
                {
                    int nodeId = TextUtils.GetIdFromPrefixedString(nextMoveRun.Name);
                    TreeNode nextNode = ShownVariationTree.GetNodeFromNodeId(nodeId);
                    if (nextNode != null)
                    {
                        bool firstInPara = false;
                        Paragraph nextPara = nextMoveRun.Parent as Paragraph;
                        if (nextPara != null)
                        {
                            firstInPara = RichTextBoxUtilities.IsFirstMoveRunInParagraph(nextMoveRun, nextPara);
                        }

                        // take care of the special case where node 0 may have a comment
                        bool includeNumber = currNode.NodeId == 0
                            || !string.IsNullOrWhiteSpace(currNode.Comment)
                            || !string.IsNullOrEmpty(nextNode.CommentBeforeMove)
                            || currNode.IsDiagram
                            || firstInPara;
                        UpdateRunText(nextMoveRun, nextNode, includeNumber);
                    }
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
        /// Finds the Run following the passed Run
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
                                if (tokens.Length == 2 && (tokens[0] + "_") == RichTextBoxUtilities.RunMovePrefix)
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
        /// Creates a list of inlines for a diagram.  
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="para"></param>
        private List<Inline> CreateInlinesForDiagram(TreeNode nd, bool isFirstInPara)
        {
            List<Inline> inlines = new List<Inline>();

            InlineUIContainer iuc = VariationTreeViewDiagram.CreateDiagram(nd, out ChessBoardSmall chessboard, IsLargeDiagram(nd));
            if (iuc != null)
            {
                Run preDiagRun;
                if (nd.IsDiagramBeforeMove)
                {
                    if (isFirstInPara)
                    {
                        preDiagRun = new Run("");
                    }
                    else
                    {
                        preDiagRun = new Run("\n");
                    }
                    preDiagRun.Name = RichTextBoxUtilities.PreInlineDiagramBeforeMoveRunPrefix + nd.NodeId.ToString();
                }
                else
                {
                    preDiagRun = new Run("\n");
                    preDiagRun.Name = RichTextBoxUtilities.PreInlineDiagramRunPrefix + nd.NodeId.ToString();
                }
                inlines.Add(preDiagRun);

                iuc.MouseDown += EventRunClicked;
                inlines.Add(iuc);

                // TODO: the following needs resolving in some other way.
                // e.g. perhaps always add the post diag run and remove in post-processing
                // if it is found to be the last run in a paragraph.
                // NOTE: it might have already been resolved by removing the last new line in paras.
                inlines.Add(CreatePostDiagramRun(nd));

                if (nd.IsDiagramBeforeMove)
                {
                    if (nd.Parent != null)
                    {
                        chessboard.DisplayPosition(nd.Parent, false);
                    }
                }
                else
                {
                    chessboard.DisplayPosition(nd, false);
                }
            }

            return inlines;
        }

        /// <summary>
        /// Creates a Run to insert after the diagram.
        /// It has a unique name and contains just a new line 
        /// </summary>
        /// <param name="nd"></param>
        /// <returns></returns>
        private Run CreatePostDiagramRun(TreeNode nd)
        {
            Run postDiagRun = new Run("\n");
            if (nd.IsDiagramBeforeMove)
            {
                postDiagRun.Name = RichTextBoxUtilities.PostInlineDiagramBeforeMoveRunPrefix + nd.NodeId.ToString();
            }
            else
            {
                postDiagRun.Name = RichTextBoxUtilities.PostInlineDiagramRunPrefix + nd.NodeId.ToString();
            }
            return postDiagRun;
        }

        /// <summary>
        /// Creates the comment-before-move Run if there is a CommentBeforeRun with the move.
        /// Adds the run to the paragraph.
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="para"></param>
        private void AddCommentBeforeMoveRunsToParagraph(TreeNode nd, Paragraph para, bool isFirstInPara, out bool diagram)
        {
            diagram = false;

            if (!IsCommentBeforeMoveRunToShow(nd))
            {
                return;
            }

            try
            {
                List<CommentPart> parts = CommentProcessor.SplitCommentTextAtUrls(nd.CommentBeforeMove);

                // if the node has a diagram, insert it at the beginning (PreComment) or end (PostComment)
                if (nd.IsDiagram && nd.IsDiagramBeforeMove)
                {
                    diagram = true;
                    CommentPart diag = new CommentPart(CommentPartType.DIAGRAM, "");
                    if (nd.IsDiagramPreComment)
                    {
                        parts.Insert(0, diag);
                    }
                    else
                    {
                        parts.Add(diag);
                    }
                }

                CommentPart startPart = new CommentPart(CommentPartType.TEXT, BuildTextForBeforeMoveStartPart(nd, isFirstInPara));
                parts.Insert(0, startPart);

                CommentPart endPart = new CommentPart(CommentPartType.TEXT, " ");
                parts.Add(endPart);

                // if the last part is DIAGRAM, we already have a new line and we don't want a leading space either!
                if (parts.Count > 1 && parts[parts.Count - 2].Type == CommentPartType.DIAGRAM)
                {
                    endPart.Text = "";
                }

                PlaceCommentBeforeMovePartsIntoParagraph(para, nd, isFirstInPara, parts);
            }
            catch (Exception ex)
            {
                AppLog.Message("AddCommentBeforeMoveRunsToParagraph()", ex);
            }
        }

        /// <summary>
        /// Creates the comment Run or Runs if there is a comment with the move.
        /// Adds the runs to the paragraph.
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="para"></param>
        private void AddCommentRunsToParagraph(TreeNode nd, Paragraph para, out bool isAssessmentBlunderShown, out bool diagram)
        {
            diagram = false;

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

                CreateReferenceCommentParts(nd, parts);

                // if the node has a diagram, insert it at the beginning(PreComment) or end (PostComment)
                if (nd.IsDiagram && !nd.IsDiagramBeforeMove)
                {
                    diagram = true;
                    CommentPart diag = new CommentPart(CommentPartType.DIAGRAM, "");
                    if (nd.IsDiagramPreComment)
                    {
                        parts.Insert(0, diag);
                    }
                    else
                    {
                        parts.Add(diag);
                    }
                }

                CommentPart startPart = new CommentPart(CommentPartType.TEXT, BuildTextForStartPart(nd));
                parts.Insert(0, startPart);

                CommentPart endPart = new CommentPart(CommentPartType.TEXT, BuildTextForEndPart(nd));
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

                // if the last part is DIAGRAM, we already have a new line and we don't want a leading space either!
                if (parts.Count > 1 && parts[parts.Count - 2].Type == CommentPartType.DIAGRAM)
                {
                    endPart.Text = "";
                }

                PlaceCommentPartsIntoParagraph(para, nd, parts, ref isAssessmentBlunderShown);
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
        private List<Inline> CreateInlineForCommentPart(CommentPart part, TreeNode nd, bool isFirstInPara, int i, int partsCount)
        {
            List<Inline> inlines = new List<Inline>();
            Inline inl;

            switch (part.Type)
            {
                case CommentPartType.DIAGRAM:
                    inlines = CreateInlinesForDiagram(nd, isFirstInPara);
                    inl = inlines.Last();
                    break;
                case CommentPartType.ASSESSMENT:
                    string assString = GuiUtilities.BuildAssessmentComment(nd);
                    inl = new Run(assString);
                    inl.ToolTip = Properties.Resources.TooltipEngineBlunderDetect;
                    inl.FontStyle = FontStyles.Normal;
                    inl.FontWeight = FontWeights.Normal;
                    if (nd.IsDiagramBeforeMove)
                    {
                        inl.PreviewMouseDown += EventCommentBeforeMoveRunClicked;
                    }
                    else
                    {
                        inl.PreviewMouseDown += EventCommentRunClicked;
                    }
                    inlines.Add(inl);
                    break;
                case CommentPartType.THUMBNAIL_SYMBOL:
                    // if this is not the second last part, insert extra space
                    string thmb;
                    if (i < partsCount - 2)
                    {
                        thmb = Constants.CHAR_THUMBNAIL.ToString() + " ";
                    }
                    else
                    {
                        thmb = Constants.CHAR_THUMBNAIL.ToString();
                    }
                    _lastThumbnailNode = nd;
                    inl = new Run(thmb);
                    string toolTip = BuildThumbnailToolTip(nd);
                    inl.ToolTip = toolTip;
                    inl.FontStyle = FontStyles.Normal;
                    inl.Foreground = ChessForgeColors.CurrentTheme.RtbForeground;
                    inl.FontWeight = FontWeights.Normal;
                    if (nd.IsDiagramBeforeMove)
                    {
                        inl.PreviewMouseDown += EventCommentBeforeMoveRunClicked;
                    }
                    else
                    {
                        inl.PreviewMouseDown += EventCommentRunClicked;
                    }
                    inlines.Add(inl);
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
                    inlines.Add(inl);
                    break;
                case CommentPartType.GAME_EXERCISE_REFERENCE:
                case CommentPartType.CHAPTER_REFERENCE:
                    inl = new Run(part.Text);
                    inl.FontWeight = FontWeights.Normal;
                    // check for legacy GUIDs as they would break inl.Name
                    if ((part.Guid ?? "").Contains("-"))
                    {
                        string oldGuid = part.Guid;
                        part.Guid = WorkbookManager.UpdateGuid(oldGuid);
                        nd.References = (nd.References ?? "").Replace(oldGuid, part.Guid);
                    }
                    inl.Name = _run_comment_article_ref + nd.NodeId.ToString() + "_" + (part.Guid ?? "");
                    inl.Tag = part.Type;
                    inl.PreviewMouseDown += EventReferenceMouseButtonDown;
                    inl.MouseEnter += EventReferenceMouseEnter;
                    inl.MouseLeave += EventReferenceMouseLeave;
                    inl.Foreground = part.Type == CommentPartType.GAME_EXERCISE_REFERENCE ?
                        ChessForgeColors.CurrentTheme.GameExerciseRefForeground : ChessForgeColors.CurrentTheme.ChapterRefForeground;
                    inl.Cursor = Cursors.Hand;
                    inlines.Add(inl);
                    break;
                default:
                    inl = new Run(part.Text);
                    if (part.Text == Constants.PSEUDO_LINE_SPACING)
                    {
                        // small font size to ensure the additional spacing is not too big
                        inl.FontSize = 4;
                    }
                    inl.FontStyle = FontStyles.Normal;
                    inl.Foreground = ChessForgeColors.CurrentTheme.RtbForeground;
                    inl.FontWeight = FontWeights.Normal;
                    if (nd.IsDiagramBeforeMove)
                    {
                        inl.PreviewMouseDown += EventCommentBeforeMoveRunClicked;
                    }
                    else
                    {
                        inl.PreviewMouseDown += EventCommentRunClicked;
                    }
                    inlines.Add(inl);
                    break;
            }

            return inlines;
        }

        /// <summary>
        /// Creates inlines for comment parts and inserts them in the appropriate order 
        /// into the paragraph.
        /// </summary>
        /// <param name="para"></param>
        /// <param name="nd"></param>
        /// <param name="parts"></param>
        private void PlaceCommentBeforeMovePartsIntoParagraph(Paragraph para, TreeNode nd, bool isFirstInPara, List<CommentPart> parts)
        {
            Inline inlPrevious = null;
            for (int i = 0; i < parts.Count; i++)
            {
                CommentPart part = parts[i];
                List<Inline> inlines = CreateInlineForCommentPart(part, nd, isFirstInPara, i, parts.Count);

                // the above may or may not have set the name.
                foreach (Inline inl in inlines)
                {
                    if (string.IsNullOrEmpty(inl.Name))
                    {
                        inl.Name = _run_comment_before_move_ + nd.NodeId.ToString();
                    }
                }

                if (inlPrevious == null)
                {
                    // this is the first comment inline which is a single inline
                    // because only multiple ones are from the diagram and it will be always preceded
                    // by at least START. 
                    Run rNode;
                    rNode = _dictNodeToRun[nd.NodeId];
                    para.Inlines.InsertBefore(rNode, inlines[0]);
                    inlPrevious = inlines[0];

                    _dictNodeToCommentBeforeMoveRun[nd.NodeId] = inlines[0];
                    _dictCommentBeforeMoveRunToParagraph[inlines[0]] = para;
                }
                else
                {
                    foreach (Inline inline in inlines)
                    {
                        para.Inlines.InsertAfter(inlPrevious, inline);
                        inlPrevious = inline;
                    }
                }
            }
        }

        /// <summary>
        /// Creates inlines for comment parts and inserts them in the appropriate order 
        /// into the paragraph.
        /// </summary>
        /// <param name="para"></param>
        /// <param name="nd"></param>
        /// <param name="parts"></param>
        /// <param name="isAssessmentBlunderShown"></param>
        private void PlaceCommentPartsIntoParagraph(Paragraph para, TreeNode nd, List<CommentPart> parts, ref bool isAssessmentBlunderShown)
        {
            Inline inlPrevious = null;
            for (int i = 0; i < parts.Count; i++)
            {
                CommentPart part = parts[i];
                if (part.Type == CommentPartType.ASSESSMENT)
                {
                    isAssessmentBlunderShown = true;
                }

                List<Inline> inlines = CreateInlineForCommentPart(part, nd, false, i, parts.Count);

                // the above may or may not have set the name.
                foreach (Inline inl in inlines)
                {
                    if (string.IsNullOrEmpty(inl.Name))
                    {
                        inl.Name = _run_comment_ + nd.NodeId.ToString();
                    }
                }

                if (inlPrevious == null)
                {
                    // this is the first comment inline which is a single inline
                    // because only multiple ones are from the diagram and it will be always preceded
                    // by at least START. 
                    Run rNode;
                    rNode = _dictNodeToRun[nd.NodeId];
                    para.Inlines.InsertAfter(rNode, inlines[0]);
                    inlPrevious = inlines[0];

                    _dictNodeToCommentRun[nd.NodeId] = inlines[0];
                    _dictCommentRunToParagraph[inlines[0]] = para;
                }
                else
                {
                    foreach (Inline inline in inlines)
                    {
                        para.Inlines.InsertAfter(inlPrevious, inline);
                        inlPrevious = inline;
                    }
                }
            }
        }

        /// <summary>
        /// Create reference run for the node.
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="parts"></param>
        private void CreateReferenceCommentParts(TreeNode nd, List<CommentPart> parts)
        {
            if (!string.IsNullOrEmpty(nd.References))
            {
                List<Article> articles = ReferenceUtils.BuildReferencedArticlesList(nd.References);
                bool first = true;
                foreach (Article article in articles)
                {
                    CommentPartType cpt;
                    string title;
                    if (article.ContentType == GameData.ContentType.STUDY_TREE)
                    {
                        AppState.Workbook.GetChapterByGuid(article.Guid, out int chapterIndex);
                        title = Properties.Resources.Chapter + " " + (chapterIndex + 1).ToString() + ": " + article.Tree.Header.GetChapterTitle();
                        cpt = CommentPartType.CHAPTER_REFERENCE;
                    }
                    else
                    {
                        title = article.Tree.Header.BuildGameReferenceTitle(false);
                        if (article.ContentType == GameData.ContentType.EXERCISE)
                        {
                            title = Properties.Resources.Exercise + ": " + title;
                        }
                        cpt = CommentPartType.GAME_EXERCISE_REFERENCE;
                    }
                    if (!first)
                    {
                        title = "; " + title;
                    }
                    if (!first || !string.IsNullOrEmpty(nd.Comment))
                    {
                        title = " " + title;
                    }
                    parts.Add(new CommentPart(cpt, title, article.Guid));
                    first = false;
                }
            }
        }

        /// <summary>
        /// Builds text for the for the Start part of the comment.
        /// For a sideline comment it will be just a single space.
        /// For a main line comment, assuming certain additional criteria,
        /// it will be NewLines instead.
        /// </summary>
        /// <param name="nd"></param>
        /// <returns></returns>
        private string BuildTextForStartPart(TreeNode nd)
        {
            string text = string.Empty;

            // if configured and this is the mainline in Game or Exercise, and the comment is non-empty
            // and it is not on move 0, add newline. 
            if (Configuration.MainLineCommentLF
                && (ContentType == GameData.ContentType.MODEL_GAME || ContentType == GameData.ContentType.EXERCISE && !AppState.IsUserSolving())
                && nd.IsMainLine()
                && !string.IsNullOrEmpty(nd.Comment)
                && nd.Parent != null
                && (!nd.IsDiagram || !nd.IsDiagramPreComment))
            {
                if (Configuration.ExtraSpacing && !_isPrinting)
                {
                    text = Constants.PSEUDO_LINE_SPACING;
                }
                else
                {
                    text = "\n";
                }
            }
            else
            {
                if (nd.Parent != null)
                {
                    text = " ";
                }
            }

            return text;
        }

        /// <summary>
        /// Creates text to start the BeforeMove comment inlines.
        /// If this is the first run in the paragraph, it will be empty,
        /// if there is a BeforeMove diagram and the comment placed before
        /// the diagram, it will be a new line,
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="isFirstInPara"></param>
        /// <returns></returns>
        private string BuildTextForBeforeMoveStartPart(TreeNode nd, bool isFirstInPara)
        {
            string text = string.Empty;

            if (nd.IsDiagram && nd.IsDiagramBeforeMove && !nd.IsDiagramPreComment && !isFirstInPara && !string.IsNullOrEmpty(nd.CommentBeforeMove))
            {
                text = "\n";
            }
            else if (nd.Parent != null && !isFirstInPara)
            {
                text = " ";
            }

            return text;
        }

        /// <summary>
        /// Builds the end part of the comment.
        /// It will be NewLines if we are on main line 
        /// or a space if we are not.
        /// </summary>
        /// <param name="nd"></param>
        /// <returns></returns>
        private string BuildTextForEndPart(TreeNode nd)
        {
            string text = string.Empty;

            // if configured and this is the mainline in Game or Exercise, and the comment is non-empty
            // add newline. 
            if (Configuration.MainLineCommentLF
                && (ContentType == GameData.ContentType.MODEL_GAME || ContentType == GameData.ContentType.EXERCISE && !AppState.IsUserSolving())
                && nd.IsMainLine()
                && !string.IsNullOrEmpty(nd.Comment)
                && (!nd.IsDiagram || nd.IsDiagramPreComment))
            {
                if (Configuration.ExtraSpacing && !_isPrinting)
                {
                    text = Constants.PSEUDO_LINE_SPACING;
                }
                else
                {
                    text += "\n";
                }
            }
            else
            {
                text += " ";
            }

            return text;
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
                   || !string.IsNullOrEmpty(nd.References)
                   || (nd.IsDiagram && !nd.IsDiagramBeforeMove)
                   || (nd.IsThumbnail && !_isPrinting)
                   || HandleBlunders && nd.Assessment != 0 && nd.IsMainLine()
                   || (_mainVariationTree.CurrentSolvingMode == VariationTree.SolvingMode.EDITING && nd.QuizPoints != 0);
        }

        /// <summary>
        /// Checks if there is anything to show in the comment-before-move run i.e.
        /// non-empty BeforeMove comment text or a BeforeMove diagram.
        /// </summary>
        /// <param name="nd"></param>
        /// <returns></returns>
        private bool IsCommentBeforeMoveRunToShow(TreeNode nd)
        {
            return !string.IsNullOrEmpty(nd.CommentBeforeMove) || (nd.IsDiagram && nd.IsDiagramBeforeMove);
        }
    }
}
