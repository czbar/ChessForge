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
        public void JobFinished(int articleIndex)
        {
            BackgroundPgnProcessor processor = _workerPool.Find(x => x.ArticleIndex == articleIndex);
            if (processor != null)
            {
                _articleList[articleIndex].Tree = processor.DataObject.Tree;
                _articleList[articleIndex].IsReady = true;

                UpdateVariablesOnJobFinish(articleIndex);

                if (ArticlesNotStarted > 0)
                {
                    ReuseWorker(processor);
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
            _state = ProcessState.CANCELED;
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
        }

        /// <summary>
        /// Updates variables upon return from a worker.
        /// </summary>
        /// <param name="articleIndex"></param>
        private void UpdateVariablesOnJobFinish(int articleIndex)
        {
            lock (_lockState)
            {
                _articlesInProgress--;
                _articlesCompleted++;
                _rawArticles[articleIndex].IsProcessed = true;

                if (_articlesCompleted >= _rawArticles.Count)
                {
                    _state = ProcessState.FINISHED;
                }
            }
        }

    }
}
