using ChessPosition;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;

namespace ChessForge
{
    /// <summary>
    /// Encapsulates a background process that searches for PGN files in a specified folder and its subfolders,
    /// </summary>
    public class BkgFindPgnFiles
    {
        // parent object owning this one
        private BkgSearchPositionManager _parent;

        // the background worker object controlled from this object
        private BackgroundWorker _worker;

        // data object to pass to and from the background worker
        private BkgFindPgnFilesData _dataObject;

        // current state of the background worker
        private ProcessState _workerState;

        // the time the process was started in ticks
        private long _startTime;

        /// <summary>
        /// Public accessor the DataObject
        /// </summary>
        public BkgFindPgnFilesData DataObject
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
        public BkgFindPgnFiles(BkgSearchPositionManager parent)
        {
            _parent = parent;
            _workerState = ProcessState.NOT_STARTED;

            _worker = new BackgroundWorker();
            _worker.WorkerReportsProgress = false;
            _worker.DoWork += DoWork;
            _worker.RunWorkerCompleted += RunWorkerCompleted;
            _worker.WorkerSupportsCancellation = true;
            _parent = parent;
        }

        /// <summary>
        /// Called by the client to start the backgound process.
        /// Searches for PGN files in the specified folder and its subfolders.
        /// </summary>
        public void Run(string rootFolder)
        {
            _startTime = DateTime.Now.Ticks;
            _workerState = ProcessState.RUNNING;
            try
            {
                _dataObject = new BkgFindPgnFilesData();
                _dataObject.RootFolder = rootFolder;
                _worker.RunWorkerAsync(_dataObject);
            }
            catch (Exception ex)
            {
                _workerState = ProcessState.UNKNOWN;
                AppLog.Message("BkgFindPgnFiles Run()", ex);
            }
        }

        /// <summary>
        /// Invoked by the background worker to perform the search for PGN files.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                _dataObject.FoundFiles = new ObservableCollection<string>(GetPgnFilesSafe(_dataObject.RootFolder));
            }
            catch
            {
            }
        }

        /// <summary>
        /// Called by the client to cancel the background process.
        /// </summary>
        public void Cancel()
        {
            try
            {
                if (_worker.IsBusy)
                {
                    _worker.CancelAsync();
                    _workerState = ProcessState.CANCELED;
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Recursively gets all PGN files in the specified folder and subfolders, 
        /// while handling exceptions that may occur due to inaccessible folders or files.
        /// </summary>
        /// <param name="rootFolder"></param>
        /// <returns></returns>
        private IEnumerable<string> GetPgnFilesSafe(string rootFolder)
        {
            if (string.IsNullOrWhiteSpace(rootFolder))
                yield break;

            var pending = new Stack<string>();
            pending.Push(rootFolder);

            while (pending.Count > 0)
            {
                string currentFolder = pending.Pop();

                string[] subfolders;
                try
                {
                    subfolders = Directory.GetDirectories(currentFolder);
                }
                catch
                {
                    continue;
                }

                foreach (string subfolder in subfolders)
                {
                    pending.Push(subfolder);
                }

                string[] files;
                try
                {
                    if (_workerState == ProcessState.CANCELED)
                    {
                        _parent.ClearUIReport();
                        yield break;
                    }
                    _parent.ReportCurrentFolder(currentFolder);
                    files = Directory.GetFiles(currentFolder, "*.pgn");
                }
                catch
                {
                    continue;
                }

                foreach (string file in files)
                {
                    yield return file;
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
            _parent.FindPgnFilesFinished(_dataObject);
        }
    }
}
