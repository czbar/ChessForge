using GameTree;
using System.Timers;

namespace ChessForge
{
    /// <summary>
    /// Manages various actions and states in response to 
    /// a PULSE timer event.
    /// </summary>
    public class PulseManager
    {
        // how many pulses before clearing the BringChapterIntoView request
        private static readonly int BRING_CHAPTER_INTO_VIEW_COUNT_DELAY = 8;

        // how many pulses before clearing the BringMoveIntoView request
        private static readonly int BRING_SELECTED_MOVE_INTO_VIEW_COUNT_DELAY = 2;

        // how many pulses before clearing the BringArticleIntoView request
        private static readonly int BRING_ARTICLE_INTO_VIEW_COUNT_DELAY = 4;

        // index of the chapter to bring into view
        private static int _chapterIndexToBringIntoView = -1;

        // identifies the article that needs to be brought into view
        private static ArticleIdentifier _articleToBringIntoView = new ArticleIdentifier();

        // counter monitoring delay on bring chapter into view
        private static int _bringChapterIntoViewCounter;

        // counter monitoring delay on bring move into view
        private static int _bringSelectedMoveIntoViewCounter;

        // counter monitoring delay on bring chapter into view
        private static int _bringArticleIntoViewCounter;

        // whether counting until bringing the selected run into view
        private static bool _bringSelectedRunIntoView = false;

        // counter monitoring the pause in pulse processing
        private static int _pauseCounter = 0;

        /// <summary>
        /// Index of the chapter to bring into view.
        /// </summary>
        public static int ChapterIndexToBringIntoView
        {
            set => _chapterIndexToBringIntoView = value;
        }

        /// <summary>
        /// Sets the flag for bringing the selected run into view.
        /// </summary>
        public static void BringSelectedRunIntoView()
        {
            _bringSelectedRunIntoView = true;
        }

        // pulse event busy flag to prevent re-entrance
        private static bool _pulseBusy = false;

        /// <summary>
        /// Handles the PULSE timer event.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        public static void PulseEventHandler(object source, ElapsedEventArgs e)
        {
            // if we are already busy, do not re-enter
            if (_pulseBusy || _pauseCounter > 0)
            {
                if (_pauseCounter > 0)
                {
                    _pauseCounter--;
                }

                return;
            }

            // set busy flag to prevent re-entrance
            _pulseBusy = true;

            WebAccessManager.UpdateWebAccess();
            UpdateEvaluationBar();
            if (_chapterIndexToBringIntoView >= 0)
            {
                AppState.MainWin.BringChapterIntoView(_chapterIndexToBringIntoView);

                if (_bringChapterIntoViewCounter++ > BRING_CHAPTER_INTO_VIEW_COUNT_DELAY)
                {
                    _bringChapterIntoViewCounter = 0;
                    _chapterIndexToBringIntoView = -1;
                }
            }

            if (_bringSelectedRunIntoView)
            {
                AppState.MainWin.Dispatcher.Invoke(() =>
                {
                    AppState.MainWin.BringSelectedRunIntoView();
                });

                if (_bringSelectedMoveIntoViewCounter++ > BRING_SELECTED_MOVE_INTO_VIEW_COUNT_DELAY)
                {
                    _bringSelectedMoveIntoViewCounter = 0;
                    _bringSelectedRunIntoView = false;
                }
            }

            if (_articleToBringIntoView.ArticleIndex >= 0)
            {
                AppState.MainWin.BringArticleIntoView(_articleToBringIntoView.ChapterIndex,
                    _articleToBringIntoView.ContentType,
                    _articleToBringIntoView.ArticleIndex);

                if (_bringArticleIntoViewCounter++ > BRING_ARTICLE_INTO_VIEW_COUNT_DELAY)
                {

                    _bringArticleIntoViewCounter = 0;
                    _articleToBringIntoView.ArticleIndex = -1;
                }
            }

            // allow re-entrance now that we are done
            _pulseBusy = false;
        }

        /// <summary>
        /// Sets the article that will be brought into view on the next pulse.
        /// </summary>
        /// <param name="chapterIndex"></param>
        /// <param name="contentType"></param>
        /// <param name="articleIndex"></param>
        public static void SetArticleToBringIntoView(int chapterIndex, GameData.ContentType contentType, int articleIndex)
        {
            _articleToBringIntoView.ChapterIndex = chapterIndex;
            _articleToBringIntoView.ContentType = contentType;
            _articleToBringIntoView.ArticleIndex = articleIndex;
        }

        /// <summary>
        /// Sets the value of the pause counter.
        /// It will prevent processing of the PULSE events for a number of pulses.
        /// </summary>
        /// <param name="count"></param>
        public static void SetPauseCounter(int count)
        {
            _pauseCounter = count;
        }

        /// <summary>
        /// Updates the position of the evaluation bar.
        /// </summary>
        private static void UpdateEvaluationBar()
        {
            AppState.MainWin.Dispatcher.Invoke(() =>
            {
                if (AppState.IsVariationTreeTabType
                || TrainingSession.IsTrainingInProgress
                   && (EvaluationManager.CurrentMode == EvaluationManager.Mode.LINE || EvaluationManager.CurrentMode == EvaluationManager.Mode.CONTINUOUS)
               )
                {
                    TreeNode nd = null;
                    if (TrainingSession.IsTrainingInProgress)
                    {
                        nd = EvaluationManager.GetEvaluatedNode(out _);
                    }
                    else if (AppState.MainWin.ActiveTreeView != null)
                    {
                        nd = AppState.MainWin.ActiveTreeView.GetSelectedNode();
                        if (nd == null && AppState.MainWin.ActiveVariationTree != null && AppState.MainWin.ActiveVariationTree.Nodes.Count > 0)
                        {
                            nd = AppState.MainWin.ActiveVariationTree.Nodes[0];
                        }
                    }

                    EvaluationBar.ShowEvaluation(nd);
                }
                else
                {
                    EvaluationBar.Show(false);
                }
            });
        }

    }

    /// <summary>
    /// Encapsulates attributes identifying a chapter in the Workbook.
    /// </summary>
    class ArticleIdentifier
    {
        public int ChapterIndex;
        public GameData.ContentType ContentType;
        public int ArticleIndex = -1;
    }
}
