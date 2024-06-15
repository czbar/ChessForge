using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows;

namespace ChessForge
{
    /// <summary>
    /// Events for the ChaptersView.
    /// </summary>
    public partial class ChaptersView : RichTextBuilder
    {
        //*******************************************************************************************
        //
        //   LEFT BUTTON DOWN CLICK 
        //
        //*******************************************************************************************


        //*******************************************************************************************
        //   Workbook Title, Chapter Header, Intro, Study, Games, Exercises headers 
        //*******************************************************************************************

        /// <summary>
        /// The Workbook title was clicked.
        /// Invoke the Workbook options dialog.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventWorkbookTitleClicked(object sender, MouseButtonEventArgs e)
        {
            try
            {
                _mainWin.DisplayPosition(PositionUtils.SetupStartingPosition());

                if (e.ChangedButton == MouseButton.Left && e.ClickCount == 2)
                {
                    _mainWin.ShowWorkbookOptionsDialog();
                }

                e.Handled = true;
            }
            catch (Exception ex)
            {
                AppLog.Message("Exception in EventWorkbookTitleClicked(): " + ex.Message);
            }
        }

        /// <summary>
        /// Event handler invoked when a Chapter Run was clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventChapterHeaderClicked(object sender, MouseButtonEventArgs e)
        {
            try
            {
                _mainWin.DisplayPosition(PositionUtils.SetupStartingPosition());

                LastClickedItemType = WorkbookManager.ItemType.CHAPTER;

                Run r = (Run)e.Source;
                int chapterIndex = TextUtils.GetIdFromPrefixedString(r.Name);
                if (chapterIndex >= 0)
                {
                    Chapter chapter = WorkbookManager.SessionWorkbook.Chapters[chapterIndex];
                    WorkbookManager.LastClickedChapterIndex = chapterIndex;
                    WorkbookManager.EnableChaptersContextMenuItems(_mainWin.UiMncChapters, true, GameData.ContentType.GENERIC);

                    if (e.ChangedButton == MouseButton.Left)
                    {
                        if (e.ClickCount == 2)
                        {
                            SelectChapter(chapterIndex, true);
                        }
                        else
                        {
                            SelectChapterHeader(chapter, false);
                        }

                        e.Handled = true;
                    }
                    else if (e.ChangedButton == MouseButton.Right)
                    {
                        // TODO: this rebuilds the Study Tree. Performance!
                        SelectChapter(chapterIndex, false);
                    }

                    HighlightChapterSelections(chapter);
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("Exception in EventChapterRunClicked(): " + ex.Message);
            }
        }

        /// <summary>
        /// Event handler invoked when a Study Tree was clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventStudyTreeHeaderClicked(object sender, MouseButtonEventArgs e)
        {
            try
            {
                _mainWin.DisplayPosition(PositionUtils.SetupStartingPosition());

                LastClickedItemType = WorkbookManager.ItemType.CHAPTER;

                Run r = (Run)e.Source;
                int chapterIndex = TextUtils.GetIdFromPrefixedString(r.Name);
                if (chapterIndex >= 0)
                {
                    Chapter chapter = WorkbookManager.SessionWorkbook.Chapters[chapterIndex];
                    WorkbookManager.LastClickedChapterIndex = chapterIndex;
                    if (e.ChangedButton == MouseButton.Left)
                    {
                        SelectChapter(chapterIndex, true);
                    }
                    else if (e.ChangedButton == MouseButton.Right)
                    {
                        WorkbookManager.EnableChaptersContextMenuItems(_mainWin.UiMncChapters, true, GameData.ContentType.STUDY_TREE);
                        SelectChapter(chapterIndex, false);
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("Exception in EventStudyTreeRunClicked(): " + ex.Message);
            }

            e.Handled = true;
        }

        /// <summary>
        /// Event handler invoked when an Intro header was clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventIntroHeaderClicked(object sender, MouseButtonEventArgs e)
        {
            try
            {
                _mainWin.DisplayPosition(PositionUtils.SetupStartingPosition());
                LastClickedItemType = WorkbookManager.ItemType.CHAPTER;

                Run r = (Run)e.Source;
                int chapterIndex = TextUtils.GetIdFromPrefixedString(r.Name);
                if (chapterIndex >= 0)
                {
                    Chapter chapter = WorkbookManager.SessionWorkbook.Chapters[chapterIndex];
                    WorkbookManager.LastClickedChapterIndex = chapterIndex;
                    if (e.ChangedButton == MouseButton.Left)
                    {
                        SelectChapter(chapterIndex, false);
                    }
                    else if (e.ChangedButton == MouseButton.Right)
                    {
                        WorkbookManager.EnableChaptersContextMenuItems(_mainWin.UiMncChapters, true, GameData.ContentType.INTRO);
                        SelectChapter(chapterIndex, false);
                    }
                    _mainWin.UiTabIntro.Focus();
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("Exception in EventIntroRunClicked(): " + ex.Message);
            }

            e.Handled = true;
        }

        /// <summary>
        /// A Create Intro line was clicked.
        /// Add the Intro tab and switch the focus there.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventCreateIntroHeaderClicked(object sender, MouseButtonEventArgs e)
        {
            try
            {
                _mainWin.DisplayPosition(PositionUtils.SetupStartingPosition());
                LastClickedItemType = WorkbookManager.ItemType.CHAPTER;

                Run r = (Run)e.Source;
                int chapterIndex = TextUtils.GetIdFromPrefixedString(r.Name);
                if (chapterIndex >= 0)
                {
                    Chapter chapter = WorkbookManager.SessionWorkbook.Chapters[chapterIndex];
                    WorkbookManager.LastClickedChapterIndex = chapterIndex;
                    if (e.ChangedButton == MouseButton.Left)
                    {
                        SelectChapter(chapterIndex, true);
                    }
                    else if (e.ChangedButton == MouseButton.Right)
                    {
                        WorkbookManager.EnableChaptersContextMenuItems(_mainWin.UiMncChapters, true, GameData.ContentType.INTRO);
                        SelectChapter(chapterIndex, false);
                    }

                    _mainWin.UiMnChptCreateIntro_Click(null, null);
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("Exception in EventIntroRunClicked(): " + ex.Message);
            }

            e.Handled = true;
        }

        /// <summary>
        /// Event handler invoked when the Model Games header was clicked.
        /// On left click, expend/collapse the list.
        /// On right click select chapter and show the context menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventModelGamesHeaderClicked(object sender, MouseButtonEventArgs e)
        {
            try
            {
                _mainWin.DisplayPosition(PositionUtils.SetupStartingPosition());

                LastClickedItemType = WorkbookManager.ItemType.NONE;

                Run r = (Run)e.Source;
                int chapterIndex = TextUtils.GetIdFromPrefixedString(r.Name);
                Chapter chapter = WorkbookManager.SessionWorkbook.ActiveChapter;

                if (chapter.Index != chapterIndex)
                {
                    SelectChapter(chapterIndex, false);
                    chapter = WorkbookManager.SessionWorkbook.ActiveChapter;
                }

                if (chapter != null)
                {
                    WorkbookManager.LastClickedChapterIndex = chapterIndex;
                    if (e.ChangedButton == MouseButton.Left)
                    {
                        if (chapter.GetModelGameCount() > 0)
                        {
                            ExpandModelGamesList(chapter);
                        }
                        else
                        {
                            WorkbookManager.EnableChaptersContextMenuItems(_mainWin.UiMncChapters, true, GameData.ContentType.MODEL_GAME, true);
                            _mainWin.UiMncChapters.IsOpen = true;
                            e.Handled = true;
                        }
                    }
                    else if (e.ChangedButton == MouseButton.Right)
                    {
                        WorkbookManager.LastClickedModelGameIndex = -1;
                        WorkbookManager.EnableChaptersContextMenuItems(_mainWin.UiMncChapters, true, GameData.ContentType.MODEL_GAME);
                        SelectChapter(chapterIndex, false);
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("Exception in EventModelGamesHeaderClicked(): " + ex.Message);
            }

            e.Handled = true;
        }

        /// <summary>
        /// Event handler invoked when the Exercises header was clicked.
        /// On left click, expend/collapse the list.
        /// On right click select chapter and show the context menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventExercisesHeaderClicked(object sender, MouseButtonEventArgs e)
        {
            try
            {
                _mainWin.DisplayPosition(PositionUtils.SetupStartingPosition());

                LastClickedItemType = WorkbookManager.ItemType.NONE;

                Run r = (Run)e.Source;
                int chapterIndex = TextUtils.GetIdFromPrefixedString(r.Name);
                Chapter chapter = WorkbookManager.SessionWorkbook.ActiveChapter;

                if (chapter.Index != chapterIndex)
                {
                    SelectChapter(chapterIndex, false);
                    chapter = WorkbookManager.SessionWorkbook.ActiveChapter;
                }

                if (chapter != null)
                {
                    WorkbookManager.LastClickedChapterIndex = chapterIndex;
                    if (e.ChangedButton == MouseButton.Left)
                    {
                        if (chapter.GetExerciseCount() > 0)
                        {
                            ExpandExercisesList(chapter);
                        }
                        else
                        {
                            WorkbookManager.EnableChaptersContextMenuItems(_mainWin.UiMncChapters, true, GameData.ContentType.EXERCISE, true);
                            _mainWin.UiMncChapters.IsOpen = true;
                            e.Handled = true;
                        }
                    }
                    else if (e.ChangedButton == MouseButton.Right)
                    {
                        WorkbookManager.LastClickedExerciseIndex = -1;
                        WorkbookManager.EnableChaptersContextMenuItems(_mainWin.UiMncChapters, true, GameData.ContentType.EXERCISE);
                        SelectChapter(chapterIndex, false);
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("Exception in EventModelGamesHeaderClicked(): " + ex.Message);
            }

            e.Handled = true;
        }


        //******************************************************************
        // Individual Games and Exercises
        //******************************************************************

        /// <summary>
        /// Event handler invoked when a Model Game Run was clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventModelGameRunClicked(object sender, MouseButtonEventArgs e)
        {
            try
            {
                LastClickedItemType = WorkbookManager.ItemType.MODEL_GAME;

                Run runGame = (Run)e.Source;
                int chapterIndex = GetChapterIndexFromChildRun(runGame);
                Chapter chapter = WorkbookManager.SessionWorkbook.ActiveChapter;
                if (chapter.Index != chapterIndex)
                {
                    SelectChapter(chapterIndex, false);
                    chapter = WorkbookManager.SessionWorkbook.ActiveChapter;
                }

                int gameIndex = TextUtils.GetIdFromPrefixedString(runGame.Name);
                WorkbookManager.LastClickedModelGameIndex = gameIndex;
                WorkbookManager.SessionWorkbook.ActiveChapter.ActiveModelGameIndex = gameIndex;

                Article activeGame = WorkbookManager.SessionWorkbook.ActiveChapter.GetModelGameAtIndex(gameIndex);
                if (activeGame != null)
                {
                    _mainWin.DisplayPosition(activeGame.Tree.GetFinalPosition());
                }
                else
                {
                    _mainWin.DisplayPosition(PositionUtils.SetupStartingPosition());
                }

                if (chapter != null && gameIndex >= 0 && gameIndex < chapter.ModelGames.Count)
                {
                    if (e.ChangedButton == MouseButton.Left)
                    {
                        if (e.ClickCount == 2)
                        {
                            _mainWin.SelectModelGame(gameIndex, true);
                        }
                    }
                    else if (e.ChangedButton == MouseButton.Right)
                    {
                        WorkbookManager.EnableChaptersContextMenuItems(_mainWin.UiMncChapters, true, GameData.ContentType.MODEL_GAME);
                    }
                }

                HighlightChapterSelections(chapter);
            }
            catch (Exception ex)
            {
                AppLog.Message("Exception in EventModelGameRunClicked(): " + ex.Message);
            }

            e.Handled = true;
        }

        /// <summary>
        /// Event handler invoked when an Exercise Run was clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventExerciseRunClicked(object sender, MouseButtonEventArgs e)
        {
            try
            {
                LastClickedItemType = WorkbookManager.ItemType.EXERCISE;

                Run runExercise = (Run)e.Source;
                int chapterIndex = GetChapterIndexFromChildRun(runExercise);
                Chapter chapter = WorkbookManager.SessionWorkbook.ActiveChapter;
                if (chapter.Index != chapterIndex)
                {
                    SelectChapter(chapterIndex, false);
                    chapter = WorkbookManager.SessionWorkbook.ActiveChapter;
                }

                int exerciseIndex = TextUtils.GetIdFromPrefixedString(runExercise.Name);
                WorkbookManager.LastClickedExerciseIndex = exerciseIndex;
                WorkbookManager.SessionWorkbook.ActiveChapter.ActiveExerciseIndex = exerciseIndex;

                Article activeEcercise = WorkbookManager.SessionWorkbook.ActiveChapter.GetExerciseAtIndex(exerciseIndex);
                if (activeEcercise != null)
                {
                    _mainWin.DisplayPosition(activeEcercise.Tree.RootNode);
                }
                else
                {
                    _mainWin.DisplayPosition(PositionUtils.SetupStartingPosition());
                }

                if (chapter != null && exerciseIndex >= 0 && exerciseIndex < chapter.Exercises.Count)
                {
                    if (e.ChangedButton == MouseButton.Left)
                    {
                        if (e.ClickCount == 2)
                        {
                            _mainWin.SelectExercise(exerciseIndex, true);
                        }
                    }
                    else if (e.ChangedButton == MouseButton.Right)
                    {
                        WorkbookManager.EnableChaptersContextMenuItems(_mainWin.UiMncChapters, true, GameData.ContentType.EXERCISE);
                    }
                }

                HighlightChapterSelections(chapter);
            }
            catch (Exception ex)
            {
                AppLog.Message("Exception in EventExerciseRunClicked(): " + ex.Message);
            }

            e.Handled = true;
        }


        //******************************************************************
        // Expand symbol for Chapters, Games and Exercises
        //******************************************************************

        /// <summary>
        /// An expand/collapse character for the chapter was clicked.
        /// Establish which chapter this is for, check its expand/collapse status and flip it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventChapterExpandSymbolClicked(object sender, MouseButtonEventArgs e)
        {
            _mainWin.DisplayPosition(PositionUtils.SetupStartingPosition());

            try
            {
                Run rChapter = (Run)e.Source;
                int chapterIndex = TextUtils.GetIdFromPrefixedString(rChapter.Name);
                if (chapterIndex >= 0)
                {
                    Chapter chapter = WorkbookManager.SessionWorkbook.Chapters[chapterIndex];
                    ExpandChapterList(chapter, false);
                }
            }
            catch
            {
            }

            e.Handled = true;
        }

        /// <summary>
        /// An expand/collapse character on the Model Games list was clicked.
        /// Establish which chapter this is for, check its expand/collapse status and flip it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventModelGamesExpandSymbolClicked(object sender, MouseButtonEventArgs e)
        {
            try
            {
                _mainWin.DisplayPosition(PositionUtils.SetupStartingPosition());

                LastClickedItemType = WorkbookManager.ItemType.NONE;

                Run rExpandSymbol = (Run)e.Source;
                int chapterIndex = TextUtils.GetIdFromPrefixedString(rExpandSymbol.Name);
                Chapter chapter = WorkbookManager.SessionWorkbook.Chapters[chapterIndex];
                if (chapter.Index != WorkbookManager.SessionWorkbook.ActiveChapter.Index)
                {
                    SelectChapter(chapterIndex, false);
                }
                chapter.IsModelGamesListExpanded = !chapter.IsModelGamesListExpanded;
                BuildChapterParagraph(chapter, _dictChapterParas[chapter.Index]);
            }
            catch (Exception ex)
            {
                AppLog.Message("Exception in EventExpandSymbolClicked(): " + ex.Message);
            }

            e.Handled = true;
        }

        /// <summary>
        /// An expand/collapse character on the Exercises list was clicked.
        /// Establish which chapter this is for, check its expand/collapse status and flip it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventExercisesExpandSymbolClicked(object sender, MouseButtonEventArgs e)
        {
            try
            {
                _mainWin.DisplayPosition(PositionUtils.SetupStartingPosition());

                LastClickedItemType = WorkbookManager.ItemType.NONE;

                Run rExpandSymbol = (Run)e.Source;
                int chapterIndex = TextUtils.GetIdFromPrefixedString(rExpandSymbol.Name);
                Chapter chapter = WorkbookManager.SessionWorkbook.Chapters[chapterIndex];
                if (chapter.Index != WorkbookManager.SessionWorkbook.ActiveChapter.Index)
                {
                    SelectChapter(chapterIndex, false);
                }
                chapter.IsExercisesListExpanded = !chapter.IsExercisesListExpanded;
                BuildChapterParagraph(chapter, _dictChapterParas[chapter.Index]);
            }
            catch (Exception ex)
            {
                AppLog.Message("Exception in EventExercisesExpandSymbolClicked(): " + ex.Message);
            }

            e.Handled = true;
        }

        //*******************************************************************************************
        //
        //   HOVER / MOUSE MOVE 
        //
        //*******************************************************************************************

        /// <summary>
        /// Display Study Tree's Thumbnail when hovering over the chapter header.
        /// Allow game drop if coming from another chapter
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventChapterHeaderHovered(object sender, MouseEventArgs e)
        {
            try
            {
                AutoScroll(e);
                if (e.LeftButton != MouseButtonState.Pressed)
                {
                    DraggedArticle.IsBlocked = false;
                }

                Run rChapter = (Run)e.Source;
                int chapterIndex = TextUtils.GetIdFromPrefixedString(rChapter.Name);
                if (chapterIndex >= 0)
                {
                    if (e.LeftButton == MouseButtonState.Pressed && !DraggedArticle.IsBlocked)
                    {
                        if (!DraggedArticle.IsDragInProgress)
                        {
                            DraggedArticle.StartDragOperation(chapterIndex, -1, GameData.ContentType.NONE);
                        }

                        if (DraggedArticle.ContentType == GameData.ContentType.MODEL_GAME 
                            || DraggedArticle.ContentType == GameData.ContentType.EXERCISE
                            || DraggedArticle.IsChapterDragged())
                        {
                            _mainWin.UiRtbChaptersView.Cursor = DragAndDropCursors.GetAllowDropCursor();
                        }
                        else
                        {
                            _mainWin.UiRtbChaptersView.Cursor = DragAndDropCursors.GetBarredDropCursor();
                        }
                    }

                    Chapter chapter = WorkbookManager.SessionWorkbook.Chapters[chapterIndex];
                    TreeNode thumb = chapter.StudyTree.Tree.GetThumbnail();
                    if (thumb != null)
                    {
                        ShowFloatingBoardForNode(e, thumb, TabViewType.CHAPTERS);
                    }
                }

                e.Handled = true;
            }
            catch
            {
            }
        }

        /// <summary>
        /// Display Chapter's Thumbnail when hovering over the StudyTree header.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventStudyTreeHeaderHovered(object sender, MouseEventArgs e)
        {
            EventChapterHeaderHovered(sender, e);
        }

        /// <summary>
        /// Display Chapter's Thumbnail when hovering over the Intro header.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventIntroHeaderHovered(object sender, MouseEventArgs e)
        {
            EventChapterHeaderHovered(sender, e);
        }

        /// <summary>
        /// Indicate that dropping a game is allowed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventModelGamesHeaderHovered(object sender, MouseEventArgs e)
        {
            try
            {
                AutoScroll(e);
                if (e.LeftButton != MouseButtonState.Pressed)
                {
                    DraggedArticle.IsBlocked = false;
                }

                if (e.LeftButton == MouseButtonState.Pressed && DraggedArticle.IsDragInProgress)
                {
                    if (DraggedArticle.ContentType == GameData.ContentType.MODEL_GAME)
                    {
                        _mainWin.UiRtbChaptersView.Cursor = DragAndDropCursors.GetAllowDropCursor();
                    }
                    else
                    {
                        _mainWin.UiRtbChaptersView.Cursor = DragAndDropCursors.GetBarredDropCursor();
                    }
                }
                e.Handled = true;
            }
            catch
            {
            }
        }

        /// <summary>
        /// Indicate that dropping an exercise is allowed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventExercisesHeaderHovered(object sender, MouseEventArgs e)
        {
            try
            {
                AutoScroll(e);
                if (e.LeftButton != MouseButtonState.Pressed)
                {
                    DraggedArticle.IsBlocked = false;
                }

                if (e.LeftButton == MouseButtonState.Pressed && DraggedArticle.IsDragInProgress)
                {
                    if (DraggedArticle.ContentType == GameData.ContentType.EXERCISE)
                    {
                        _mainWin.UiRtbChaptersView.Cursor = DragAndDropCursors.GetAllowDropCursor();
                    }
                    else
                    {
                        _mainWin.UiRtbChaptersView.Cursor = DragAndDropCursors.GetBarredDropCursor();
                    }
                }
                e.Handled = true;
            }
            catch
            {
            }
        }

        /// <summary>
        /// Displays Model Game's thumbnail or the final position on the floating board.
        /// If the left button is pressed, initiates the game drag-and-drop.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventModelGameRunHovered(object sender, MouseEventArgs e)
        {
            try
            {
                AutoScroll(e);
                if (e.LeftButton != MouseButtonState.Pressed)
                {
                    DraggedArticle.IsBlocked = false;
                }

                Run run = (Run)e.Source;

                Chapter chapter = GetChapterAndItemIndexFromRun(run, out int gameIndex);
                Point p = e.GetPosition(run);
                Article game = chapter.GetModelGameAtIndex(gameIndex);
                TreeNode node = game.Tree.GetThumbnail();

                if (e.LeftButton == MouseButtonState.Pressed && !DraggedArticle.IsBlocked)
                {
                    // if not already in progress start the drag
                    if (!DraggedArticle.IsDragInProgress)
                    {
                        DraggedArticle.StartDragOperation(chapter.Index, gameIndex, GameData.ContentType.MODEL_GAME);
                    }
                    // drag in progress; set appropriate cursor
                    if (DraggedArticle.ContentType == GameData.ContentType.MODEL_GAME)
                    {
                        _mainWin.UiRtbChaptersView.Cursor = DragAndDropCursors.GetAllowDropCursor();
                    }
                    else
                    {
                        _mainWin.UiRtbChaptersView.Cursor = DragAndDropCursors.GetBarredDropCursor();
                    }
                }
                else
                {
                    if (node == null)
                    {
                        node = game.Tree.GetFinalPosition();
                    }
                    ShowFloatingBoardForNode(e, node, TabViewType.MODEL_GAME);
                }
                e.Handled = true;
            }
            catch
            {
            }
        }

        /// <summary>
        /// Displays Exercise's thumbnail or the initial position on the floating board.
        /// If the left button is pressed, initiates the exercise drag-and-drop.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventExerciseRunHovered(object sender, MouseEventArgs e)
        {
            try
            {
                AutoScroll(e);
                if (e.LeftButton != MouseButtonState.Pressed)
                {
                    DraggedArticle.IsBlocked = false;
                }

                Run r = (Run)e.Source;

                Chapter chapter = GetChapterAndItemIndexFromRun(r, out int exerciseIndex);
                Article exer = chapter.GetExerciseAtIndex(exerciseIndex);
                if (e.LeftButton == MouseButtonState.Pressed && !DraggedArticle.IsBlocked)
                {
                    // if not already in progress start the drag
                    if (!DraggedArticle.IsDragInProgress)
                    {
                        DraggedArticle.StartDragOperation(chapter.Index, exerciseIndex, GameData.ContentType.EXERCISE);
                    }
                    // drag in progress; set appropriate cursor
                    if (DraggedArticle.ContentType == GameData.ContentType.EXERCISE)
                    {
                        _mainWin.UiRtbChaptersView.Cursor = DragAndDropCursors.GetAllowDropCursor();
                    }
                    else
                    {
                        _mainWin.UiRtbChaptersView.Cursor = DragAndDropCursors.GetBarredDropCursor();
                    }
                }
                else
                {
                    TreeNode node = exer.Tree.GetThumbnail();
                    if (node == null)
                    {
                        node = exer.Tree.Nodes[0];
                    }
                    ShowFloatingBoardForNode(e, node, TabViewType.EXERCISE);
                }

                e.Handled = true;
            }
            catch
            {
            }
        }


        //*******************************************************************************************
        //
        //   MOUSE LEAVE 
        //
        //*******************************************************************************************

        /// <summary>
        /// The mouse no longer hovers over the chapter, so hide the floating board.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventChapterHeaderLeft(object sender, MouseEventArgs e)
        {
            HideFloatingBoard(TabViewType.CHAPTERS);
        }

        /// <summary>
        /// The mouse no longer hovers over the study tree, so hide the floating board.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventStudyTreeHeaderLeft(object sender, MouseEventArgs e)
        {
            HideFloatingBoard(TabViewType.CHAPTERS);
        }

        /// <summary>
        /// The mouse no longer hovers over the Intro header, so hide the floating board.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventIntroHeaderLeft(object sender, MouseEventArgs e)
        {
            HideFloatingBoard(TabViewType.CHAPTERS);
        }

        /// <summary>
        /// The mouse no longer hovers over the Model Game, so hide the floating board.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventModelGameRunLeft(object sender, MouseEventArgs e)
        {
            HideFloatingBoard(TabViewType.MODEL_GAME);
        }

        /// <summary>
        /// The mouse no longer hovers over the Exercise, so hide the floating board.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventExerciseRunLeft(object sender, MouseEventArgs e)
        {
            HideFloatingBoard(TabViewType.EXERCISE);
        }


        //*******************************************************************************************
        //
        //   LEFT BUTTON UP / DROP 
        //
        //*******************************************************************************************

        /// <summary>
        /// A mouse button was released over the chapter header.
        /// If drag-n-drop is in progress, insert the dragged game or exercise 
        /// as the first one in the relevant list, remove from the original spot.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventChapterHeaderDrop(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                Run run = e.Source as Run;
                if (run != null)
                {
                    try
                    {
                        if (DraggedArticle.IsDragInProgress)
                        {
                            int targetChapterIndex = GetChapterIndexFromChildRun(run);
                            if (DraggedArticle.IsChapterDragged())
                            {
                                MoveChapter(DraggedArticle.ChapterIndex, targetChapterIndex);
                            }
                            else if (DraggedArticle.ContentType == GameData.ContentType.MODEL_GAME || DraggedArticle.ContentType == GameData.ContentType.EXERCISE)
                            {
                                MoveArticle(targetChapterIndex, 0);
                            }
                        }
                    }
                    catch { }
                }

                e.Handled = true;
            }

            DraggedArticle.StopDragOperation();
            _mainWin.UiRtbChaptersView.Cursor = Cursors.Arrow;
        }

        /// <summary>
        /// Handle mouse left button up as if it was a chapter header
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventIntroHeaderDrop(object sender, MouseButtonEventArgs e)
        {
            EventChapterHeaderDrop(sender, e);
        }

        /// <summary>
        /// Handle mouse left button up as if it was a chapter header
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventStudyTreeHeaderDrop(object sender, MouseButtonEventArgs e)
        {
            EventChapterHeaderDrop(sender, e);
        }

        /// <summary>
        /// A mouse button was released over the game list header.
        /// If drag-n-drop is in progress, insert the dragged game here, 
        /// remove from the original spot and rebuild the chapter(s).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventModelGamesHeaderDrop(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                Run run = e.Source as Run;
                if (run != null)
                {
                    try
                    {
                        if (DraggedArticle.IsDragInProgress && DraggedArticle.ContentType == GameData.ContentType.MODEL_GAME)
                        {
                            int targetChapterIndex = GetChapterIndexFromChildRun(run);
                            MoveArticle(targetChapterIndex, 0);
                        }
                    }
                    catch { }
                }

                e.Handled = true;
            }

            DraggedArticle.StopDragOperation();
            _mainWin.UiRtbChaptersView.Cursor = Cursors.Arrow;
        }

        /// <summary>
        /// A mouse button was released over the exercise list header.
        /// If drag-n-drop is in progress, insert the dragged exercise here, 
        /// remove from the original spot and rebuild the chapter(s).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventExercisesHeaderDrop(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                Run run = e.Source as Run;
                if (run != null)
                {
                    try
                    {
                        if (DraggedArticle.IsDragInProgress && DraggedArticle.ContentType == GameData.ContentType.EXERCISE)
                        {
                            int targetChapterIndex = GetChapterIndexFromChildRun(run);
                            MoveArticle(targetChapterIndex, 0);
                        }
                    }
                    catch { }
                }

                e.Handled = true;
            }

            DraggedArticle.StopDragOperation();
            _mainWin.UiRtbChaptersView.Cursor = Cursors.Arrow;
        }

        /// <summary>
        /// A mouse button was released over the game run.
        /// If drag-n-drop is in progress, insert the dragged run here, 
        /// remove from the original spot and rebuild the chapter(s).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventModelGameRunDrop(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                Run run = e.Source as Run;
                if (run != null)
                {
                    try
                    {
                        if (DraggedArticle.IsDragInProgress && DraggedArticle.ContentType == GameData.ContentType.MODEL_GAME)
                        {
                            _mainWin.UiRtbChaptersView.Cursor = Cursors.Arrow;

                            int targetChapterIndex = GetChapterIndexFromChildRun(run);
                            int targetGameIndex = TextUtils.GetIdFromPrefixedString(run.Name);

                            // figure out if we hit the upper or the lower half of the Run
                            Point ptMousePos = e.GetPosition(_mainWin.UiRtbChaptersView);
                            TextPointer tpMousePos = _mainWin.UiRtbChaptersView.GetPositionFromPoint(ptMousePos, true);

                            // if we move down by half the font size, is it still the same the same TextPointer
                            Point ptBelow = new Point(ptMousePos.X, ptMousePos.Y);
                            ptBelow.Y += run.FontSize / 2;

                            TextPointer tpBelow = _mainWin.UiRtbChaptersView.GetPositionFromPoint(ptBelow, true);
                            if (tpMousePos.CompareTo(tpBelow) != 0)
                            {
                                targetGameIndex++;
                            }

                            if (DraggedArticle.ChapterIndex != targetChapterIndex || DraggedArticle.ArticleIndex != targetGameIndex)
                            {
                                MoveArticle(targetChapterIndex, targetGameIndex);
                            }
                        }
                    }
                    catch { }
                }

                e.Handled = true;
            }

            DraggedArticle.StopDragOperation();
            _mainWin.UiRtbChaptersView.Cursor = Cursors.Arrow;
        }

        /// <summary>
        /// A mouse button was released over an exercise run.
        /// If drag-n-drop is in progress, insert the dragged run here, 
        /// remove from the original spot and rebuild the chapter(s).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventExerciseRunDrop(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                Run run = e.Source as Run;
                if (run != null)
                {
                    try
                    {
                        if (DraggedArticle.IsDragInProgress && DraggedArticle.ContentType == GameData.ContentType.EXERCISE)
                        {
                            _mainWin.UiRtbChaptersView.Cursor = Cursors.Arrow;

                            int targetChapterIndex = GetChapterIndexFromChildRun(run);
                            int targetExerciseIndex = TextUtils.GetIdFromPrefixedString(run.Name);

                            // figure out if we hit the upper or the lower half of the Run
                            Point ptMousePos = e.GetPosition(_mainWin.UiRtbChaptersView);
                            TextPointer tpMousePos = _mainWin.UiRtbChaptersView.GetPositionFromPoint(ptMousePos, true);

                            // if we move down by half the font size, is it still the same the same TextPointer
                            Point ptBelow = new Point(ptMousePos.X, ptMousePos.Y);
                            ptBelow.Y += run.FontSize / 2;

                            TextPointer tpBelow = _mainWin.UiRtbChaptersView.GetPositionFromPoint(ptBelow, true);
                            if (tpMousePos.CompareTo(tpBelow) != 0)
                            {
                                targetExerciseIndex++;
                            }

                            if (DraggedArticle.ChapterIndex != targetChapterIndex || DraggedArticle.ArticleIndex != targetExerciseIndex)
                            {
                                MoveArticle(targetChapterIndex, targetExerciseIndex);
                            }
                        }
                    }
                    catch { }

                    e.Handled = true;
                }
            }

