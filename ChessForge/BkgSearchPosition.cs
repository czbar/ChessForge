using ChessPosition;
using GameTree;
using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;

namespace ChessForge
{
    /// <summary>
    /// Encapsulates a background process that processes a GameData object
    /// to build an Article object
    /// </summary>
    public class BkgSearchPosition
    {
        // parent object owning this one
        private BkgSearchPositionManager _parent;

        // the background worker object controlled from this object
        private BackgroundWorker _worker;

        // data object to pass to and from the background worker
        private BkgSearchPositionData _dataObject;

        /// <summary>
        /// Returns id that this processor received from the Manager.
        /// </summary>
        public int FileIndex
        {
            get => _dataObject == null ? -1 : _dataObject.FileIndex;
        }

        // current state of the background worker
        private ProcessState _workerState;

        // the time the process was started in ticks
        private long _startTime;

        /// <summary>
        /// Public accessor the DataObject
        /// </summary>
        public BkgSearchPositionData DataObject
        { get { return _dataObject; } }

        /// <summary>
        // The time the process was started in Ticks.
        /// </summary>
        public long StartTime
        {
            get => _startTime;
        }

        /// <summary>
        /// The current state of the background worker
        /// </summary>
        public ProcessState WorkerState
        {
            get { return _workerState; }
        }

        /// <summary>
        /// Create a new background worker object
        /// </summary>
        public BkgSearchPosition(BkgSearchPositionManager parent)
        {
            _parent = parent;
            _workerState = ProcessState.NOT_STARTED;

            _worker = new BackgroundWorker();
            _worker.WorkerReportsProgress = false;
            _worker.DoWork += DoWork;
            _worker.RunWorkerCompleted += RunWorkerCompleted;
            _parent = parent;
        }

        /// <summary>
        /// Called by the client to start the backgound process.
        /// Parses a file containing multiple games and builds a variation tree for each game. 
        /// Looks for the searched position in each.
        /// Finishes as soon as the position is found in a game or all games have been processed.
        /// </summary>
        /// <param name="articleText"></param>
        /// <param name="tree"></param>
        /// <param name="dummyRun">Of true then the requested article has already been processed so shortcircuit this method.</param>
        /// <param name="fen"></param>
        public void Run(int fileIndex, string filePath)
        {
            _startTime = DateTime.Now.Ticks;
            _workerState = ProcessState.RUNNING;
            try
            {
                _dataObject = new BkgSearchPositionData();
                _dataObject.FileIndex = fileIndex;
                _dataObject.FilePath = filePath;
                _worker.RunWorkerAsync(_dataObject);
            }
            catch (Exception ex)
            {
                _workerState = ProcessState.UNKNOWN;
                AppLog.Message("BkgSearchPosition Run()", ex);
            }
        }

        /// <summary>
        /// Performs background parsing of the passed text.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DoWork(object sender, DoWorkEventArgs e)
        {
            BkgSearchPositionData dataObject = e.Argument as BkgSearchPositionData;
            try
            {
                List<GameData> games = PgnMultiGameParser.ParsePgnMultiGameText(File.ReadAllText(dataObject.FilePath), out _);

                foreach (GameData gm in games)
                {
                    string fen = gm.Header.GetFenString();
                    if (!gm.Header.IsExercise())
                    {
                        fen = null;
                    }

                    try
                    {
                        VariationTree tree = new VariationTree(gm.GetContentType(false));
                        PgnGameParser parser = new PgnGameParser(gm.GameText, tree, fen);
                        List<TreeNode> lstNodes = SearchPosition.FindIdenticalNodes(tree, _parent.SearchCrits, true);
                        if (lstNodes.Count > 0)
                        {
                            _dataObject.PositionFound = true;
                            break;
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
            catch 
            {
            }
        }

        /// <summary>
        /// Invoked by the background worker once the task is finished.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            _workerState = ProcessState.FINISHED;
            _parent.JobFinished(_dataObject.FileIndex, _dataObject.PositionFound);
        }
    }
}
