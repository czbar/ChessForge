using GameTree;
using ChessPosition;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ChessForge
{
    /// <summary>
    /// Manages the process of processing multiple PGNs in parallel.
    /// </summary>
    public class BackgroundPgnProcessingManager
    {
        // number of parallel workers in the pool
        private int WORKERS_COUNT = 8;

        // state of the parsing multi-task
        private ProcessState _state;

        // list of background processors
        private List<BackgroundPgnProcessor> _workerPool = new List<BackgroundPgnProcessor>();

        // Text of errors returned by the workers
        private List<string> _errors = new List<string>();

        // number of articles to process
        private int _articlesToProcess;

        // number of articles currently being processed
        private int _articlesInProgress;

        // number of articles processed
        private int _articlesCompleted;

        // index of the last scheduled article
        private int _lastScheduledArticle = -1;

        // reference to the list of articles being processed
        private ObservableCollection<GameData> _rawArticles;

        // used as a lock to access state variables
        private object _lockState = new object();

        //Workbook owning this object
        private Workbook _parent;

        /// <summary>
        /// Number of articles not scheduled for parsing yet
        /// </summary>
        private int ArticlesNotStarted
        {
            get => _articlesToProcess - (_articlesInProgress + _articlesCompleted);
        }

        /// <summary>
        /// Accessor to the Game processing status.
        /// </summary>
        public ProcessState State { get => _state; }

        // list of Articles to be processed by the background workers
        public List<Article> _articleList;

        /// <summary>
        /// Constructors. Sets the initial state.
        /// </summary>
        public BackgroundPgnProcessingManager(Workbook parent)
        {
            _state = ProcessState.NOT_STARTED;
            _parent = parent;
        }

        /// <summary>
        /// Prepares the process of parsing articles from the passed list.
        /// </summary>
        /// <param name="articleDataList"></param>
        public void Execute(ref ObservableCollection<GameData> articleDataList, ref List<Article> articleList)
        {
            _state = ProcessState.RUNNING;
            _rawArticles = articleDataList;
            _articleList = articleList;
            Initialize();

            if (_articlesToProcess == 0)
            {
                _state = ProcessState.FINISHED;
            }
            else
            {
                StartProcessing();
            }
        }

        /// <summary>
        /// Called by the background workers to report completion of the work
        /// </summary>
        /// <param name="articleIndex"></param>
        public void JobFinished(int articleIndex, string errorText)
        {
            BackgroundPgnProcessor processor = _workerPool.Find(x => x.ArticleIndex == articleIndex);
            if (processor != null)
            {
                _articleList[articleIndex].Tree = processor.DataObject.Tree;
                _articleList[articleIndex].IsReady = true;
                _errors[articleIndex] = errorText;

                UpdateVariablesOnJobFinish(articleIndex);

                if (ArticlesNotStarted > 0)
                {
                    ReuseWorker(processor);
                }

                if (_articlesCompleted % 500 == 0)
                {
                    AppState.MainWin.BoardCommentBox.ReadingItems(_articlesCompleted, _articleList.Count);
                }
            }
            else
            {
                // this should never happen and we can't do anything sensible here. Log the error
                AppLog.Message("ERROR: BackgroundPgnProcessingManager:JobFinished failed to identify the background worker");
            }
        }

        /// <summary>
        /// Resets the state so that we no longer accept jobs.
        /// </summary>
        public void CancelAll()
        {
            if (_state != ProcessState.FINISHED)
            {
                _state = ProcessState.CANCELED;
                AppState.MainWin.BoardCommentBox.ShowTabHints();
            }
        }

        /// <summary>
        /// Serves the request to synchronously process the article's text.
        /// In order not to risk upsetting the integrity of the ongoing background
        /// processing, a copy of the article will made, processed and returned.
        /// </summary>
        /// <param name="article"></param>
        /// <returns></returns>
        public Article ProcessArticleSync(Article article)
        {
            Article retArticle = article;

            int index = GetArticleIndex(article);

            // we will get -1 if this is a newly created study tree that should not be processed
            if (index >= 0)
            {
                try
                {
                    if (!_articleList[index].IsReady)
                    {
                        retArticle = new Article(article.Tree);
                        retArticle.Tree.Header = article.Tree.Header.CloneMe(true);
                        VariationTree tree = retArticle.Tree;

                        // check if fen needs to be set
                        string fen = tree.Header.IsExercise() ? tree.Header.GetFenString() : null;
                        PgnGameParser pp = new PgnGameParser(_rawArticles[index].GameText, tree, fen, false);
                    }
                }
                catch
                {
                }

                retArticle.IsReady = true;
            }

            return retArticle;
        }

        /// <summary>
        /// Starts the processing.
        /// Kicks off the initial bunch of background workers.
        /// </summary>
        private void StartProcessing()
        {
            for (int i = 0; i < WORKERS_COUNT; i++)
            {
                if (i >= _rawArticles.Count)
                {
                    break;
                }

                bool res = KickoffWorker(i, i);
                lock (_lockState)
                {
                    _articlesInProgress++;
                    if (!res)
                    {
                        UpdateVariablesOnJobFinish(i);
                    }
                }
            }
        }

        /// <summary>
        /// Starts a worker at the passed index for processing
        /// of the article at the passed index.
        /// </summary>
        /// <param name="workerIndex"></param>
        /// <param name="articleIndex"></param>
        /// <returns>true if the run was started, false if the argument was null</returns>
        private bool KickoffWorker(int workerIndex, int articleIndex)
        {
            bool res = false;

            if (_articleList[articleIndex] != null)
            {
                _workerPool[workerIndex].Run(articleIndex, _rawArticles[articleIndex].GameText, ref _articleList[articleIndex].Tree);
                res = true;
            }
            _lastScheduledArticle = articleIndex;

            return res;
        }

        /// <summary>
        /// Starts the passed worker for processing
        /// of the article at the passed index.
        /// </summary>
        /// <param name="worker"></param>
        /// <param name="articleIndex">this is the index in the output list of articles.</param>
        /// <returns>true if the run was started, false if the argument was null</returns>
        private bool KickoffWorker(BackgroundPgnProcessor worker, int articleIndex)
        {
            bool res = false;

            if (_articleList[articleIndex] != null)
            {
                worker.Run(articleIndex, _rawArticles[articleIndex].GameText, ref _articleList[articleIndex].Tree);
                res = true;
            }
            _lastScheduledArticle = articleIndex;

            return res;
        }

        /// <summary>
        /// Check if there are any jobs still pending and if so
        /// take the next one and start the passed worker on it.
        /// </summary>
        /// <param name="processor"></param>
        private void ReuseWorker(BackgroundPgnProcessor processor)
        {
            if (ArticlesNotStarted > 0 && _state != ProcessState.CANCELED)
            {
                // find the first unproccesed index
                for (int i = _lastScheduledArticle + 1; i < _rawArticles.Count; ++i)
                {
                    if (!_rawArticles[i].IsProcessed)
                    {
                        _lastScheduledArticle = i;
                        bool res = KickoffWorker(processor, i);
                        _articlesInProgress++;
                        if (!res)
                        {
                            UpdateVariablesOnJobFinish(i);
                        }
                        break;
                    }
                }
            }
        }


        /// <summary>
        /// Finds the passed article in the list of articles.
        /// </summary>
        /// <param name="article"></param>
        /// <returns></returns>
        private int GetArticleIndex(Article article)
        {
            int index = -1;

            //_articleList will be null if we are creating a new Workbook
            if (_articleList != null)
            {
                for (int i = 0; i < _articleList.Count; i++)
                {
                    if (article == _articleList[i])
                    {
                        index = i;
                        break;
                    }
                }
            }

            return index;
        }

        /// <summary>
        /// Prepares data structures and performs parsing of the supplied list of
        /// PGN articles.
        /// </summary>
        private void Initialize()
        {
            for (int i = 0; i < WORKERS_COUNT; i++)
            {
                _workerPool.Add(new BackgroundPgnProcessor(this));
            }

            lock (_lockState)
            {
                _articlesToProcess = _rawArticles.Count;
            }

            for (int i = 0; i < _rawArticles.Count; i++)
            {
                _errors.Add(null);
            }
        }

        /// <summary>
        /// Updates variables upon return from a worker.
        /// </summary>
        /// <param name="articleIndex"></param>
        private void UpdateVariablesOnJobFinish(int articleIndex)
        {
            lock (_lockState)
            {
                try
                {
                    _articlesInProgress--;
                    _articlesCompleted++;
                    _rawArticles[articleIndex].IsProcessed = true;

                    if (_articlesCompleted >= _rawArticles.Count)
                    {
                        _state = ProcessState.FINISHED;
                        ReportErrors();
                        AppState.MainWin.BoardCommentBox.ShowTabHints();
                    }
                }
                catch (Exception ex)
                {
                    AppLog.Message("UpdateVariablesOnJobFinish()", ex);
                }
            }
        }

        /// <summary>
        /// If any errors were detected, shows them in a dialog.
        /// </summary>
        private void ReportErrors()
        {
            StringBuilder sbErrors = new StringBuilder();
            int errorCount = 0;
            int chapterIndex = -1;

            for (int i = 0; i < _errors.Count; i++)
            {
                if (_errors[i] != null)
                {
                    if (_articleList[i].ContentType == GameData.ContentType.STUDY_TREE)
                    {
                        chapterIndex++;
                    }

                    if (!string.IsNullOrEmpty(_errors[i]))
                    {
                        sbErrors.AppendLine(WorkbookManager.BuildGameParseErrorText(chapterIndex, i + 1, _rawArticles[i], _errors[i]));
                        errorCount++;
                    }
                }
            }

            if (errorCount > 0)
            {
                WorkbookManager.ShowPgnProcessingErrors(Properties.Resources.DlgParseErrors, ref sbErrors);
            }
        }

    }
}
