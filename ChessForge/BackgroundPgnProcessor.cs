using GameTree;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;

namespace ChessForge
{
    /// <summary>
    /// Enumeration of possible Background Worker states.
    /// </summary>
    public enum BackgroundWorkerState
    {
        UNKNOWN,
        NOT_STARTED,
        RUNNING,
        FINISHED
    };

    /// <summary>
    /// Encapsulates a background process that processes a GameData object
    /// to build an Article object
    /// </summary>
    public class BackgroundPgnProcessor
    {
        // the background worker object controlled from this object
        private BackgroundWorker _worker;

        // data object to pass to and from the background worker
        private BackgroundPgnParserData _dataObject;

        // current state of the background worker
        private BackgroundWorkerState _workerState;

        public BackgroundPgnParserData DataObject
            { get { return _dataObject; } }

        /// <summary>
        /// Returns id that this processor received from the Manager.
        /// </summary>
        public int ProcessorId
        {
            get => _dataObject == null ? -1 : _dataObject.ProcessorId;
        }

        /// <summary>
        /// The current state of the background worker
        /// </summary>
        public BackgroundWorkerState WorkerState
        {
            get { return _workerState; }
        }

        /// <summary>
        /// Create a new background worker object
        /// </summary>
        public BackgroundPgnProcessor()
        {
            _worker = new BackgroundWorker();
            _workerState = BackgroundWorkerState.NOT_STARTED;

            _worker.WorkerReportsProgress = false;
            _worker.DoWork += DoWork;
            _worker.RunWorkerCompleted += RunWorkerCompleted;
        }

        /// <summary>
        /// Called by the client to start the backgound process.
        /// Sets the event handlers.
        /// </summary>
        /// <param name="gameText"></param>
        /// <param name="tree"></param>
        /// <param name="fen"></param>
        public void Run(int processorId, string gameText, VariationTree tree, string fen = null)
        {
            _workerState = BackgroundWorkerState.RUNNING;
            try
            {
                _dataObject = new BackgroundPgnParserData();
                _dataObject.ProcessorId = processorId;
                _dataObject.GameText = gameText;
                _dataObject.Fen = fen;
                _dataObject.Tree = tree;

                _worker.RunWorkerAsync(_dataObject);
            }
            catch (Exception ex)
            {
                _workerState = BackgroundWorkerState.UNKNOWN;
                AppLog.Message("BackgroundWorker Run()", ex);
            }
        }

        /// <summary>
        /// Performs background parsing of the passed text.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundPgnParserData dataObject = e.Argument as BackgroundPgnParserData;
            try
            {
                PgnGameParser pp = new PgnGameParser(dataObject.GameText, dataObject.Tree, dataObject.Fen);
            }
            catch { }
        }

        /// <summary>
        /// Invoked by the background worker once the task is finished.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            _workerState = BackgroundWorkerState.FINISHED;
            BackgroundPgnProcessingManager.JobFinished(_dataObject.ProcessorId);
        }
    }
}
