using GameTree;
using ChessPosition;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessForge
{
    /// <summary>
    /// Manages the process of processing multiple PGNs in parallel.
    /// </summary>
    public class BackgroundPgnProcessingManager
    {
        // number of parallel workers in the pool
        private int WORKERS_COUNT = 10;

        // state of the parsing multi-task
        private ProcessState _state;

        // list of background processors
        private List<BackgroundPgnProcessor> _workerPool;

        // number of games to process
        private int _gamesToProcess;

        // number of games currently being processed
        private int _gamesInProgress;

        // number of games processed
        private int _gamesCompleted;

        // index of the last scheduled game
        private int _lastScheduledGame = -1;

        // reference to the list of games being processed
        private ObservableCollection<GameData> _games;

        // used as a lock to access state variables
        private object _lockState = new object();

        /// <summary>
        /// Number of games not scheduled for parsing yet
        /// </summary>
        private int GamesNotStarted
        {
            get => _gamesToProcess - (_gamesInProgress + _gamesCompleted);
        }

        // list of VariationTrees sent to, and received from the workers 
        public List<VariationTree> _processedVariationTrees;

        /// <summary>
        /// Constructors. Sets the initial state
        /// </summary>
        public BackgroundPgnProcessingManager()
        {
            _state = ProcessState.NOT_STARTED;
        }

        /// <summary>
        /// Prepares the process of parsing games from the passed list.
        /// </summary>
        /// <param name="games"></param>
        public void Execute(ObservableCollection<GameData> games)
        {
            _state = ProcessState.RUNNING;
            _games = games;
            Initialize();

            if (_gamesToProcess == 0)
            {
                _state = ProcessState.FINISHED;
            }
            else
            {
                StartProcessing();
            }
        }

        /// <summary>
        /// Starts the processing.
        /// Kicks off the initial bunch of background workers.
        /// </summary>
        private void StartProcessing()
        {
            for (int i = 0; i < WORKERS_COUNT; i++)
            {
                if (i >= _games.Count)
                {
                    break;
                }

                KickoffWorker(i, i);
                lock (_lockState)
                {
                    _gamesInProgress++;
                }
            }
        }

        /// <summary>
        /// Starts a worker at the passed index for processing
        /// of the game at the passed index.
        /// </summary>
        /// <param name="workerIndex"></param>
        /// <param name="gameIndex"></param>
        private void KickoffWorker(int workerIndex, int gameIndex)
        {
            _workerPool[workerIndex].Run(gameIndex, _games[gameIndex].GameText, _processedVariationTrees[gameIndex]);
            _lastScheduledGame = gameIndex;
        }

        /// <summary>
        /// Starts the passed worker for processing
        /// of the game at the passed index.
        /// </summary>
        /// <param name="worker"></param>
        /// <param name="gameIndex"></param>
        private void KickoffWorker(BackgroundPgnProcessor worker, int gameIndex)
        {
            worker.Run(gameIndex, _games[gameIndex].GameText, _processedVariationTrees[gameIndex]);
            _lastScheduledGame = gameIndex;
        }

        /// <summary>
        /// Prepares data structures and performs parsing of the supplied list of
        /// PGN games.
        /// </summary>
        private void Initialize()
        {
            for (int i = 0; i < WORKERS_COUNT; i++)
            {
                _workerPool.Add(new BackgroundPgnProcessor(this));
            }

            lock (_lockState)
            {
                _gamesToProcess = _games.Count;
            }

            _processedVariationTrees = new List<VariationTree>(_gamesToProcess);
            for (int i = 0; i < _games.Count; i++)
            {
                _processedVariationTrees.Add(new VariationTree(GameData.ContentType.GENERIC));
            }
        }

        /// <summary>
        /// Called by the background workers to report completion of the work
        /// </summary>
        /// <param name="id"></param>
        public void JobFinished(int id)
        {
            BackgroundPgnProcessor processor = _workerPool.Find(x => x.GameIndex == id);
            if (processor != null)
            {
                _processedVariationTrees[id] = processor.DataObject.Tree;
                lock (_lockState)
                {
                    _gamesInProgress--;
                    _gamesCompleted++;
                    _games[id].IsProcessed = true;

                    if (_gamesCompleted >= _games.Count)
                    {
                        _state = ProcessState.FINISHED;
                    }
                }

                if (GamesNotStarted > 0)
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
        /// Check if there are any jobs still pending and if so
        /// take the next one and start the passed worker on it.
        /// </summary>
        /// <param name="processor"></param>
        private void ReuseWorker(BackgroundPgnProcessor processor)
        {
            if (GamesNotStarted > 0 && _state != ProcessState.CANCELED)
            {
                // find the first unproccesed index
                for (int i = _lastScheduledGame + 1; i < _games.Count; ++i)
                {
                    if (!_games[i].IsProcessed)
                    {
                        KickoffWorker(processor, i);
                    }
                }
            }
        }
    }
}