            DraggedArticle.StopDragOperation();
            _mainWin.UiRtbChaptersView.Cursor = Cursors.Arrow;
        }

        /// <summary>
        /// Moves the dragged article from its original place to the target location.
        /// </summary>
        /// <param name="targetChapterIndex"></param>
        /// <param name="articleIndex"></param>
        private void MoveArticle(int targetChapterIndex, int articleIndex)
        {
            Chapter targetChapter = AppState.Workbook.Chapters[targetChapterIndex];
            AppState.Workbook.MoveArticle(DraggedArticle.ContentType, DraggedArticle.ChapterIndex, DraggedArticle.ArticleIndex, targetChapterIndex, articleIndex);
            AppState.IsDirty = true;

            RebuildChapterParagraph(AppState.Workbook.Chapters[DraggedArticle.ChapterIndex]);
            if (DraggedArticle.ChapterIndex != articleIndex)
            {
                RebuildChapterParagraph(targetChapter);
            }
            HighlightActiveChapter();
        }

        /// <summary>
        /// Moves chapter from one index position to another. 
        /// </summary>
        /// <param name="sourceIndex"></param>
        /// <param name="targetIndex"></param>
        public void MoveChapter(int sourceIndex, int targetIndex)
        {
            if (AppState.Workbook.MoveChapter(sourceIndex, targetIndex))
            {
                BuildFlowDocumentForChaptersView();

                AppState.MainWin.SelectChapterByIndex(targetIndex, false, false);
                PulseManager.ChaperIndexToBringIntoView = targetIndex;
            }
        }

        /// <summary>
        /// Scrolls one line up/down when the mouse is close to the top/bottom
        /// of the view and the left mouse key is pressed.
        /// </summary>
        /// <param name="e"></param>
        private void AutoScroll(MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point pt = e.GetPosition(AppState.MainWin.UiRtbChaptersView);
                if (pt.Y < 50)
                {
                    AppState.MainWin.UiRtbChaptersView.LineUp();
                }
                else if (pt.Y > AppState.MainWin.UiRtbChaptersView.ActualHeight - 50)
                {
                    AppState.MainWin.UiRtbChaptersView.LineDown();
                }
            }
        }

    }
}
