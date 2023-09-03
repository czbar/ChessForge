﻿using GameTree;
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
        /// <param name="fen"></param>
        public void Run(int articleIndex, string articleText, ref VariationTree tree, string fen = null)
        {
            _workerState = ProcessState.RUNNING;
            try
            {
                _dataObject = new BackgroundPgnParserData();
                _dataObject.ArticleIndex = articleIndex;
                _dataObject.ArticleText = articleText;
                _dataObject.Fen = fen;
                _dataObject.Tree = tree;

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
                PgnGameParser pp = new PgnGameParser(_dataObject.ArticleText, _dataObject.Tree, _dataObject.Fen, false);
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
            _parent.JobFinished(_dataObject.ArticleIndex);
        }
    }
}
