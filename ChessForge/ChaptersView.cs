using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;
using System.Windows.Documents;
using ChessPosition;
using System.Collections.ObjectModel;
using System.Windows.Input;
using ChessPosition.GameTree;

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
            Document.PageWidth = 2000; // prevent word wrap
            _dictChapterParas.Clear();

            AddNewParagraphToDoc(STYLE_WORKBOOK_TITLE, WorkbookManager.SessionWorkbook.Title);

            foreach (Chapter chapter in WorkbookManager.SessionWorkbook.Chapters)
            {
                Paragraph para = BuildChapterParagraph(chapter);
                Document.Blocks.Add(para);
                _dictChapterParas[chapter.Id] = para;
            }

            HighlightActiveChapter();
        }

        /// <summary>
        /// Brings the title line of the chapter into view.
        /// </summary>
        /// <param name="chapterId"></param>
        public void BringChapterIntoView(int chapterId)
        {
            Run rChapter = FindChapterTitleRun(chapterId);
            rChapter?.BringIntoView();
        }

        /// <summary>
        /// Brings the the title line of the model game or exercise into view.
        /// </summary>
        /// <param name="chapterId"></param>
        /// <param name="contentType"></param>
        /// <param name="index"></param>
        public void BringGameUnitIntoView(int chapterId, GameData.ContentType contentType, int index)
        {
            Paragraph paraChapter = FindChapterParagraph(chapterId);
            if (paraChapter != null)
            {
                Run r = FindGameUnitRunInParagraph(paraChapter, contentType, index);
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
                int activeChapterId = WorkbookManager.SessionWorkbook.ActiveChapter.Id;
                // iterate over the runs with chapter name and set the font for the active one to bold
                foreach (Block b in Document.Blocks)
                {
                    if (b is Paragraph)
                    {
                        foreach (Inline r in ((Paragraph)b).Inlines)
                        {
                            int chapterId = GetNodeIdFromRunName((r as Run).Name, _run_chapter_title_);
                            if (chapterId >= 0)
                            {
                                if (chapterId == activeChapterId)
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
                    if (id == chapter.Id)
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
                    _dictChapterParas[chapter.Id] = para;
                }
                else
                {
                    para.Inlines.Clear();
                }

                para.Name = _par_chapter_ + chapter.Id.ToString();

                char expandCollapse = chapter.IsViewExpanded ? Constants.CharCollapse : Constants.CharExpand;
                Run rExpandChar = CreateRun(STYLE_CHAPTER_TITLE, expandCollapse.ToString() + " ", true);
                rExpandChar.Name = _run_chapter_expand_char_ + chapter.Id.ToString();
                rExpandChar.MouseDown += EventChapterExpandSymbolClicked;
                para.Inlines.Add(rExpandChar);

                string chapterNo = (WorkbookManager.SessionWorkbook.GetChapterIndex(chapter) + 1).ToString();
                Run rTitle = CreateRun(STYLE_CHAPTER_TITLE, chapterNo + ". " + chapter.GetTitle(), true);
                if (chapter.Id == WorkbookManager.SessionWorkbook.ActiveChapter.Id)
                {
                    rTitle.FontWeight = FontWeights.Bold;
                }
                rTitle.Name = _run_chapter_title_ + chapter.Id.ToString();
                rTitle.MouseDown += EventChapterHeaderClicked;
                para.Inlines.Add(rTitle);

                if (chapter.IsViewExpanded)
                {
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
        /// <param name="chapterId"></param>
        /// <returns></returns>
        private Paragraph FindChapterParagraph(int chapterId)
        {
            Paragraph paraChapter = null;
            string paraName = _par_chapter_ + chapterId.ToString();
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
        /// Finds the Run for a game unit with at a given index in a given chapter.
        /// </summary>
        /// <param name="paraChapter"></param>
        /// <param name="contentType"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        private Run FindGameUnitRunInParagraph(Paragraph paraChapter, GameData.ContentType contentType, int index)
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
        /// <param name="chapterId"></param>
        /// <returns></returns>
        private Run FindChapterTitleRun(int chapterId)
        {
            Run rChapter = null;
            string runName = _run_chapter_title_ + chapterId.ToString();
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
            para.Inlines.Add(new Run("\n"));
            Run r = CreateRun(STYLE_SUBHEADER, SUBHEADER_INDENT + "Study Tree", true);
            r.Name = _run_study_tree_ + chapter.Id.ToString();
            r.MouseDown += EventStudyTreeHeaderClicked;
            para.Inlines.Add(r);
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
            para.Inlines.Add(new Run("\n"));
            para.Inlines.Add(CreateRun(STYLE_SUBHEADER, SUBHEADER_INDENT, true));
            InsertExpandCollapseSymbolRun(para, _run_model_games_expand_char_, chapter.Id, GameData.ContentType.MODEL_GAME, chapter.IsModelGamesListExpanded, chapter.HasAnyModelGame);
            Run r = CreateRun(STYLE_SUBHEADER, "Model Games", true);
            r.Name = _run_model_games_header_ + chapter.Id.ToString();
            r.MouseDown += EventModelGamesHeaderClicked;
            para.Inlines.Add(r);

            if (chapter.IsModelGamesListExpanded)
            {
                for (int i = 0; i < chapter.ModelGames.Count; i++)
                {
                    para.Inlines.Add(new Run("\n"));
                    Run rGame = CreateRun(STYLE_SUBHEADER, SUBHEADER_DOUBLE_INDENT + (i + 1).ToString() + ". " + chapter.ModelGames[i].Tree.Header.BuildGameHeaderLine(), true);
                    rGame.Name = _run_model_game_ + i.ToString();
                    rGame.MouseDown += EventModelGameRunClicked;
                    if (i == chapter.ActiveModelGameIndex && chapter == WorkbookManager.SessionWorkbook.ActiveChapter)
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
            InsertExpandCollapseSymbolRun(para, _run_exercises_expand_char_, chapter.Id, GameData.ContentType.EXERCISE, chapter.IsExercisesListExpanded, chapter.HasAnyExercise);
            Run r = CreateRun(STYLE_SUBHEADER, "Exercises", true);
            r.Name = _run_exercises_header_ + chapter.Id.ToString();
            r.MouseDown += EventExercisesHeaderClicked;
            para.Inlines.Add(r);

            if (chapter.IsExercisesListExpanded)
            {
                for (int i = 0; i < chapter.Exercises.Count; i++)
                {
                    para.Inlines.Add(new Run("\n"));
                    Run rGame = CreateRun(STYLE_SUBHEADER, SUBHEADER_DOUBLE_INDENT + (i + 1).ToString() + ". " + chapter.Exercises[i].Tree.Header.BuildGameHeaderLine(true), true);
                    rGame.Name = _run_exercise_ + i.ToString();
                    rGame.MouseDown += EventExerciseRunClicked;
                    if (i == chapter.ActiveExerciseIndex && chapter == WorkbookManager.SessionWorkbook.ActiveChapter)
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
        private Run InsertExpandCollapseSymbolRun(Paragraph para, string prefix, int chapterId, GameData.ContentType contentType, bool isExpanded, bool hasContent)
        {
            if (hasContent)
            {
                char expandCollapse = isExpanded ? Constants.CharCollapse : Constants.CharExpand;
                Run rExpandChar = CreateRun(STYLE_SUBHEADER, expandCollapse.ToString() + " ", true);
                rExpandChar.Name = prefix + chapterId;
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
        /// Event handler invoked when a Chapter Run was clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventChapterHeaderClicked(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Run r = (Run)e.Source;
                int chapterId = TextUtils.GetIdFromPrefixedString(r.Name);
                if (chapterId >= 0)
                {
                    Chapter chapter = WorkbookManager.SessionWorkbook.GetChapterById(chapterId);
                    WorkbookManager.LastClickedChapterId = chapterId;

                    if (e.ChangedButton == MouseButton.Left)
                    {
                        if (e.ClickCount == 2)
                        {
                            SelectChapter(chapterId, true);
                        }
                        else
                        {
                            ExpandChapterList(chapter);
                            SelectChapter(chapterId, false);
                        }
                    }
                    else if (e.ChangedButton == MouseButton.Right)
                    {
                        WorkbookManager.EnableChaptersContextMenuItems(_mainWin._cmChapters, true, GameData.ContentType.GENERIC);
                        // TODO: this rebuilds the Study Tree. Performance!
                        SelectChapter(chapterId, false);
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("Exception in EventChapterRunClicked(): " + ex.Message);
            }
        }

        /// <summary>
        /// An expand/collapse character for the chapter was clicked.
        /// Establish which chapter this is for, check its expand/collapse status and flip it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventChapterExpandSymbolClicked(object sender, MouseButtonEventArgs e)
        {
            EventChapterHeaderClicked(sender, e);
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
                Run r = (Run)e.Source;
                int chapterId = TextUtils.GetIdFromPrefixedString(r.Name);
                Chapter chapter = WorkbookManager.SessionWorkbook.ActiveChapter;

                if (chapter.Id != chapterId)
                {
                    SelectChapter(chapterId, false);
                    chapter = WorkbookManager.SessionWorkbook.ActiveChapter;
                }

                if (chapter != null)
                {
                    WorkbookManager.LastClickedChapterId = chapterId;
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
                        SelectChapter(chapterId, false);
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
                Run r = (Run)e.Source;
                int chapterId = TextUtils.GetIdFromPrefixedString(r.Name);
                Chapter chapter = WorkbookManager.SessionWorkbook.ActiveChapter;

                if (chapter.Id != chapterId)
                {
                    SelectChapter(chapterId, false);
                    chapter = WorkbookManager.SessionWorkbook.ActiveChapter;
                }

                if (chapter != null)
                {
                    WorkbookManager.LastClickedChapterId = chapterId;
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
                        SelectChapter(chapterId, false);
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("Exception in EventModelGamesHeaderClicked(): " + ex.Message);
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
                Run r = (Run)e.Source;
                int chapterId = TextUtils.GetIdFromPrefixedString(r.Name);
                if (chapterId >= 0)
                {
                    Chapter chapter = WorkbookManager.SessionWorkbook.GetChapterById(chapterId);
                    WorkbookManager.LastClickedChapterId = chapterId;
                    if (e.ChangedButton == MouseButton.Left)
                    {
                        SelectChapter(chapterId, true);
                    }
                    else if (e.ChangedButton == MouseButton.Right)
                    {
                        WorkbookManager.EnableChaptersContextMenuItems(_mainWin._cmChapters, true, GameData.ContentType.STUDY_TREE);
                        SelectChapter(chapterId, false);
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("Exception in EventStudyTreeRunClicked(): " + ex.Message);
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
                Run r = (Run)e.Source;
                int chapterId = GetChapterIdFromChildRun(r);
                Chapter chapter = WorkbookManager.SessionWorkbook.ActiveChapter;
                if (chapter.Id != chapterId)
                {
                    SelectChapter(chapterId, false);
                    chapter = WorkbookManager.SessionWorkbook.ActiveChapter;
                }

                int gameIndex = TextUtils.GetIdFromPrefixedString(r.Name);
                WorkbookManager.LastClickedModelGameIndex = gameIndex;
                WorkbookManager.SessionWorkbook.ActiveChapter.ActiveModelGameIndex = gameIndex;

                if (chapter != null && gameIndex >= 0 && gameIndex < chapter.ModelGames.Count)
                {
                    if (e.ChangedButton == MouseButton.Left)
                    {
                        _mainWin.SelectModelGame(gameIndex, true);
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
        /// Event handler invoked when an Exercise Run was clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventExerciseRunClicked(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Run r = (Run)e.Source;
                int chapterId = GetChapterIdFromChildRun(r);
                Chapter chapter = WorkbookManager.SessionWorkbook.ActiveChapter;
                if (chapter.Id != chapterId)
                {
                    SelectChapter(chapterId, false);
                    chapter = WorkbookManager.SessionWorkbook.ActiveChapter;
                }

                int exerciseIndex = TextUtils.GetIdFromPrefixedString(r.Name);
                WorkbookManager.LastClickedExerciseIndex = exerciseIndex;
                WorkbookManager.SessionWorkbook.ActiveChapter.ActiveExerciseIndex = exerciseIndex;

                if (chapter != null && exerciseIndex >= 0 && exerciseIndex < chapter.Exercises.Count)
                {
                    if (e.ChangedButton == MouseButton.Left)
                    {
                        _mainWin.SelectExercise(exerciseIndex, true);
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
        /// An expand/collapse character on the Model Games list was clicked.
        /// Establish which chapter this is for, check its expand/collapse status and flip it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventModelGamesExpandSymbolClicked(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Run r = (Run)e.Source;
                int chapterId = TextUtils.GetIdFromPrefixedString(r.Name);
                Chapter chapter = WorkbookManager.SessionWorkbook.GetChapterById(chapterId);
                if (chapter.Id != WorkbookManager.SessionWorkbook.ActiveChapter.Id)
                {
                    SelectChapter(chapterId, false);
                }
                chapter.IsModelGamesListExpanded = !chapter.IsModelGamesListExpanded;
                BuildChapterParagraph(chapter, _dictChapterParas[chapter.Id]);
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
                Run r = (Run)e.Source;
                int chapterId = TextUtils.GetIdFromPrefixedString(r.Name);
                Chapter chapter = WorkbookManager.SessionWorkbook.GetChapterById(chapterId);
                if (chapter.Id != WorkbookManager.SessionWorkbook.ActiveChapter.Id)
                {
                    SelectChapter(chapterId, false);
                }
                chapter.IsExercisesListExpanded = !chapter.IsExercisesListExpanded;
                BuildChapterParagraph(chapter, _dictChapterParas[chapter.Id]);
            }
            catch (Exception ex)
            {
                AppLog.Message("Exception in EventExercisesExpandSymbolClicked(): " + ex.Message);
            }
        }

        /// <summary>
        /// Expands or collapses the list of chapters depending
        /// on its current state.
        /// </summary>
        /// <param name="chapter"></param>
        private void ExpandChapterList(Chapter chapter)
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

            BuildChapterParagraph(chapter, _dictChapterParas[chapter.Id]);
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
                _runToBringToView = new ChaptersViewRunAttrs(chapter.Id, GameData.ContentType.MODEL_GAME);
            }

            BuildChapterParagraph(chapter, _dictChapterParas[chapter.Id]);
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
                _runToBringToView = new ChaptersViewRunAttrs(chapter.Id, GameData.ContentType.EXERCISE);
            }

            BuildChapterParagraph(chapter, _dictChapterParas[chapter.Id]);
        }

        /// <summary>
        /// Gets the ID from the parent paragraph identifying
        /// the chapter the passed Run belongs in.
        /// </summary>
        /// <returns></returns>
        private int GetChapterIdFromChildRun(Run r)
        {
            try
            {
                Paragraph para = r.Parent as Paragraph;
                return TextUtils.GetIdFromPrefixedString(para.Name);
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

            Paragraph para = GetParagraph(attrs.ChapterId);
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
        /// <param name="chapterId"></param>
        /// <returns></returns>
        private Paragraph GetParagraph(int chapterId)
        {
            Paragraph para = null;
            foreach (Block block in Document.Blocks)
            {
                if (block is Paragraph)
                {
                    int id = TextUtils.GetIdFromPrefixedString(block.Name);
                    if (id == chapterId)
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
        /// <param name="chapterId"></param>
        /// <param name="focusOnStudyTree"></param>
        private void SelectChapter(int chapterId, bool focusOnStudyTree)
        {
            Chapter prevActiveChapter = WorkbookManager.SessionWorkbook.ActiveChapter;

            _mainWin.SelectChapterById(chapterId, focusOnStudyTree);

            if (prevActiveChapter.Id != chapterId)
            {
                BuildChapterParagraph(prevActiveChapter, _dictChapterParas[prevActiveChapter.Id]);
            }
        }

    }
}
