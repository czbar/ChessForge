using GameTree;
using ChessPosition;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessForge
{
    /// <summary>
    /// Encapsulates a background process that processes a GameData object
    /// to build an Article object
    /// </summary>
    public class BackgroundPgnProcessor
    {
        // parent object owning this one
        private BackgroundPgnProcessingManager _parent;

        // the background worker object controlled from this object
        private BackgroundWorker _worker;

        // data object to pass to and from the background worker
        private BackgroundPgnParserData _dataObject;

        // current state of the background worker
        private ProcessState _workerState;

        // the time the process was started in ticks
        private long _startTime;

        /// <summary>
        /// Public accessor the DataObject
        /// </summary>
        public BackgroundPgnParserData DataObject
            { get { return _dataObject; } }

        /// <summary>
        /// Returns id that this processor received from the Manager.
        /// </summary>
        public int ArticleIndex
        {
            get => _dataObject == null ? -1 : _dataObject.ArticleIndex;
        }

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
        public BackgroundPgnProcessor(BackgroundPgnProcessingManager parent)
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
        /// Sets the event handlers.
        /// </summary>
        /// <param name="articleText"></param>
        /// <param name="tree"></param>
        /// <param name="dummyRun">Of true then the requested article has already been processed so shortcircuit this method.</param>
        /// <param name="fen"></param>
        public void Run(int articleIndex, string articleText, VariationTree tree, bool dummyRun, string fen = null)
        {
            _startTime = DateTime.Now.Ticks;
            _workerState = ProcessState.RUNNING;
            try
            {
                _dataObject = new BackgroundPgnParserData();
                _dataObject.ArticleIndex = articleIndex;
                _dataObject.ArticleText = articleText;
                _dataObject.Tree = dummyRun ? null : tree;

                if (tree != null)
                {
                    // check if fen needs to be set
                    if (!tree.Header.IsExercise())
                    {
                        fen = null;
                    }
                    else
                    {
                        fen = tree.Header.GetFenString();
                    }
                }

                _dataObject.Fen = fen;

                _worker.RunWorkerAsync(_dataObject);
            }
            catch (Exception ex)
            {
                _workerState = ProcessState.UNKNOWN;
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
                if (dataObject.Tree != null)
                {
                    PgnGameParser pp = new PgnGameParser(_dataObject.ArticleText, _dataObject.Tree, _dataObject.Fen, false);
                }
            }
            catch (Exception ex)
            {
                if (ex is ParserException)
                {
                    _dataObject.ErrorText = GuiUtilities.TranslateParseException(ex as ParserException);
                }
                else
                {
                    _dataObject.ErrorText = ex.Message;
                }
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
            _parent.JobFinished(_dataObject.ArticleIndex, _dataObject.ErrorText);
        }
    }
}
