using ChessPosition;
using ChessPosition.Utils;
using GameTree;
using System;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace ChessForge
{
    public partial class VariationTreeView : RichTextBuilder
    {
        /// <summary>
        /// Event handler invoked when a Run was clicked.
        /// In response, we highlight the line to which this Run belongs
        /// (selecting the top branch for the part of the line beyond
        /// the clicked Run).
        /// 
        /// This event will also be invoked if an inline diagram was clicked.
        /// In that case the diagram's associated Run will be identified.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventRunClicked(object sender, MouseButtonEventArgs e)
        {
            Run r = null;

            if (e.Source is Run)
            {
                r = e.Source as Run;
            }
            else if (sender is InlineUIContainer iuc)
            {
                int nodeId = TextUtils.GetIdFromPrefixedString(iuc.Name);
                if (_dictNodeToRun.ContainsKey(nodeId))
                {
                    r = _dictNodeToRun[nodeId];
                }
            }

            _mainWin.StopReplayIfActive();
            SelectRun(r, e.ClickCount, e.ChangedButton);
        }

        /// <summary>
        /// A hyperlink part of the comment was clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventHyperlinkMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                try
                {
                    var hyperlink = (Hyperlink)sender;
                    Process.Start(hyperlink.NavigateUri.ToString());
                }
                catch { }
            }
        }

        /// <summary>
        /// Highlight the link when hovered over. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventHyperlinkMouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Hyperlink hl)
            {
                hl.Foreground = ChessForgeColors.CurrentTheme.HyperlinkHoveredForeground;
                e.Handled = true;
            }
        }

        /// <summary>
        /// Back to normal hyperlink color when mouse left.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventHyperlinkMouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Hyperlink hl)
            {
                hl.Foreground = ChessForgeColors.CurrentTheme.HyperlinkForeground;
                e.Handled = true;
            }
        }

        /// <summary>
        /// An article reference in the comment was clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventReferenceMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (sender is Inline inl)
                {
                    int nodeId = TextUtils.GetNodeIdAndArticleRefFromPrefixedString(inl.Name, out string articleRef);
                    TreeNode node = _mainWin.ActiveVariationTree.GetNodeFromNodeId(nodeId);

                    Article article = AppState.Workbook.GetArticleByGuid(articleRef, out int chapterIndex, out int articleIndex, true);

                    ReferenceUtils.LastClickedReference = articleRef;
                    ReferenceUtils.LastClickedReferenceNodeId = nodeId;

                    if (e.ChangedButton == MouseButton.Left)
                    {
                        if (_dictNodeToRun.ContainsKey(nodeId))
                        {
                            SelectRun(_dictNodeToRun[nodeId], 1, MouseButton.Left);
                        }

                        if (article != null)
                        {
                            if (article.ContentType == GameData.ContentType.MODEL_GAME || article.ContentType == GameData.ContentType.EXERCISE)
                            {
                                _mainWin.SelectArticle(chapterIndex, article.ContentType, articleIndex);
                            }
                            else
                            {
                                _mainWin.SelectChapterByIndex(chapterIndex, true);
                            }
                        }
                    }
                    else if (e.ChangedButton == MouseButton.Right && article != null)
                    {
                        try
                        {
                            // configure and show the context menu
                            ContextMenu cmReferences = _mainWin.Resources["CmReferences"] as ContextMenu;

                            ReferenceUtils.GetReferenceCountsByType(node, out int nodeGameRefCount, out int nodeExerciseRefCount, out int nodeChapterRefCount);
                            ReferenceUtils.GetReferenceCountsByType(_mainVariationTree, out int treeGameRefCount, out int treeExerciseRefCount, out int treeChapterRefCount);
                            ContextMenus.EnableReferencesMenuItems(cmReferences, article,
                                treeGameRefCount + treeExerciseRefCount, treeChapterRefCount,
                                nodeGameRefCount + nodeExerciseRefCount, nodeChapterRefCount);

                            if (cmReferences != null)
                            {
                                cmReferences.IsOpen = true;
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Highlight the article reference when hovered over. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventReferenceMouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Inline inl)
            {
                inl.Foreground = ChessForgeColors.CurrentTheme.GameExerciseRefHoveredForeground;
                if (inl.Tag is CommentPartType cpt && cpt == CommentPartType.CHAPTER_REFERENCE)
                {
                    inl.Foreground = ChessForgeColors.CurrentTheme.ChapterRefHoveredForeground;
                }
                e.Handled = true;
            }
        }

        /// <summary>
        /// Back to normal article reference color when mouse left.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventReferenceMouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Inline inl)
            {
                inl.Foreground = ChessForgeColors.CurrentTheme.GameExerciseRefForeground;
                if (inl.Tag is CommentPartType cpt && cpt == CommentPartType.CHAPTER_REFERENCE)
                {
                    inl.Foreground = ChessForgeColors.CurrentTheme.ChapterRefForeground;
                }
                e.Handled = true;
            }
        }

        /// <summary>
        /// A "comment run" was clicked.
        /// Invoke the dialog and update the run as needed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventCommentRunClicked(object sender, MouseButtonEventArgs e)
        {
            if (!IsSelectionEnabled())
            {
                return;
            }

            Run r = (Run)e.Source;

            int nodeId = TextUtils.GetIdFromPrefixedString(r.Name);
            TreeNode nd = _mainWin.ActiveVariationTree.GetNodeFromNodeId(nodeId);

            if (_dictNodeToRun.ContainsKey(nd.NodeId))
            {
                SelectRun(_dictNodeToRun[nd.NodeId], 1, MouseButton.Left);
                if (e.ClickCount == 2 && _mainWin.InvokeAnnotationsDialog(nd))
                {
                    InsertOrUpdateCommentRun(nd);
                }
            }
        }

        /// <summary>
        /// A "comment before move run" was clicked.
        /// Invoke the dialog and update the run as needed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventCommentBeforeMoveRunClicked(object sender, MouseButtonEventArgs e)
        {
            if (!IsSelectionEnabled())
            {
                return;
            }

            if (e.ClickCount == 2)
            {
                Run r = (Run)e.Source;

                int nodeId = TextUtils.GetIdFromPrefixedString(r.Name);
                TreeNode nd = _mainWin.ActiveVariationTree.GetNodeFromNodeId(nodeId);

                if (_dictNodeToRun.ContainsKey(nd.NodeId))
                {
                    SelectRun(_dictNodeToRun[nd.NodeId], 1, MouseButton.Left);
                    if (_mainWin.InvokeCommentBeforeMoveDialog(nd))
                    {
                        InsertOrUpdateCommentBeforeMoveRun(nd);
                    }
                }
            }
        }

        /// <summary>
        /// The Page Header paragraph was clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        virtual protected void EventPageHeaderClicked(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.ClickCount == 2)
                {
                    GameData.ContentType contentType = _mainVariationTree.Header.GetContentType(out _);
                    switch (contentType)
                    {
                        case GameData.ContentType.EXERCISE:
                            _mainWin.EditExerciseHeader();
                            break;
                        case GameData.ContentType.MODEL_GAME:
                            _mainWin.EditGameHeader();
                            break;
                        case GameData.ContentType.STUDY_TREE:
                            _mainWin.RenameChapter(AppState.ActiveChapter);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("EventPageHeaderClicked()", ex);
            }
        }

        /// <summary>
        /// A run in the fork table was clicked.
        /// Select the move with the nodeid.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventForkChildClicked(object sender, MouseButtonEventArgs e)
        {
            try
            {
                TextElement r = e.Source as TextElement;
                int id = TextUtils.GetIdFromPrefixedString(r.Name);
                Run rPly = _dictNodeToRun[id];
                SelectRun(rPly, 1, MouseButton.Left);
                rPly.BringIntoView();
            }
            catch (Exception ex)
            {
                AppLog.Message("EventForkChildClicked()", ex);
            }

            e.Handled = true;
        }
    }
}
