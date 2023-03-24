using ChessForge.Properties;
using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace ChessForge
{
    /// <summary>
    /// Manages text and events in the Chapters view.
    /// The view is hosted in a RichTextBox.
    /// </summary>
    public class ChaptersView : RichTextBuilder
    {
        // Application's Main Window
        private MainWindow _mainWin;

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
        private const string SUBHEADER_DOUBLE_INDENT = "            ";

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
            [STYLE_WORKBOOK_TITLE] = new RichTextPara(0, 10, 18, FontWeights.Bold, null, TextAlignment.Left),
            [STYLE_CHAPTER_TITLE] = new RichTextPara(10, 10, 16, FontWeights.Normal, null, TextAlignment.Left),
            [STYLE_SUBHEADER] = new RichTextPara(40, 10, 14, FontWeights.Normal, null, TextAlignment.Left),
            [STYLE_MODEL_GAME] = new RichTextPara(70, 5, 14, FontWeights.Normal, null, TextAlignment.Left),
            [STYLE_EXERCISE] = new RichTextPara(90, 5, 14, FontWeights.Normal, null, TextAlignment.Left),
            ["default"] = new RichTextPara(140, 5, 11, FontWeights.Normal, null, TextAlignment.Left),
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

        /// <summary>
        /// Constructor. Sets a reference to the 
        /// FlowDocument for the RichTextBox control, via
        /// a call to the base class's constructor.
        /// </summary>
        /// <param name="doc"></param>
        public ChaptersView(FlowDocument doc, MainWindow mainWin) : base(doc)
        {
            _mainWin = mainWin;
        }

        /// <summary>
        /// Builds a list of paragraphs to show the chapters in this Workbook and their content.
        /// </summary>
        public void BuildFlowDocumentForChaptersView()
        {
            Document.Blocks.Clear();
            _dictChapterParas.Clear();

            Paragraph paraWorkbookTitle = AddNewParagraphToDoc(STYLE_WORKBOOK_TITLE, WorkbookManager.SessionWorkbook.Title);
            paraWorkbookTitle.MouseDown += EventWorkbookTitleClicked;

            foreach (Chapter chapter in WorkbookManager.SessionWorkbook.Chapters)
            {
                Paragraph para = BuildChapterParagraph(chapter);
                Document.Blocks.Add(para);
                _dictChapterParas[chapter.Index] = para;
            }

            HighlightActiveChapter();
        }

        /// <summary>
        /// Show/Hide intro headers per the current status
        /// </summary>
        public void UpdateIntroHeaders()
        {
            List<IntroRunsToModify> lstRuns = new List<IntroRunsToModify>();

            foreach (Block block in Document.Blocks)
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
        public void BringChapterIntoView(int chapterIndex)
        {
            Run rChapter = FindChapterTitleRun(chapterIndex);
            rChapter?.BringIntoView();
        }

        /// <summary>
        /// Brings the title line of the chapter into view.
        /// </summary>
        /// <param name="chapterIndex"></param>
        public void BringChapterIntoViewByIndex(int chapterIndex)
        {
            Run rChapter = FindChapterTitleRun(chapterIndex);
            rChapter?.BringIntoView();
        }

        /// <summary>
        /// Brings the the title line of the model game or exercise into view.
        /// </summary>
        /// <param name="chapterIndex"></param>
        /// <param name="contentType"></param>
        /// <param name="index"></param>
        public void BringArticleIntoView(int chapterIndex, GameData.ContentType contentType, int index)
        {
            Paragraph paraChapter = FindChapterParagraph(chapterIndex);
            if (paraChapter != null)
            {
                AppState.DoEvents();
                Run r = FindArticleRunInParagraph(paraChapter, contentType, index);
                r?.BringIntoView();
            }
        }

        /// <summary>
        /// Highlights the title of the ActiveChapter.
        /// </summary>
        public void HighlightActiveChapter()
        {
            try
            {
                int activeChapterIndex = WorkbookManager.SessionWorkbook.ActiveChapter.Index;
                // iterate over the runs with chapter name and set the font for the active one to bold
                foreach (Block b in Document.Blocks)
                {
                    if (b is Paragraph)
                    {
                        foreach (Inline r in ((Paragraph)b).Inlines)
                        {
                            int chapterIndex = GetNodeIdFromRunName((r as Run).Name, _run_chapter_title_);
                            if (chapterIndex >= 0)
                            {
                                if (chapterIndex == activeChapterIndex)
                                {
                                    (r as Run).FontWeight = FontWeights.Bold;
                                }
                                else
                                {
                                    (r as Run).FontWeight = FontWeights.Normal;
                                }
                            }
                        }
                    }
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
        public Paragraph RebuildChapterParagraph(Chapter chapter)
        {
            Paragraph para = null; ;

            foreach (Block b in Document.Blocks)
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

            return BuildChapterParagraph(chapter, para);
        }

        /// <summary>
        /// Builds a paragraph for a single chapter
        /// </summary>
        /// <param name="chapter"></param>
        /// <param name="para"></param>
        /// <returns></returns>
        private Paragraph BuildChapterParagraph(Chapter chapter, Paragraph para = null)
        {
            if (chapter == null)
            {
                return null;
            }

            try
            {
                if (para == null)
                {
                    para = AddNewParagraphToDoc(STYLE_CHAPTER_TITLE, "");
                    _dictChapterParas[chapter.Index] = para;
                }
                else
                {
                    para.Inlines.Clear();
                }

                para.Name = _par_chapter_ + chapter.Index.ToString();

                char expandCollapse = chapter.IsViewExpanded ? Constants.CharCollapse : Constants.CharExpand;
                Run rExpandChar = CreateRun(STYLE_CHAPTER_TITLE, expandCollapse.ToString() + " ", true);
                rExpandChar.Name = _run_chapter_expand_char_ + chapter.Index.ToString();
                rExpandChar.MouseDown += EventChapterExpandSymbolClicked;
                para.Inlines.Add(rExpandChar);

                string chapterNo = (WorkbookManager.SessionWorkbook.GetChapterIndex(chapter) + 1).ToString();
                Run rTitle = CreateRun(STYLE_CHAPTER_TITLE, chapterNo + ". " + chapter.GetTitle(), true);
                if (chapter.Index == WorkbookManager.SessionWorkbook.ActiveChapter.Index)
                {
                    rTitle.FontWeight = FontWeights.Bold;
                }
                rTitle.Name = _run_chapter_title_ + chapter.Index.ToString();
                rTitle.MouseDown += EventChapterHeaderClicked;
                rTitle.MouseMove += EventChapterHeaderHovered;
                rTitle.MouseLeave += EventChapterHeaderLeft;
                para.Inlines.Add(rTitle);

                if (chapter.IsViewExpanded)
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
        private Paragraph FindChapterParagraph(int chapterIndex)
        {
            Paragraph paraChapter = null;
            string paraName = _par_chapter_ + chapterIndex.ToString();
            foreach (Block block in Document.Blocks)
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
        private Run FindChapterTitleRun(int chapterIndex)
        {
            Run rChapter = null;
            string runName = _run_chapter_title_ + chapterIndex.ToString();
            foreach (Block block in Document.Blocks)
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
            Run r = CreateRun(STYLE_SUBHEADER, "\n" + SUBHEADER_INDENT + res, true);
            r.Name = _run_study_tree_ + chapter.Index.ToString();
            if (LastClickedItemType == WorkbookManager.ItemType.STUDY)
            {
                r.FontWeight = FontWeights.Bold;
            }
            r.MouseDown += EventStudyTreeHeaderClicked;
            r.MouseMove += EventStudyTreeHeaderHovered;
            r.MouseLeave += EventStudyTreeHeaderLeft;
            para.Inlines.Add(r);
            return r;
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
                    r.FontWeight = FontWeights.Bold;
                }
                r.MouseDown += EventIntroHeaderClicked;
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
            r.Foreground = Brushes.Gray;
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
        /// Inserts the Model Games subheader
        /// and model games titles
        /// </summary>
        /// <param name="para"></param>
        /// <returns></returns>
        private Run InsertModelGamesRuns(Paragraph para, Chapter chapter)
        {
            //para.Inlines.Add(new Run("\n"));
            para.Inlines.Add(CreateRun(STYLE_SUBHEADER, "\n" + SUBHEADER_INDENT, true));
            InsertExpandCollapseSymbolRun(para, _run_model_games_expand_char_, chapter.Index, GameData.ContentType.MODEL_GAME, chapter.IsModelGamesListExpanded, chapter.HasAnyModelGame);
            string res = Resources.Games;
            Run r = CreateRun(STYLE_SUBHEADER, res, true);
            r.Name = _run_model_games_header_ + chapter.Index.ToString();
            r.MouseDown += EventModelGamesHeaderClicked;
            para.Inlines.Add(r);

            if (chapter.IsModelGamesListExpanded)
            {
                for (int i = 0; i < chapter.ModelGames.Count; i++)
                {
                    para.Inlines.Add(new Run("\n"));
                    Run rGame = CreateRun(STYLE_SUBHEADER, SUBHEADER_DOUBLE_INDENT + (i + 1).ToString() + ". " + chapter.ModelGames[i].Tree.Header.BuildGameHeaderLine(false), true);
                    rGame.Name = _run_model_game_ + i.ToString();
                    rGame.MouseDown += EventModelGameRunClicked;
                    rGame.MouseMove += EventModelGameRunHovered;
                    rGame.MouseLeave += EventModelGameRunLeft;
                    if (LastClickedItemType == WorkbookManager.ItemType.MODEL_GAME && i == chapter.ActiveModelGameIndex && chapter == WorkbookManager.SessionWorkbook.ActiveChapter)
                    {
                        rGame.FontWeight = FontWeights.Bold;
                    }
                    para.Inlines.Add(rGame);
                }
            }

            return r;
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
            Run r = CreateRun(STYLE_SUBHEADER, res, true);
            r.Name = _run_exercises_header_ + chapter.Index.ToString();
            r.MouseDown += EventExercisesHeaderClicked;
            para.Inlines.Add(r);

            if (chapter.IsExercisesListExpanded)
            {
                for (int i = 0; i < chapter.Exercises.Count; i++)
                {
                    para.Inlines.Add(new Run("\n"));
                    Run rGame = CreateRun(STYLE_SUBHEADER, SUBHEADER_DOUBLE_INDENT + (i + 1).ToString() + ". " + chapter.Exercises[i].Tree.Header.BuildGameHeaderLine(true, false), false);
                    rGame.Name = _run_exercise_ + i.ToString();
                    rGame.MouseDown += EventExerciseRunClicked;
                    rGame.MouseMove += EventExerciseRunHovered;
                    rGame.MouseLeave += EventExerciseRunLeft;
                    if (LastClickedItemType == WorkbookManager.ItemType.EXERCISE && i == chapter.ActiveExerciseIndex && chapter == WorkbookManager.SessionWorkbook.ActiveChapter)
                    {
                        rGame.FontWeight = FontWeights.Bold;
                    }
                    para.Inlines.Add(rGame);
                }
            }

            return r;
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
            if (hasContent)
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
        private void ExpandChapterList(Chapter chapter, bool forceExpand)
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

            BuildChapterParagraph(chapter, _dictChapterParas[chapter.Index]);
        }

        /// <summary>
        /// Expands or collapses the list of Model Games depending
        /// on its current state.
        /// </summary>
        /// <param name="chapter"></param>
        private void ExpandModelGamesList(Chapter chapter)
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

            BuildChapterParagraph(chapter, _dictChapterParas[chapter.Index]);
        }

        /// <summary>
        /// Expands or collapses the list of Model Games depending
        /// on its current state.
        /// </summary>
        /// <param name="chapter"></param>
        private void ExpandExercisesList(Chapter chapter)
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

            BuildChapterParagraph(chapter, _dictChapterParas[chapter.Index]);
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
                    }
                    break;
                case WorkbookManager.ItemType.MODEL_GAME:
                    Article newSelGame = GetAdjacentModelGame(out index, upOrDown);
                    if (newSelGame != null)
                    {
                        chapter.ActiveModelGameIndex = index;
                        RebuildChapterParagraph(chapter);
                        _mainWin.DisplayPosition(newSelGame.Tree.GetFinalPosition());
                        BringArticleIntoView(chapter.Index, GameData.ContentType.MODEL_GAME, index);
                    }
                    break;
                case WorkbookManager.ItemType.EXERCISE:
                    Article newSelExercise = GetAdjacentExercise(out index, upOrDown);
                    if (newSelExercise != null)
                    {
                        chapter.ActiveExerciseIndex = index;
                        RebuildChapterParagraph(chapter);
                        _mainWin.DisplayPosition(newSelExercise.Tree.RootNode);
                        BringArticleIntoView(chapter.Index, GameData.ContentType.EXERCISE, index);
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

            RebuildChapterParagraph(newChapter);
            RebuildChapterParagraph(oldChapter);
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
            foreach (Block block in Document.Blocks)
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
        /// selected one (e.g. to remove bolding on selected games/exercises)
        /// </summary>
        /// <param name="chapterIndex"></param>
        /// <param name="focusOnStudyTree"></param>
        private void SelectChapter(int chapterIndex, bool focusOnStudyTree)
        {
            Chapter prevActiveChapter = WorkbookManager.SessionWorkbook.ActiveChapter;

            if (focusOnStudyTree || prevActiveChapter.Index != chapterIndex)
            {
                _mainWin.SelectChapterByIndex(chapterIndex, focusOnStudyTree);
                BuildChapterParagraph(prevActiveChapter, _dictChapterParas[prevActiveChapter.Index]);
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
                ExpandChapterList(chapter, forceExpand);
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
        /// Displays the floating board for the passed position.
        /// </summary>
        /// <param name="thumb"></param>
        /// <param name="e"></param>
        private void ShowFloatingBoard(TreeNode thumb, MouseEventArgs e, PieceColor sideAtBottom = PieceColor.None)
        {
            if (thumb != null)
            {
                if (sideAtBottom == PieceColor.None)
                {
                    sideAtBottom = WorkbookManager.SessionWorkbook.TrainingSideConfig;
                }

                Point pt = e.GetPosition(_mainWin.UiRtbChaptersView);
                _mainWin.ChaptersFloatingBoard.FlipBoard(sideAtBottom == PieceColor.Black);
                _mainWin.ChaptersFloatingBoard.DisplayPosition(thumb, false);
                int xOffset = 20;
                int yOffset = 20;
                _mainWin.UiVbChaptersFloatingBoard.Margin = new Thickness(pt.X + xOffset, pt.Y + yOffset, 0, 0);
                _mainWin.ShowChaptersFloatingBoard(true);
            }
        }

        /// <summary>
        /// Hides the floating chessboard.
        /// </summary>
        private void HideFloatingBoard()
        {
            _mainWin.ShowChaptersFloatingBoard(false);
        }

        //*******************************************************************************************
        //
        //   EVENT HANDLERS 
        //
        //*******************************************************************************************

        /// <summary>
        /// The Worbook title was clicked.
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
                    _mainWin.ShowWorkbookOptionsDialog(false);
                }
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
                    WorkbookManager.EnableChaptersContextMenuItems(_mainWin._cmChapters, true, GameData.ContentType.GENERIC);

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
                    }
                    else if (e.ChangedButton == MouseButton.Right)
                    {
                        // TODO: this rebuilds the Study Tree. Performance!
                        SelectChapter(chapterIndex, false);
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("Exception in EventChapterRunClicked(): " + ex.Message);
            }
        }

        /// <summary>
        /// Display Study Tree's Thumbnail.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventChapterHeaderHovered(object sender, MouseEventArgs e)
        {
            try
            {
                Run r = (Run)e.Source;
                int chapterIndex = TextUtils.GetIdFromPrefixedString(r.Name);
                if (chapterIndex >= 0)
                {
                    Chapter chapter = WorkbookManager.SessionWorkbook.Chapters[chapterIndex];
                    TreeNode thumb = chapter.StudyTree.Tree.GetThumbnail();
                    if (thumb != null)
                    {
                        Point pt = e.GetPosition(_mainWin.UiRtbChaptersView);
                        _mainWin.ChaptersFloatingBoard.FlipBoard(WorkbookManager.SessionWorkbook.TrainingSideConfig == PieceColor.Black);
                        _mainWin.ChaptersFloatingBoard.DisplayPosition(thumb, false);
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
                        _mainWin.UiVbChaptersFloatingBoard.Margin = new Thickness(xCoord, pt.Y + yOffset, 0, 0);
                        _mainWin.ShowChaptersFloatingBoard(true);
                    }
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// The mouse no longer hovers over the chapter, so hide the floating board.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventChapterHeaderLeft(object sender, MouseEventArgs e)
        {
            HideFloatingBoard();
        }

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
                Run r = (Run)e.Source;
                int chapterIndex = TextUtils.GetIdFromPrefixedString(r.Name);
                if (chapterIndex >= 0)
                {
                    Chapter chapter = WorkbookManager.SessionWorkbook.Chapters[chapterIndex];
                    ExpandChapterList(chapter, false);
                }
            }
            catch
            {
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
                        WorkbookManager.EnableChaptersContextMenuItems(_mainWin._cmChapters, true, GameData.ContentType.STUDY_TREE);
                        SelectChapter(chapterIndex, false);
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("Exception in EventStudyTreeRunClicked(): " + ex.Message);
            }
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
                        SelectChapter(chapterIndex, true);
                    }
                    else if (e.ChangedButton == MouseButton.Right)
                    {
                        WorkbookManager.EnableChaptersContextMenuItems(_mainWin._cmChapters, true, GameData.ContentType.INTRO);
                        SelectChapter(chapterIndex, false);
                    }
                    _mainWin.UiTabIntro.Focus();
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("Exception in EventIntroRunClicked(): " + ex.Message);
            }
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
                        WorkbookManager.EnableChaptersContextMenuItems(_mainWin._cmChapters, true, GameData.ContentType.INTRO);
                        SelectChapter(chapterIndex, false);
                    }

                    _mainWin.UiMnChptCreateIntro_Click(null, null);
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("Exception in EventIntroRunClicked(): " + ex.Message);
            }
        }

        /// <summary>
        /// Display Chapter's Thumbnail.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventStudyTreeHeaderHovered(object sender, MouseEventArgs e)
        {
            EventChapterHeaderHovered(sender, e);
        }

        /// <summary>
        /// Display Chapter's Thumbnail.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventIntroHeaderHovered(object sender, MouseEventArgs e)
        {
            EventChapterHeaderHovered(sender, e);
        }

        /// <summary>
        /// The mouse no longer hovers over the study tree, so hide the floating board.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventStudyTreeHeaderLeft(object sender, MouseEventArgs e)
        {
            HideFloatingBoard();
        }

        /// <summary>
        /// The mouse no longer hovers over the Intro header, so hide the floating board.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventIntroHeaderLeft(object sender, MouseEventArgs e)
        {
            HideFloatingBoard();
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
                            WorkbookManager.EnableChaptersContextMenuItems(_mainWin._cmChapters, true, GameData.ContentType.MODEL_GAME, true);
                            _mainWin._cmChapters.IsOpen = true;
                            e.Handled = true;
                        }
                    }
                    else if (e.ChangedButton == MouseButton.Right)
                    {
                        WorkbookManager.LastClickedModelGameIndex = -1;
                        WorkbookManager.EnableChaptersContextMenuItems(_mainWin._cmChapters, true, GameData.ContentType.MODEL_GAME);
                        SelectChapter(chapterIndex, false);
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("Exception in EventModelGamesHeaderClicked(): " + ex.Message);
            }
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
                            WorkbookManager.EnableChaptersContextMenuItems(_mainWin._cmChapters, true, GameData.ContentType.EXERCISE, true);
                            _mainWin._cmChapters.IsOpen = true;
                            e.Handled = true;
                        }
                    }
                    else if (e.ChangedButton == MouseButton.Right)
                    {
                        WorkbookManager.LastClickedExerciseIndex = -1;
                        WorkbookManager.EnableChaptersContextMenuItems(_mainWin._cmChapters, true, GameData.ContentType.EXERCISE);
                        SelectChapter(chapterIndex, false);
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("Exception in EventModelGamesHeaderClicked(): " + ex.Message);
            }
        }

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

                Run r = (Run)e.Source;
                int chapterIndex = GetChapterIndexFromChildRun(r);
                Chapter chapter = WorkbookManager.SessionWorkbook.ActiveChapter;
                if (chapter.Index != chapterIndex)
                {
                    SelectChapter(chapterIndex, false);
                    chapter = WorkbookManager.SessionWorkbook.ActiveChapter;
                }

                int gameIndex = TextUtils.GetIdFromPrefixedString(r.Name);
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
                        WorkbookManager.EnableChaptersContextMenuItems(_mainWin._cmChapters, true, GameData.ContentType.MODEL_GAME);
                    }
                }

                RebuildChapterParagraph(chapter);
            }
            catch (Exception ex)
            {
                AppLog.Message("Exception in EventModelGameRunClicked(): " + ex.Message);
            }
        }

        /// <summary>
        /// Display Model Game's Thumbnail.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventModelGameRunHovered(object sender, MouseEventArgs e)
        {
            try
            {
                Run r = (Run)e.Source;

                Chapter chapter = GetChapterAndItemIndexFromRun(r, out int index);
                Article game = chapter.GetModelGameAtIndex(index);
                TreeNode thumb = game.Tree.GetThumbnail();

                ShowFloatingBoard(thumb, e);
            }
            catch
            {
            }
        }

        /// <summary>
        /// The mouse no longer hovers over the Model Game, so hide the floating board.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventModelGameRunLeft(object sender, MouseEventArgs e)
        {
            HideFloatingBoard();
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

                Run r = (Run)e.Source;
                int chapterIndex = GetChapterIndexFromChildRun(r);
                Chapter chapter = WorkbookManager.SessionWorkbook.ActiveChapter;
                if (chapter.Index != chapterIndex)
                {
                    SelectChapter(chapterIndex, false);
                    chapter = WorkbookManager.SessionWorkbook.ActiveChapter;
                }

                int exerciseIndex = TextUtils.GetIdFromPrefixedString(r.Name);
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
                        WorkbookManager.EnableChaptersContextMenuItems(_mainWin._cmChapters, true, GameData.ContentType.EXERCISE);
                    }
                }

                RebuildChapterParagraph(chapter);
            }
            catch (Exception ex)
            {
                AppLog.Message("Exception in EventExerciseRunClicked(): " + ex.Message);
            }
        }

        /// <summary>
        /// Display Exercise's Thumbnail.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventExerciseRunHovered(object sender, MouseEventArgs e)
        {
            try
            {
                Run r = (Run)e.Source;

                Chapter chapter = GetChapterAndItemIndexFromRun(r, out int index);
                Article exer = chapter.GetExerciseAtIndex(index);
                TreeNode thumb = exer.Tree.GetThumbnail();

                ShowFloatingBoard(thumb, e, exer.Tree.RootNode.ColorToMove);
            }
            catch
            {
            }
        }

        /// <summary>
        /// The mouse no longer hovers over the Exercise, so hide the floating board.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventExerciseRunLeft(object sender, MouseEventArgs e)
        {
            HideFloatingBoard();
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

                Run r = (Run)e.Source;
                int chapterIndex = TextUtils.GetIdFromPrefixedString(r.Name);
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

                Run r = (Run)e.Source;
                int chapterIndex = TextUtils.GetIdFromPrefixedString(r.Name);
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
