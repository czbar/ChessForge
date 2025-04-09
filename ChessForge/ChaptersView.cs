using ChessForge.Properties;
using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace ChessForge
{
    /// <summary>
    /// Manages the Chapters view.
    /// The view is hosted in a RichTextBox.
    /// </summary>
    public partial class ChaptersView : RichTextBuilder
    {
        // Application's Main Window
        private MainWindow _mainWin;

        // RichTextBox underpinning this view
        private RichTextBox _richTextBox;

        // whether the view needs refreshing
        private bool _isDirty = true;

        /// <summary>
        /// RichTextPara dictionary accessor
        /// </summary>
        override internal Dictionary<string, RichTextPara> RichTextParas { get { return _richTextParas; } }

        private static readonly string STYLE_WORKBOOK_TITLE = "workbook_title";
        private static readonly string STYLE_CHAPTER_TITLE = "chapter_title";
        private static readonly string STYLE_SUBHEADER = "subheader";
        private static readonly string STYLE_MODEL_GAME = "model_game";
        private static readonly string STYLE_EXERCISE = "exercise";

        private const string SUBHEADER_INDENT = "        ";
        private const string SELECTED_SUBHEADER_INDENT = "       ";
        private const string SUBHEADER_DOUBLE_INDENT = "            ";
        private const string SELECTED_ARTICLE_INDENT = "           ";

        private string SELECTED_STUDY_PREFIX = SELECTED_SUBHEADER_INDENT + Constants.CHAR_SELECTED.ToString() + " ";
        private string SELECTED_ARTICLE_PREFIX = SELECTED_ARTICLE_INDENT + Constants.CHAR_SELECTED.ToString() + " ";
        private string SELECTED_CHAPTER_PREFIX = Constants.CHAR_SELECTED.ToString() + " ";

        private string NON_SELECTED_STUDY_PREFIX = SUBHEADER_INDENT;
        private string NON_SELECTED_ARTICLE_PREFIX = SUBHEADER_DOUBLE_INDENT;
        private string NON_SELECTED_CHAPTER_PREFIX = "";

        /// <summary>
        /// The type of item most recently clicked. It is required when handling an
        /// ambiguous user command e.g. Move Up/Down when we don't know which item we should
        /// apply it to.
        /// </summary>
        public WorkbookManager.ItemType LastClickedItemType = WorkbookManager.ItemType.NONE;

        /// <summary>
        /// Layout definitions for paragraphs at different levels.
        /// </summary>
        private readonly Dictionary<string, RichTextPara> _richTextParas = new Dictionary<string, RichTextPara>()
        {
            [STYLE_WORKBOOK_TITLE] = new RichTextPara(0, 10, 18, FontWeights.Bold, TextAlignment.Left),
            [STYLE_CHAPTER_TITLE] = new RichTextPara(10, 10, 16, FontWeights.Normal, TextAlignment.Left),
            [STYLE_SUBHEADER] = new RichTextPara(40, 10, 14, FontWeights.Normal, TextAlignment.Left),
            [STYLE_MODEL_GAME] = new RichTextPara(70, 5, 14, FontWeights.Normal, TextAlignment.Left),
            [STYLE_EXERCISE] = new RichTextPara(90, 5, 14, FontWeights.Normal, TextAlignment.Left),
            ["default"] = new RichTextPara(140, 5, 11, FontWeights.Normal, TextAlignment.Left),
        };

        /// <summary>
        /// Maps chapter Ids to Chapter objects
        /// </summary>
        private readonly Dictionary<int, Paragraph> _dictChapterParas = new Dictionary<int, Paragraph>();

        /// <summary>
        /// Names and prefixes for the Runs.
        /// They will identify the element of the Workbook structure being shown.
        /// In the run names they will be followed by the element's index in their
        /// respective list.
        /// </summary>
        private readonly string _run_chapter_expand_char_ = "_run_chapter_expand_char_";
        private readonly string _run_model_games_expand_char_ = "_run_model_games_expand_char_";
        private readonly string _run_exercises_expand_char_ = "_run_exercises_expand_char_";

        private readonly string _run_chapter_title_ = "_run_chapter_title_";
        private readonly string _run_study_tree_ = "study_tree_";
        private readonly string _run_intro_ = "intro_";
        private readonly string _run_create_intro_ = "create_intro_";

        private readonly string _run_model_games_header_ = "_run_model_games_";
        private readonly string _run_model_game_ = "_run_model_game_";

        private readonly string _run_exercises_header_ = "_run_exercises_";
        private readonly string _run_exercise_ = "_run_exercise_";

        /// <summary>
        /// Names and prefixes for the Paragraphs.
        /// </summary>
//        private readonly string _par_workbook_title_ = "par_workbook_title_";
        private readonly string _par_chapter_ = "par_chapter_";

        // attributes of the Run to bring to view
        private ChaptersViewRunAttrs _runToBringToView;

        // whether the view is built for printing
        private bool _isPrinting;

        /// <summary>
        /// Constructor. Sets a reference to the 
        /// FlowDocument for the RichTextBox control, via
        /// a call to the base class's constructor.
        /// </summary>
        /// <param name="rtb"></param>
        /// <param name="mainWin">Main application window or null if called for export/print.</param>
        public ChaptersView(RichTextBox rtb, MainWindow mainWin, bool isPrinting = false) : base(rtb)
        {
            // for the GUI display we need mainWin but when buidling the view
            // to print we don't
            if (mainWin != null)
            {
                _mainWin = mainWin;
                _mainWin.UiRtbChaptersView.AllowDrop = false;
                _mainWin.UiRtbChaptersView.IsReadOnly = true;

                _richTextBox = AppState.MainWin.UiRtbChaptersView;

                SetRtbPageWidth(_richTextBox.Document);
            }

            _isPrinting = isPrinting;
        }

        /// <summary>
        /// Flags whether the view needs refreshing
        /// </summary>
        public bool IsDirty
        {
            get { return _isDirty; }
            set { _isDirty = value; }
        }

        /// <summary>
        /// Builds a list of paragraphs to show the chapters in this Workbook and their content.
        /// </summary>
        public void BuildFlowDocumentForChaptersView(bool opBehind)
        {
            FlowDocument doc;
            if (opBehind)
            {
                doc = new FlowDocument();
            }
            else
            {
                doc = HostRtb.Document;
            }

            SetRtbPageWidth(doc);

            try
            {
                doc.Blocks.Clear();
                _dictChapterParas.Clear();

                Paragraph paraWorkbookTitle = AddNewParagraphToDoc(doc, STYLE_WORKBOOK_TITLE, WorkbookManager.SessionWorkbook.Title);
                paraWorkbookTitle.MouseDown += EventWorkbookTitleClicked;

                foreach (Chapter chapter in WorkbookManager.SessionWorkbook.Chapters)
                {
                    Paragraph para = BuildChapterParagraph(doc, chapter);
                    doc.Blocks.Add(para);
                    _dictChapterParas[chapter.Index] = para;
                }

                HighlightActiveChapter(doc);

                IsDirty = false;
            }
            catch (Exception ex)
            {
                AppLog.Message("BuildFlowDocumentForChaptersView(false)", ex);
            }

            if (opBehind)
            {
                HostRtb.Document = doc;
            }
        }

        /// <summary>
        /// Left mouse button has been released.
        /// Stop any drag operation that may be in progress.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            DraggedArticle.StopDragOperation();
            _mainWin.UiRtbChaptersView.Cursor = Cursors.Arrow;
        }

        /// <summary>
        /// Mouse left the Chapters View area.
        /// Stop any drag operation that may be in progress.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void MouseLeave(object sender, MouseEventArgs e)
        {
            DraggedArticle.StopDragOperation();
            _mainWin.UiRtbChaptersView.Cursor = Cursors.Arrow;
        }

        /// <summary>
        /// Mouse entered the Chapters View area.
        /// If the left button is down block entering of the drag-and-drop mode.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void MouseEnter(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DraggedArticle.IsBlocked = true;
            }
        }

        /// <summary>
        /// Mouse moved within the area and the event was not handled
        /// downstream.
        /// If drag is in progress show the "barred" cursor.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void MouseMove(object sender, MouseEventArgs e)
        {
            AutoScroll(e);
            if (DraggedArticle.IsDragInProgress)
            {
                _mainWin.UiRtbChaptersView.Cursor = DragAndDropCursors.GetBarredDropCursor();
            }
        }

        /// <summary>
        /// Swaps lines for 2 model games in the view identified by their indices.
        /// The game at the targetIndex is the new active game.
        /// </summary>
        /// <param name="chapter"></param>
        /// <param name="sourceIndex"></param>
        /// <param name="targetIndex"></param>
        public bool SwapModelGames(FlowDocument doc, Chapter chapter, int sourceIndex, int targetIndex)
        {
            bool res = false;

            Paragraph para = FindChapterParagraph(doc, chapter.Index);
            if (para != null)
            {
                Run first = FindArticleRunInParagraph(para, GameData.ContentType.MODEL_GAME, sourceIndex);
                Run second = FindArticleRunInParagraph(para, GameData.ContentType.MODEL_GAME, targetIndex);

                if (first != null && second != null)
                {
                    res = true;

                    first.Text = BuildModelGameTitleText(chapter, sourceIndex);
                    ShowSelectionMark(ref first, false, SELECTED_ARTICLE_PREFIX, NON_SELECTED_ARTICLE_PREFIX);

                    second.Text = BuildModelGameTitleText(chapter, targetIndex);
                    ShowSelectionMark(ref second, true, SELECTED_ARTICLE_PREFIX, NON_SELECTED_ARTICLE_PREFIX);

                    PulseManager.SetArticleToBringIntoView(chapter.Index, GameData.ContentType.MODEL_GAME, targetIndex);
                }
            }

            return res;
        }

        /// <summary>
        /// Swaps lines for 2 model games in the view identified by their indices.
        /// The game at the targetIndex is the new active game.
        /// </summary>
        /// <param name="chapter"></param>
        /// <param name="sourceIndex"></param>
        /// <param name="targetIndex"></param>
        public bool SwapExercises(FlowDocument doc, Chapter chapter, int sourceIndex, int targetIndex)
        {
            bool res = false;

            Paragraph para = FindChapterParagraph(doc, chapter.Index);
            if (para != null)
            {
                Run first = FindArticleRunInParagraph(para, GameData.ContentType.EXERCISE, sourceIndex);
                Run second = FindArticleRunInParagraph(para, GameData.ContentType.EXERCISE, targetIndex);

                if (first != null && second != null)
                {
                    res = true;

                    first.Text = BuildExerciseTitleText(chapter, sourceIndex);
                    ShowSelectionMark(ref first, false, SELECTED_ARTICLE_PREFIX, NON_SELECTED_ARTICLE_PREFIX);

                    second.Text = BuildExerciseTitleText(chapter, targetIndex);
                    ShowSelectionMark(ref second, true, SELECTED_ARTICLE_PREFIX, NON_SELECTED_ARTICLE_PREFIX);

                    PulseManager.SetArticleToBringIntoView(chapter.Index, GameData.ContentType.EXERCISE, targetIndex);
                }
            }

            return res;
        }

        /// <summary>
        /// Show/Hide intro headers per the current status
        /// </summary>
        public void UpdateIntroHeaders(FlowDocument doc)
        {
            List<IntroRunsToModify> lstRuns = new List<IntroRunsToModify>();

            foreach (Block block in doc.Blocks)
            {
                if (block is Paragraph)
                {
                    Paragraph para = (Paragraph)block;

                    Run introRun = null;
                    Run studyRun = null;

                    // we look for a Study Tree Run. From it, we'll get the chapter index
                    // and whether we found Intro run before. Then we'll decide what to do
                    foreach (Inline inl in para.Inlines)
                    {
                        if (inl is Run)
                        {
                            Run run = (Run)inl;
                            if (run.Name.StartsWith(_run_intro_))
                            {
                                introRun = run;
                            }
                            else if (run.Name.StartsWith(_run_study_tree_))
                            {
                                studyRun = run;
                                break;
                            }
                        }
                    }
                    if (studyRun != null)
                    {
                        lstRuns.Add(new IntroRunsToModify(para, introRun, studyRun));
                    }
                }
            }

            foreach (IntroRunsToModify r in lstRuns)
            {
                HandleIntroRun(r.Para, r.IntroRun, r.StudyRun);
            }
        }

        /// <summary>
        /// Sets the page width of the FlowDocument.
        /// It has to be reasonably large because we do not want 
        /// chapter titles wrapping but rather to have them on a single line 
        /// with a scroll bar if needed.
        /// </summary>
        /// <param name="doc"></param>
        private void SetRtbPageWidth(FlowDocument doc)
        {
            if (doc != null)
            {
                doc.PageWidth = 1000;
            }
        }

        /// <summary>
        /// Deletes or creates the Intro run as necessary.
        /// </summary>
        /// <param name="para"></param>
        /// <param name="introRun"></param>
        /// <param name="studyRun"></param>
        private void HandleIntroRun(Paragraph para, Run introRun, Run studyRun)
        {
            try
            {
                if (WorkbookManager.SessionWorkbook == null || studyRun == null)
                {
                    return;
                }

                int chapterIndex = TextUtils.GetIdFromPrefixedString(studyRun.Name);
                if (chapterIndex >= 0)
                {
                    Chapter chapter = WorkbookManager.SessionWorkbook.Chapters[chapterIndex];
                    bool introVisible = chapter.ShowIntro;
                    if (introVisible && introRun == null)
                    {
                        InsertIntroRun(para, chapter, studyRun);
                        RemoveCreateIntroRun(para);
                    }
                    else if (!introVisible && introRun != null)
                    {
                        InsertCreateIntroRun(para, chapter, studyRun);
                        para.Inlines.Remove(introRun);
                    }
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Remove the CrateIntro run if found.
        /// </summary>
        /// <param name="para"></param>
        private void RemoveCreateIntroRun(Paragraph para)
        {
            Run createIntro = null;

            foreach (Inline inl in para.Inlines)
            {
                if (inl is Run)
                {
                    if (inl.Name.StartsWith(_run_create_intro_))
                    {
                        createIntro = inl as Run;
                    }
                }
            }

            para.Inlines.Remove(createIntro);
        }


        /// <summary>
        /// Brings the title line of the chapter into view.
        /// </summary>
        /// <param name="chapterIndex"></param>
        public void BringChapterIntoView(FlowDocument doc, int chapterIndex)
        {
            try
            {
                Run rChapter = FindChapterTitleRun(doc, chapterIndex);
                rChapter?.BringIntoView();
            }
            catch { }
        }

        /// <summary>
        /// Brings ActiveChapter into view
        /// </summary>
        public void BringActiveChapterIntoView()
        {
            try
            {
                if (AppState.ActiveChapter != null)
                {
                    PulseManager.ChapterIndexToBringIntoView = AppState.ActiveChapter.Index;
                }
            }
            catch { }
        }


        /// <summary>
        /// Brings the title line of the chapter into view.
        /// </summary>
        /// <param name="chapterIndex"></param>
        public void BringChapterIntoViewByIndex(FlowDocument doc, int chapterIndex)
        {
            _mainWin.Dispatcher.Invoke(() =>
              {
                  Run rChapter = FindChapterTitleRun(doc, chapterIndex);
                  rChapter?.BringIntoView();
              });
        }

        /// <summary>
        /// Brings the the title line of the model game or exercise into view.
        /// </summary>
        /// <param name="chapterIndex"></param>
        /// <param name="contentType"></param>
        /// <param name="index"></param>
        public void BringArticleIntoView(FlowDocument doc, int chapterIndex, GameData.ContentType contentType, int index)
        {
            _mainWin.Dispatcher.Invoke(() =>
            {
                Paragraph paraChapter = FindChapterParagraph(doc, chapterIndex);
                if (paraChapter != null)
                {
                    Run r = FindArticleRunInParagraph(paraChapter, contentType, index);
                    r?.BringIntoView();
                }
            });
        }

        /// <summary>
        /// Highlights the title of the ActiveChapter.
        /// </summary>
        public void HighlightActiveChapter(FlowDocument doc)
        {
            Run runToSelect = null;
            Run runToClear = null;

            Paragraph paraToClear = null;

            try
            {
                int activeChapterIndex = WorkbookManager.SessionWorkbook.ActiveChapter.Index;
                // iterate over the runs with chapter name and marks selection on the active one
                foreach (Block block in doc.Blocks)
                {
                    if (block is Paragraph)
                    {
                        foreach (Inline inl in ((Paragraph)block).Inlines)
                        {
                            Run run = inl as Run;
                            if (run != null)
                            {
                                int chapterIndex = GetNodeIdFromRunName(run.Name, _run_chapter_title_);
                                if (chapterIndex >= 0)
                                {
                                    if (chapterIndex == activeChapterIndex)
                                    {
                                        runToSelect = run;
                                    }
                                    else
                                    {
                                        if (run.Text.StartsWith(SELECTED_CHAPTER_PREFIX))
                                        {
                                            // we need to identify one that is actually selected
                                            runToClear = run;
                                            paraToClear = block as Paragraph;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (runToSelect != null)
                {
                    ShowSelectionMark(ref runToSelect, true, SELECTED_CHAPTER_PREFIX, NON_SELECTED_CHAPTER_PREFIX);
                    int chapterIndex = GetChapterIndexFromChildRun(runToSelect);
                    Chapter chapter = WorkbookManager.SessionWorkbook.GetChapterByIndex(chapterIndex);
                    HighlightChapterSelections(chapter);
                }

                if (runToClear != null)
                {
                    ShowSelectionMark(ref runToClear, false, SELECTED_CHAPTER_PREFIX, NON_SELECTED_CHAPTER_PREFIX);
                    int chapterIndex = GetChapterIndexFromChildRun(runToClear);
                    Chapter chapter = WorkbookManager.SessionWorkbook.GetChapterByIndex(chapterIndex);
                    ClearChapterSelections(chapter, paraToClear);
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Identifies Paragraph for the chapter and then
        /// calls BuildChapterParagraph.
        /// </summary>
        /// <param name="chapter"></param>
        /// <returns></returns>
        public Paragraph RebuildChapterParagraph(FlowDocument doc, Chapter chapter)
        {
            Paragraph para = null; ;
            int chapterIndex = chapter.Index;

            foreach (Block b in doc.Blocks)
            {
                if (b is Paragraph)
                {
                    int id = TextUtils.GetIdFromPrefixedString((b as Paragraph).Name);
                    if (id == chapterIndex)
                    {
                        para = b as Paragraph;
                        break;
                    }
                }
            }

            return BuildChapterParagraph(doc, chapter, para);
        }

        /// <summary>
        /// Builds a paragraph for a single chapter
        /// </summary>
        /// <param name="chapter"></param>
        /// <param name="para"></param>
        /// <returns></returns>
        private Paragraph BuildChapterParagraph(FlowDocument doc, Chapter chapter, Paragraph para = null)
        {
            if (chapter == null)
            {
                return null;
            }

            try
            {
                if (para == null)
                {
                    para = AddNewParagraphToDoc(doc, STYLE_CHAPTER_TITLE, "");
                    _dictChapterParas[chapter.Index] = para;
                }
                else
                {
                    para.Inlines.Clear();
                }

                para.Name = _par_chapter_ + chapter.Index.ToString();

                if (!_isPrinting)
                {
                    char expandCollapse = chapter.IsViewExpanded ? Constants.CharCollapse : Constants.CharExpand;
                    Run rExpandChar = CreateRun(STYLE_CHAPTER_TITLE, expandCollapse.ToString() + " ", true);
                    rExpandChar.Name = _run_chapter_expand_char_ + chapter.Index.ToString();
                    rExpandChar.MouseDown += EventChapterExpandSymbolClicked;
                    para.Inlines.Add(rExpandChar);
                }
                else
                {
                    // in printing we want an empty line between chapters
                    para.Inlines.Add(new Run("\n"));
                }

                    string chapterNo = (WorkbookManager.SessionWorkbook.GetChapterIndex(chapter) + 1).ToString();
                Run rChapter = CreateRun(STYLE_CHAPTER_TITLE, "[" + chapterNo + ".] " + chapter.GetTitle(), true);
                if (chapter.Index == WorkbookManager.SessionWorkbook.ActiveChapter.Index && !_isPrinting)
                {
                    ShowSelectionMark(ref rChapter, true, SELECTED_CHAPTER_PREFIX, NON_SELECTED_CHAPTER_PREFIX);
                }
                rChapter.Name = _run_chapter_title_ + chapter.Index.ToString();
                rChapter.PreviewMouseDown += EventChapterHeaderClicked;
                rChapter.PreviewMouseUp += EventChapterHeaderDrop;
                rChapter.MouseMove += EventChapterHeaderHovered;
                rChapter.MouseLeave += EventChapterHeaderLeft;
                para.Inlines.Add(rChapter);

                if (chapter.IsViewExpanded || _isPrinting)
                {
                    InsertIntroRun(para, chapter);
                    InsertStudyRun(para, chapter);
                    InsertModelGamesRuns(para, chapter);
                    InsertExercisesRuns(para, chapter);
                }

                return para;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Finds the Paragraph for a given chapter.
        /// </summary>
        /// <param name="chapterIndex"></param>
        /// <returns></returns>
        private Paragraph FindChapterParagraph(FlowDocument doc, int chapterIndex)
        {
            Paragraph paraChapter = null;
            string paraName = _par_chapter_ + chapterIndex.ToString();
            foreach (Block block in doc.Blocks)
            {
                if (block is Paragraph)
                {
                    if (block.Name == paraName)
                    {
                        paraChapter = (Paragraph)block;
                        break;
                    }
                }
            }
            return paraChapter;
        }

        /// <summary>
        /// Finds the Run for an Article with at a given index in a given chapter.
        /// </summary>
        /// <param name="paraChapter"></param>
        /// <param name="contentType"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        private Run FindArticleRunInParagraph(Paragraph paraChapter, GameData.ContentType contentType, int index)
        {
            Run r = null;

            string runName = "";
            if (contentType == GameData.ContentType.MODEL_GAME)
            {
                runName = _run_model_game_ + index.ToString();
            }
            else if (contentType == GameData.ContentType.EXERCISE)
            {
                runName = _run_exercise_ + index.ToString();
            }

            foreach (Inline inl in paraChapter.Inlines)
            {
                if (inl is Run && inl.Name == runName)
                {
                    r = inl as Run;
                    break;
                }
            }

            return r;
        }


        /// <summary>
        /// Finds the Run with the title of a given chapter.
        /// </summary>
        /// <param name="chapterIndex"></param>
        /// <returns></returns>
        private Run FindChapterTitleRun(FlowDocument doc, int chapterIndex)
        {
            Run rChapter = null;
            string runName = _run_chapter_title_ + chapterIndex.ToString();
            foreach (Block block in HostRtb.Document.Blocks)
            {
                if (block is Paragraph)
                {
                    foreach (Inline inl in (block as Paragraph).Inlines)
                    {
                        if (inl is Run && inl.Name == runName)
                        {
                            rChapter = inl as Run;
                            break;
                        }
                    }
                }
                if (rChapter != null)
                {
                    break;
                }
            }

            return rChapter;
        }

        /// <summary>
        /// Inserts a Run representing the Study of the Chapter.
        /// There is always precisely one run in each chapter.
        /// </summary>
        /// <param name="para"></param>
        /// <returns></returns>
        private Run InsertStudyRun(Paragraph para, Chapter chapter)
        {
            string res = Resources.Study;
            Run runStudy = CreateRun(STYLE_SUBHEADER, "\n" + SUBHEADER_INDENT + res, true);
            runStudy.Name = _run_study_tree_ + chapter.Index.ToString();
            if (LastClickedItemType == WorkbookManager.ItemType.STUDY)
            {
                ShowSelectionMark(ref runStudy, true, SELECTED_STUDY_PREFIX, NON_SELECTED_STUDY_PREFIX);
            }
            runStudy.PreviewMouseDown += EventStudyTreeHeaderClicked;
            runStudy.PreviewMouseUp += EventStudyTreeHeaderDrop;
            runStudy.MouseMove += EventStudyTreeHeaderHovered;
            runStudy.MouseLeave += EventStudyTreeHeaderLeft;
            para.Inlines.Add(runStudy);
            return runStudy;
        }

        /// <summary>
        /// Inserts a Run representing the Intro to the Chapter.
        /// It will be shown or not depending on the content.
        /// </summary>
        /// <param name="para"></param>
        /// <returns></returns>
        private Run InsertIntroRun(Paragraph para, Chapter chapter, Run studyRun = null)
        {
            if (chapter.ShowIntro)
            {
                string res = Resources.Intro;
                Run r = CreateRun(STYLE_SUBHEADER, "\n" + SUBHEADER_INDENT + res, true);
                r.Name = _run_intro_ + chapter.Index.ToString();
                if (LastClickedItemType == WorkbookManager.ItemType.INTRO)
                {
                    ShowSelectionMark(ref r, true, SELECTED_STUDY_PREFIX, NON_SELECTED_STUDY_PREFIX);
                }
                r.PreviewMouseDown += EventIntroHeaderClicked;
                r.PreviewMouseUp += EventIntroHeaderDrop;
                r.MouseMove += EventIntroHeaderHovered;
                r.MouseLeave += EventIntroHeaderLeft;
                if (studyRun == null)
                {
                    para.Inlines.Add(r);
                }
                else
                {
                    para.Inlines.InsertBefore(studyRun, r);
                }
                return r;
            }
            else
            {
                InsertCreateIntroRun(para, chapter, studyRun);
                return null;
            }
        }

        /// <summary>
        /// Creates a Run advising the user of the option to create an Intro tab 
        /// </summary>
        /// <param name="para"></param>
        /// <param name="chapter"></param>
        /// <param name="studyRun"></param>
        /// <returns></returns>
        private Run InsertCreateIntroRun(Paragraph para, Chapter chapter, Run studyRun = null)
        {
            string res = Resources.CreateIntro;
            Run r = CreateRun(STYLE_SUBHEADER, "\n" + SUBHEADER_INDENT + "  " + res, true);
            r.Name = _run_create_intro_ + chapter.Index.ToString();
            r.FontWeight = FontWeights.Normal;
            r.FontStyle = FontStyles.Italic;
            r.Foreground = ChessForgeColors.CurrentTheme.ChaptersCreateIntroForeground;
            r.FontSize -= 2;
            r.MouseDown += EventCreateIntroHeaderClicked;
            if (studyRun == null)
            {
                para.Inlines.Add(r);
            }
            else
            {
                para.Inlines.InsertBefore(studyRun, r);
            }
            return r;
        }

        /// <summary>
        /// Builds text of a model game line
        /// </summary>
        /// <param name="chapter"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        private string BuildModelGameTitleText(Chapter chapter, int index)
        {
            return SUBHEADER_DOUBLE_INDENT + (index + 1).ToString() + ". " + chapter.ModelGames[index].Tree.Header.BuildGameHeaderLine(false, true, true, true);
        }

        /// <summary>
        /// Inserts the Model Games subheader
        /// and model games titles
        /// </summary>
        /// <param name="para"></param>
        /// <returns></returns>
        private Run InsertModelGamesRuns(Paragraph para, Chapter chapter)
        {
            para.Inlines.Add(CreateRun(STYLE_SUBHEADER, "\n" + SUBHEADER_INDENT, true));
            InsertExpandCollapseSymbolRun(para, _run_model_games_expand_char_, chapter.Index, GameData.ContentType.MODEL_GAME, chapter.IsModelGamesListExpanded, chapter.HasAnyModelGame);
            string res = Resources.Games;
            Run runGamesHeader = CreateRun(STYLE_SUBHEADER, res, true);
            runGamesHeader.Name = _run_model_games_header_ + chapter.Index.ToString();
            runGamesHeader.MouseDown += EventModelGamesHeaderClicked;
            runGamesHeader.PreviewMouseUp += EventModelGamesHeaderDrop;
            runGamesHeader.MouseMove += EventModelGamesHeaderHovered;
            para.Inlines.Add(runGamesHeader);

            if (chapter.IsModelGamesListExpanded)
            {
                for (int i = 0; i < chapter.ModelGames.Count; i++)
                {
                    para.Inlines.Add(new Run("\n"));
                    string lineText = BuildModelGameTitleText(chapter, i);
                    Run rGame = CreateRun(STYLE_SUBHEADER, lineText, true);
                    if (_isPrinting)
                    {
                        rGame.FontSize -= 2;
                    }
                    rGame.Name = _run_model_game_ + i.ToString();
                    // use Preview for MouseDn/Up but "plain" MouseMove. Otherwise drag-and-drop logic won't work
                    // as move events won't be received until mouse button is no longer down.
                    rGame.PreviewMouseDown += EventModelGameRunClicked;
                    rGame.PreviewMouseUp += EventModelGameRunDrop;
                    rGame.MouseMove += EventModelGameRunHovered;
                    rGame.MouseLeave += EventModelGameRunLeft;
                    if (LastClickedItemType == WorkbookManager.ItemType.MODEL_GAME 
                        && i == chapter.ActiveModelGameIndex 
                        && chapter == WorkbookManager.SessionWorkbook.ActiveChapter)
                    {
                        ShowSelectionMark(ref rGame, true, SELECTED_ARTICLE_PREFIX, NON_SELECTED_ARTICLE_PREFIX);
                    }
                    para.Inlines.Add(rGame);
                }
            }

            return runGamesHeader;
        }

        /// <summary>
        /// Builds text of an exercise line
        /// </summary>
        /// <param name="chapter"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        private string BuildExerciseTitleText(Chapter chapter, int index)
        {
            return SUBHEADER_DOUBLE_INDENT + (index + 1).ToString() + ". " + chapter.Exercises[index].Tree.Header.BuildGameHeaderLine(true, false, true, true);
        }

        /// <summary>
        /// Inserts the Exercise subheader
        /// and Exercise titles
        /// </summary>
        /// <param name="para"></param>
        /// <returns></returns>
        private Run InsertExercisesRuns(Paragraph para, Chapter chapter)
        {
            para.Inlines.Add(new Run("\n"));
            para.Inlines.Add(CreateRun(STYLE_SUBHEADER, SUBHEADER_INDENT, true));
            InsertExpandCollapseSymbolRun(para, _run_exercises_expand_char_, chapter.Index, GameData.ContentType.EXERCISE, chapter.IsExercisesListExpanded, chapter.HasAnyExercise);
            string res = Resources.Exercises;
            Run runExercisesHeader = CreateRun(STYLE_SUBHEADER, res, true);
            runExercisesHeader.Name = _run_exercises_header_ + chapter.Index.ToString();
            runExercisesHeader.MouseDown += EventExercisesHeaderClicked;
            runExercisesHeader.PreviewMouseUp += EventExercisesHeaderDrop;
            runExercisesHeader.MouseMove += EventExercisesHeaderHovered;
            para.Inlines.Add(runExercisesHeader);

            if (chapter.IsExercisesListExpanded)
            {
                for (int i = 0; i < chapter.Exercises.Count; i++)
                {
                    para.Inlines.Add(new Run("\n"));
                    string lineText = BuildExerciseTitleText(chapter, i);
                    Run rExercise = CreateRun(STYLE_SUBHEADER, lineText, false);
                    if (_isPrinting)
                    {
                        rExercise.FontSize -= 2;
                    }
                    rExercise.Name = _run_exercise_ + i.ToString();

                    // use Preview for MouseDn/Up but "plain" MouseMove. Otherwise drag-and-drop logic won't work
                    // as move events won't be received until mouse button is no longer down.
                    rExercise.PreviewMouseDown += EventExerciseRunClicked;
                    rExercise.PreviewMouseUp += EventExerciseRunDrop;
                    rExercise.MouseMove += EventExerciseRunHovered;
                    rExercise.MouseLeave += EventExerciseRunLeft;
                    if (LastClickedItemType == WorkbookManager.ItemType.EXERCISE 
                        && i == chapter.ActiveExerciseIndex 
                        && chapter == WorkbookManager.SessionWorkbook.ActiveChapter)
                    {
                        ShowSelectionMark(ref rExercise, true, SELECTED_ARTICLE_PREFIX, NON_SELECTED_ARTICLE_PREFIX);
                    }
                    para.Inlines.Add(rExercise);
                }
            }

            return runExercisesHeader;
        }

        /// <summary>
        /// Inserts a Run with the Expand/Collapse symbol.
        /// If hasContent is false, no symbol will be inserted.
        /// </summary>
        /// <param name="isExpanded"></param>
        /// <param name="hasContent"></param>
        /// <returns></returns>
        private Run InsertExpandCollapseSymbolRun(Paragraph para, string prefix, int chapterIndex, GameData.ContentType contentType, bool isExpanded, bool hasContent)
        {
            if (hasContent && !_isPrinting)
            {
                char expandCollapse = isExpanded ? Constants.CharCollapse : Constants.CharExpand;
                Run rExpandChar = CreateRun(STYLE_SUBHEADER, expandCollapse.ToString() + " ", true);
                rExpandChar.Name = prefix + chapterIndex;
                switch (contentType)
                {
                    case GameData.ContentType.MODEL_GAME:
                        rExpandChar.MouseDown += EventModelGamesExpandSymbolClicked;
                        break;
                    case GameData.ContentType.EXERCISE:
                        rExpandChar.MouseDown += EventExercisesExpandSymbolClicked;
                        break;
                }

                para.Inlines.Add(rExpandChar);

                return rExpandChar;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Expands or collapses the list of chapters depending
        /// on its current state.
        /// </summary>
        /// <param name="chapter"></param>
        private void ExpandChapterList(FlowDocument doc, Chapter chapter, bool forceExpand)
        {
            if (forceExpand)
            {
                chapter.IsViewExpanded = true;
            }
            else
            {
                bool? isExpanded = chapter.IsViewExpanded;

                if (isExpanded == true)
                {
                    chapter.IsViewExpanded = false;
                }
                else if (isExpanded == false)
                {
                    chapter.IsViewExpanded = true;
                }
            }

            BuildChapterParagraph(doc, chapter, _dictChapterParas[chapter.Index]);
        }

        /// <summary>
        /// Expands or collapses the list of Model Games depending
        /// on its current state.
        /// </summary>
        /// <param name="chapter"></param>
        private void ExpandModelGamesList(FlowDocument doc, Chapter chapter)
        {
            bool? isExpanded = chapter.IsModelGamesListExpanded;

            if (isExpanded == true)
            {
                chapter.IsModelGamesListExpanded = false;
            }
            else if (isExpanded == false)
            {
                chapter.IsModelGamesListExpanded = true;
                _runToBringToView = new ChaptersViewRunAttrs(chapter.Index, GameData.ContentType.MODEL_GAME);
            }

            BuildChapterParagraph(doc, chapter, _dictChapterParas[chapter.Index]);
        }

        /// <summary>
        /// Expands or collapses the list of Model Games depending
        /// on its current state.
        /// </summary>
        /// <param name="chapter"></param>
        private void ExpandExercisesList(FlowDocument doc, Chapter chapter)
        {
            bool? isExpanded = chapter.IsExercisesListExpanded;

            if (isExpanded == true)
            {
                chapter.IsExercisesListExpanded = false;
            }
            else if (isExpanded == false)
            {
                chapter.IsExercisesListExpanded = true;
                _runToBringToView = new ChaptersViewRunAttrs(chapter.Index, GameData.ContentType.EXERCISE);
            }

            BuildChapterParagraph(doc, chapter, _dictChapterParas[chapter.Index]);
        }

        /// <summary>
        /// Moves current selection up or down.
        /// If there is an item of the current selection type available to move it,
        /// we will select it.
        /// Otherwise, we will change the selected item.
        /// Note that if we change the type of the selected item we need to rebuild
        /// the entire view so that the highlights are reset correctly
        /// </summary>
        /// <param name="upOrDown"></param>
        public void MoveSelection(bool upOrDown)
        {
            GetSelectedItem(out WorkbookManager.ItemType itemType, out Chapter chapter, out int articleIndex);
            MoveSelectedItemUpOrDown(itemType, chapter, articleIndex, upOrDown);
        }

        /// <summary>
        /// Acts on selection i.e. opens the selected Intro, Study Tree, Game or Exercise.
        /// </summary>
        public void ActOnSelection()
        {
        }

        /// <summary>
        /// Moves selection up or down in the view
        /// </summary>
        /// <param name="itemType"></param>
        /// <param name="chapter"></param>
        /// <param name="articleIndex"></param>
        private void MoveSelectedItemUpOrDown(WorkbookManager.ItemType itemType, Chapter chapter, int articleIndex, bool upOrDown)
        {
            int index = -1;

            switch (itemType)
            {
                case WorkbookManager.ItemType.CHAPTER:
                    Chapter newSelChapter = upOrDown ? GetPreviousChapter(chapter) : GetNextChapter(chapter);
                    if (newSelChapter != null)
                    {
                        ActivateAndHighlightChapter(newSelChapter, chapter, true);
                        PulseManager.ChapterIndexToBringIntoView = newSelChapter.Index;
                    }
                    break;
                case WorkbookManager.ItemType.MODEL_GAME:
                    Article newSelGame = GetAdjacentModelGame(out index, upOrDown);
                    if (newSelGame != null)
                    {
                        chapter.ActiveModelGameIndex = index;
                        HighlightChapterSelections(chapter);
                        _mainWin.DisplayPosition(newSelGame.Tree.GetFinalPosition());
                        PulseManager.SetArticleToBringIntoView(chapter.Index, GameData.ContentType.MODEL_GAME, index);
                    }
                    break;
                case WorkbookManager.ItemType.EXERCISE:
                    Article newSelExercise = GetAdjacentExercise(out index, upOrDown);
                    if (newSelExercise != null)
                    {
                        chapter.ActiveExerciseIndex = index;
                        HighlightChapterSelections(chapter);
                        _mainWin.DisplayPosition(newSelExercise.Tree.RootNode);
                        PulseManager.SetArticleToBringIntoView(chapter.Index, GameData.ContentType.EXERCISE, index);
                    }
                    break;
            }
        }

        /// <summary>
        /// Changes the ActiveChapter and highlights the headers in the GUI accordingly.
        /// </summary>
        /// <param name="newChapter"></param>
        /// <param name="oldChapter"></param>
        /// <param name="expandView"></param>
        private void ActivateAndHighlightChapter(Chapter newChapter, Chapter oldChapter, bool expandView)
        {
            WorkbookManager.SessionWorkbook.ActiveChapter = newChapter;
            if (expandView)
            {
                newChapter.IsViewExpanded = true;
            }

            RebuildChapterParagraph(HostRtb.Document, newChapter);
            RebuildChapterParagraph(HostRtb.Document, oldChapter);
        }

        /// <summary>
        /// Returns the Chapter object in the list before the passed one.
        /// </summary>
        /// <param name="chapter"></param>
        /// <returns></returns>
        private Chapter GetPreviousChapter(Chapter chapter)
        {
            int index = WorkbookManager.SessionWorkbook.GetChapterIndex(chapter);
            if (index > 0)
            {
                index--;
                return WorkbookManager.SessionWorkbook.Chapters[index];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Returns the Chapter object in the list after the passed one.
        /// </summary>
        /// <param name="chapter"></param>
        /// <returns></returns>
        private Chapter GetNextChapter(Chapter chapter)
        {
            int index = WorkbookManager.SessionWorkbook.GetChapterIndex(chapter);
            if (index < WorkbookManager.SessionWorkbook.GetChapterCount() - 1)
            {
                index++;
                return WorkbookManager.SessionWorkbook.Chapters[index];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Returns the adjacent (previous or next) game.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="upOrDown"></param>
        /// <returns></returns>
        private Article GetAdjacentModelGame(out int index, bool upOrDown)
        {
            index = -1;

            try
            {
                bool allow = false;

                Chapter chapter = WorkbookManager.SessionWorkbook.ActiveChapter;
                if (upOrDown)
                {
                    if (chapter.ActiveModelGameIndex > 0)
                    {
                        chapter.ActiveModelGameIndex--;
                        allow = true;
                    }
                }
                else
                {
                    if (chapter.ActiveModelGameIndex < chapter.GetModelGameCount() - 1)
                    {
                        chapter.ActiveModelGameIndex++;
                        allow = true;
                    }
                }

                if (allow)
                {
                    index = chapter.ActiveModelGameIndex;
                    return chapter.ModelGames[chapter.ActiveModelGameIndex];
                }
                else
                {
                    return null;
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Returns the adjacent (previous or next) exercise.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="upOrDown"></param>
        /// <returns></returns>
        private Article GetAdjacentExercise(out int index, bool upOrDown)
        {
            index = -1;

            try
            {
                bool allow = false;

                Chapter chapter = WorkbookManager.SessionWorkbook.ActiveChapter;
                if (upOrDown)
                {
                    if (chapter.ActiveExerciseIndex > 0)
                    {
                        chapter.ActiveExerciseIndex--;
                        allow = true;
                    }
                }
                else
                {
                    if (chapter.ActiveExerciseIndex < chapter.GetExerciseCount() - 1)
                    {
                        chapter.ActiveExerciseIndex++;
                        allow = true;
                    }
                }

                if (allow)
                {
                    index = chapter.ActiveExerciseIndex;
                    return chapter.Exercises[chapter.ActiveExerciseIndex];
                }
                else
                {
                    return null;
                }
            }
            catch
            {
                return null;
            }
        }


        /// <summary>
        /// Returns details of the currently selected item which could be chapter, intro, study tree, game or exercise.
        /// </summary>
        /// <param name="itemType"></param>
        /// <param name="chapter"></param>
        /// <param name="articleIndex"></param>
        private void GetSelectedItem(out WorkbookManager.ItemType itemType, out Chapter chapter, out int articleIndex)
        {
            itemType = WorkbookManager.ItemType.NONE;
            chapter = null;
            articleIndex = -1;

            try
            {
                itemType = LastClickedItemType == WorkbookManager.ItemType.NONE ? WorkbookManager.ItemType.CHAPTER : LastClickedItemType;
                chapter = WorkbookManager.SessionWorkbook.ActiveChapter;
                articleIndex = -1;
                if (itemType == WorkbookManager.ItemType.MODEL_GAME)
                {
                    articleIndex = chapter.ActiveModelGameIndex;
                }
                else if (itemType == WorkbookManager.ItemType.EXERCISE)
                {
                    articleIndex = chapter.ActiveExerciseIndex;
                }
            }
            catch { }
        }

        /// <summary>
        /// Gets the ID from the parent paragraph identifying
        /// the chapter the passed Run belongs in.
        /// </summary>
        /// <returns></returns>
        private int GetChapterIndexFromChildRun(Run r)
        {
            try
            {
                Paragraph para = r.Parent as Paragraph;
                if (para != null)
                {
                    return TextUtils.GetIdFromPrefixedString(para.Name);
                }
                else
                {
                    return -1;
                }
            }
            catch
            {
                return -1;
            }
        }

        /// <summary>
        /// Brings Run matching attributes of 
        /// the _runToBringToView object int view.
        /// </summary>
        public void BringRunToview()
        {
            if (_runToBringToView != null)
            {
                try
                {
                    Run r = GetRunFromAttrs(_runToBringToView);
                    if (r != null)
                    {
                        r.BringIntoView();
                        r = null;
                    }
                    _runToBringToView = null;
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// Finds a Run matching the passed attributes.
        /// If we are looking for a Game or Exercise we will stop search
        /// at the currently selected one.
        /// </summary>
        /// <param name="attrs"></param>
        /// <returns></returns>
        private Run GetRunFromAttrs(ChaptersViewRunAttrs attrs)
        {
            Run r = null;

            Paragraph para = GetParagraph(attrs.ChapterIndex);
            if (para != null)
            {
                foreach (Inline inl in para.Inlines)
                {
                    if (inl is Run)
                    {
                        if (GetRunContentType(inl as Run) == attrs.ContentType)
                        {
                            r = inl as Run;
                            int index = TextUtils.GetIdFromPrefixedString(r.Name);
                            if (index >= 0)
                            {
                                if (attrs.ContentType == GameData.ContentType.MODEL_GAME && WorkbookManager.SessionWorkbook.ActiveChapter.ActiveModelGameIndex == index)
                                {
                                    break;
                                }
                                else if (attrs.ContentType == GameData.ContentType.EXERCISE && WorkbookManager.SessionWorkbook.ActiveChapter.ActiveExerciseIndex == index)
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            return r;
        }

        /// <summary>
        /// Gets the type of content representing by a Run
        /// based on the Run's name.
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        private GameData.ContentType GetRunContentType(Run r)
        {
            GameData.ContentType contentType = GameData.ContentType.NONE;

            if (r != null && r.Name != null)
            {
                if (r.Name.StartsWith(_run_exercises_header_) || r.Name.StartsWith(_run_exercise_))
                {
                    contentType = GameData.ContentType.EXERCISE;
                }
                else if (r.Name.StartsWith(_run_model_games_header_) || r.Name.StartsWith(_run_model_game_))
                {
                    contentType = GameData.ContentType.MODEL_GAME;
                }
            }

            return contentType;
        }

        /// <summary>
        /// Gets Paragraph object from a chapter id.  
        /// </summary>
        /// <param name="chapterIndex"></param>
        /// <returns></returns>
        private Paragraph GetParagraph(int chapterIndex)
        {
            Paragraph para = null;
            foreach (Block block in HostRtb.Document.Blocks)
            {
                if (block is Paragraph)
                {
                    int id = TextUtils.GetIdFromPrefixedString(block.Name);
                    if (id == chapterIndex)
                    {
                        para = block as Paragraph;
                        break;
                    }
                }
            }

            return para;
        }

        /// <summary>
        /// Selects a Chapter.
        /// If this is a new chapter, refreshes the view of the previously
        /// selected one (e.g. to remove marks on selected games/exercises)
        /// </summary>
        /// <param name="chapterIndex"></param>
        /// <param name="focusOnStudyTree"></param>
        private void SelectChapter(int chapterIndex, bool focusOnStudyTree)
        {
            Chapter prevActiveChapter = WorkbookManager.SessionWorkbook.ActiveChapter;

            if (focusOnStudyTree || prevActiveChapter.Index != chapterIndex)
            {
                _mainWin.SelectChapterByIndex(chapterIndex, focusOnStudyTree);
                ClearChapterSelections(prevActiveChapter, _dictChapterParas[prevActiveChapter.Index]);
            }
        }

        /// <summary>
        /// Returns the Chapter object and the item (Game or Exercise)
        /// index deduced the Run's name.
        /// </summary>
        /// <param name="r"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        private Chapter GetChapterAndItemIndexFromRun(Run r, out int index)
        {
            index = -1;
            Chapter chapter = null;
            try
            {
                int chapterIndex = GetChapterIndexFromChildRun(r);
                chapter = WorkbookManager.SessionWorkbook.Chapters[chapterIndex];
                index = TextUtils.GetIdFromPrefixedString(r.Name);
            }
            catch
            {
            }

            return chapter;
        }

        /// <summary>
        /// Selects the header od the passed chapter in this view.
        /// </summary>
        /// <param name="chapter"></param>
        /// <param name="forceExpand"></param>
        private void SelectChapterHeader(Chapter chapter, bool forceExpand)
        {
            if (chapter == null)
            {
                return;
            }

            if (WorkbookManager.SessionWorkbook.ActiveChapter != null && WorkbookManager.SessionWorkbook.ActiveChapter == chapter)
            {
                //ExpandChapterList(chapter, forceExpand);
            }
            else
            {
                if (forceExpand)
                {
                    chapter.IsViewExpanded = true;
                }
                SelectChapter(chapter.Index, false);
            }
        }

        /// <summary>
        /// Remove selection marks from any previously selected item
        /// </summary>
        /// <param name="chapter"></param>
        /// <param name="para"></param>
        private void ClearChapterSelections(Chapter chapter, Paragraph para)
        {
            if (para == null)
            {
                return;
            }

            List<Run> runsToClear = new List<Run>();

            foreach (Inline inl in para.Inlines)
            {
                Run run = inl as Run;
                if (run != null)
                {
                    runsToClear.Add(run);
                }
            }

            for (int i = 0; i < runsToClear.Count; i++)
            {
                Run r = runsToClear[i];
                TabViewType runType = GetRunTypeFromName(runsToClear[i].Name);
                if (runType == TabViewType.CHAPTERS)
                {
                    ShowSelectionMark(ref r, false, SELECTED_CHAPTER_PREFIX, NON_SELECTED_CHAPTER_PREFIX);
                }
                else if (runType == TabViewType.MODEL_GAME || runType == TabViewType.EXERCISE)
                {
                    ShowSelectionMark(ref r, false, SELECTED_ARTICLE_PREFIX, NON_SELECTED_ARTICLE_PREFIX);
                }
            }
        }

        /// <summary>
        /// Highlights chapter/games/exercise selections.
        /// </summary>
        /// <param name="chapter"></param>
        /// <param name="para"></param>
        private void HighlightChapterSelections(Chapter chapter)
        {
            Paragraph para = GetParagraphForChapter(HostRtb.Document, chapter);
            if (para == null)
            {
                return;
            }

            Run gameRunToSelect = null;
            Run gameRunToClear = null;

            Run exerciseRunToSelect = null;
            Run exerciseRunToClear = null;

            Run chapterRunToSelect = null;

            foreach (Inline inl in para.Inlines)
            {
                Run run = inl as Run;
                if (run != null)
                {
                    if (run.FontWeight != FontWeights.Normal)
                    {
                        run.FontWeight = FontWeights.Normal;
                    }

                    TabViewType runType = GetRunTypeFromName(run.Name);
                    int index = TextUtils.GetIdFromPrefixedString(run.Name);
                    switch (runType)
                    {
                        case TabViewType.CHAPTERS:
                            chapterRunToSelect = run;
                            break;
                        case TabViewType.MODEL_GAME:
                            if (index == chapter.ActiveModelGameIndex)
                            {
                                gameRunToSelect = run;
                            }
                            else if (run.Text.Contains(SELECTED_ARTICLE_PREFIX))
                            {
                                gameRunToClear = run;
                            }
                            break;
                        case TabViewType.EXERCISE:
                            if (index == chapter.ActiveExerciseIndex)
                            {
                                exerciseRunToSelect = run;
                            }
                            else if (run.Text.Contains(SELECTED_ARTICLE_PREFIX))
                            {
                                exerciseRunToClear = run;
                            }
                            break;
                    }
                }
            }

            if (gameRunToSelect != null)
            {
                ShowSelectionMark(ref gameRunToSelect, true, SELECTED_ARTICLE_PREFIX, NON_SELECTED_ARTICLE_PREFIX);
            }
            if (gameRunToClear != null)
            {
                ShowSelectionMark(ref gameRunToClear, false, SELECTED_ARTICLE_PREFIX, NON_SELECTED_ARTICLE_PREFIX);
            }
            if (exerciseRunToSelect != null)
            {
                ShowSelectionMark(ref exerciseRunToSelect, true, SELECTED_ARTICLE_PREFIX, NON_SELECTED_ARTICLE_PREFIX);
            }
            if (exerciseRunToClear != null)
            {
                ShowSelectionMark(ref exerciseRunToClear, false, SELECTED_ARTICLE_PREFIX, NON_SELECTED_ARTICLE_PREFIX);
            }
            if (chapterRunToSelect != null)
            {
                ShowSelectionMark(ref chapterRunToSelect, true, SELECTED_CHAPTER_PREFIX, NON_SELECTED_CHAPTER_PREFIX);
            }
        }

        /// <summary>
        /// Returns the type of object/article represented by the Run
        /// as encode in the Run's name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private TabViewType GetRunTypeFromName(string name)
        {
            TabViewType typ = TabViewType.NONE;

            if (string.IsNullOrEmpty(name))
            {
                return TabViewType.NONE;
            }
            else if (name.StartsWith(_run_chapter_title_))
            {
                typ = TabViewType.CHAPTERS;
            }
            else if (name.StartsWith(_run_model_game_))
            {
                typ = TabViewType.MODEL_GAME;
            }
            else if (name.StartsWith(_run_exercise_))
            {
                typ = TabViewType.EXERCISE;
            }

            return typ;
        }

        /// <summary>
        /// Returns the Paragraph hosting the passed chapter.
        /// </summary>
        /// <param name="chapter"></param>
        /// <returns></returns>
        private Paragraph GetParagraphForChapter(FlowDocument doc, Chapter chapter)
        {
            Paragraph para = null;

            if (chapter != null)
            {
                foreach (Block b in doc.Blocks)
                {
                    if (b is Paragraph)
                    {
                        int id = TextUtils.GetIdFromPrefixedString((b as Paragraph).Name);
                        if (id == chapter.Index)
                        {
                            para = b as Paragraph;
                            break;
                        }
                    }
                }
            }

            return para;
        }

        /// <summary>
        /// Modifies the text of the run to include or remove the SELECTION MARK.
        /// </summary>
        /// <param name="run"></param>
        /// <param name="highlighted"></param>
        private void ShowSelectionMark(ref Run run, bool highlighted, string selectionPrefix, string nonSelectionPrefix)
        {
            if (string.IsNullOrEmpty(run.Text) || _isPrinting)
            {
                return;
            }

            if (highlighted)
            {
                if (!run.Text.StartsWith(selectionPrefix))
                {
                    if (string.IsNullOrEmpty(nonSelectionPrefix))
                    {
                        run.Text = selectionPrefix + run.Text;
                    }
                    else
                    {
                        run.Text = run.Text.Replace(nonSelectionPrefix, selectionPrefix);
                    }
                }
            }
            else
            {
                if (run.Text.StartsWith(selectionPrefix))
                {
                    run.Text = run.Text.Replace(selectionPrefix, nonSelectionPrefix);
                }
            }
        }

        /// <summary>
        /// Shows the floating board of the requested type.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="nd"></param>
        /// <param name="viewType"></param>
        private void ShowFloatingBoardForNode(MouseEventArgs e, TreeNode nd, TabViewType viewType)
        {
            Point pt = e.GetPosition(_mainWin.UiRtbChaptersView);
            ChessBoardSmall floatingBoard = null;
            switch (viewType)
            {
                case TabViewType.CHAPTERS:
                    floatingBoard = _mainWin.ChaptersFloatingBoard;
                    break;
                case TabViewType.MODEL_GAME:
                    floatingBoard = _mainWin.ModelGameFloatingBoard;
                    break;
                case TabViewType.EXERCISE:
                    floatingBoard = _mainWin.ExerciseFloatingBoard;
                    break;
            }

            if (floatingBoard != null)
            {
                floatingBoard.FlipBoard(WorkbookManager.SessionWorkbook.TrainingSideConfig == PieceColor.Black);
                floatingBoard.DisplayPosition(nd, false);
                int xOffset = 20;
                int yOffset = 20;
                if (_mainWin.UiRtbChaptersView.ActualHeight < pt.Y + 170)
                {
                    yOffset = -170;
                }
                double xCoord = pt.X + xOffset;
                if (xCoord + 170 > _mainWin.UiRtbChaptersView.ActualWidth)
                {
                    xCoord = _mainWin.UiRtbChaptersView.ActualWidth - 170;
                }

                switch (viewType)
                {
                    case TabViewType.CHAPTERS:
                        _mainWin.UiVbChaptersFloatingBoard.Margin = new Thickness(xCoord, pt.Y + yOffset, 0, 0);
                        break;
                    case TabViewType.MODEL_GAME:
                        _mainWin.UiVbModelGameFloatingBoard.Margin = new Thickness(xCoord, pt.Y + yOffset, 0, 0);
                        break;
                    case TabViewType.EXERCISE:
                        _mainWin.UiVbExerciseFloatingBoard.Margin = new Thickness(xCoord, pt.Y + yOffset, 0, 0);
                        break;
                }
                _mainWin.ShowChaptersFloatingBoard(true, viewType);
            }
        }

        /// <summary>
        /// Hides the floating chessboard.
        /// </summary>
        private void HideFloatingBoard(TabViewType viewType)
        {
            _mainWin.ShowChaptersFloatingBoard(false, viewType);
        }

    }

    class IntroRunsToModify
    {
        public IntroRunsToModify(Paragraph para, Run introRun, Run studyRun)
        {
            Para = para;
            IntroRun = introRun;
            StudyRun = studyRun;
        }

        public Paragraph Para;
        public Run IntroRun;
        public Run StudyRun;
    }
}
