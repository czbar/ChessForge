using GameTree;
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
        private static int WORKERS_COUNT = 10;
        
        // list of background processors
        private static List<BackgroundPgnProcessor> _workerPool;

        // number of games to process
        private static int _gamesToProcess;

        // number of games currently being processed
        private static int _gamesInProgress;

        // number of games processed
        private static int _gamesCompleted;

        // indicates whether the class has been initialized
        private static bool _isInitialized = false;

        // list of variation trees sent to, and received from the workers 
        public static List<VariationTree> _processedVariationTrees;

        /// <summary>
        /// Prepares data structures and performs parsing of the supplied list of
        /// PGN games.
        /// </summary>
        /// <param name="games"></param>
        public static void ParseGameDataList(ObservableCollection<GameData> games)
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            _gamesToProcess = games.Count;

            if (_processedVariationTrees != null)
            {
                _processedVariationTrees.Clear();
            }

            _processedVariationTrees = new List<VariationTree>(_gamesToProcess);

            for (int i = 0; i < games.Count; i++)
            {
                _processedVariationTrees.Add(new VariationTree(GameData.ContentType.GENERIC));
            }
        }

        /// <summary>
        /// Called by the background workers to report completion of the work
        /// </summary>
        /// <param name="id"></param>
        public static void JobFinished(int id)
        {
            BackgroundPgnProcessor processor = _workerPool.Find( x => x.ProcessorId == id );
            if (processor != null)
            {
                _processedVariationTrees[id] = processor.DataObject.Tree;
                _gamesCompleted++;
                ReuseWorker(processor);
            }
            else
            {
                // TODO: something serioulsy wrong here
            }
        }

        /// <summary>
        /// Cancels all pending processes and restes the state.
        /// </summary>
        public static void CancelAll()
        {
            // TODO: implement
        }

        /// <summary>
        /// Initializes the pool of worker objects.
        /// This needs to be done only once in the lifetime
        /// of the app.
        /// </summary>
        private static void Initialize()
        {
            for (int i = 0; i < WORKERS_COUNT; i++)
            {
                _workerPool.Add(new BackgroundPgnProcessor());
            }
            _isInitialized = true;
        }

        /// <summary>
        /// Check if there are any jobs still pending and if so
        /// take the next one and start the passed worker on it.
        /// </summary>
        /// <param name="processor"></param>
        private static void ReuseWorker(BackgroundPgnProcessor processor)
        {
            // TODO: implement
        }
    }
}
