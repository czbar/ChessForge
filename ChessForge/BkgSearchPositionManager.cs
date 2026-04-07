using ChessPosition;
using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace ChessForge
{
    /// <summary>
    /// Manages the process of processing multiple PGNs in parallel.
    /// </summary>
    public class BkgSearchPositionManager
    {
        // number of parallel workers in the pool
        private int _parallelWorkers = 4;

        // state of the parsing multi-task
        private ProcessState _state;

        // list of background processors
        private List<BkgSearchPosition> _workerPool = new List<BkgSearchPosition>();

        // number of files to process
        private int _filesToProcess;

        // number of files currently being processed
        private int _filesInProgress;

        // number of files processed
        private int _filesCompleted;

        // index of the last scheduled article
        private int _lastScheduledFile = -1;

        // used as a lock to access state variables
        private object _lockState = new object();

        /// <summary>
        /// Position search criteria. This is used by the background workers 
        /// to determine whether the position in the file being processed matches the searched one.
        /// </summary>
        public SearchPositionCriteria SearchCrits;

        /// <summary>
        /// Number of files not scheduled for parsing yet
        /// </summary>
        private int FilesNotStarted
        {
            get => _filesToProcess - (_filesInProgress + _filesCompleted);
        }

        /// <summary>
        /// Accessor to the Game processing status.
        /// </summary>
        public ProcessState State { get => _state; }

        // list of files to be processed by the background workers
        public ObservableCollection<FileToProcess> _fileList = new ObservableCollection<FileToProcess>();

        // list of files with positions found by the background workers
        public ObservableCollection<string> _filesFound = new ObservableCollection<string>();

        // reference to the parent dialog to update the UI
        private SearchPgnFilesDialog _parentDialog;

        /// <summary>
        /// Constructors. Sets the initial state.
        /// </summary>
        public BkgSearchPositionManager(SearchPgnFilesDialog dlg, SearchPositionCriteria crits)
        {
            _state = ProcessState.NOT_STARTED;
            _parentDialog = dlg;
            SearchCrits = crits;
        }

        /// <summary>
        /// Prepares the process of parsing articles from the passed list.
        /// </summary>
        /// <param name="articleDataList"></param>
        public void Execute(ObservableCollection<string> fileList)
        {
            _state = ProcessState.RUNNING;
            foreach (var file in fileList)
            {
                FileToProcess item = new FileToProcess()
                {
                    FilePath = file,
                    IsProcessed = false
                };
                _fileList.Add(item);
            }

            Initialize();

            _filesToProcess = _fileList.Count;
            if (_filesToProcess == 0)
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
        /// <param name="fileIndex"></param>
        public void JobFinished(int fileIndex, bool positionFound)
        {
            BkgSearchPosition processor = _workerPool.Find(x => x.FileIndex == fileIndex);
            if (processor != null)
            {
                if (positionFound)
                {
                    _filesFound.Add(_fileList[fileIndex].FilePath);
                }
                UpdateVariablesOnJobFinish(fileIndex);
                if (FilesNotStarted > 0)
                {
                    ReuseWorker(processor);
                }
            }
            else
            {
                // this should never happen and we can't do anything sensible here. Log the error
                AppLog.Message("ERROR: BkgSearchPositionManager:JobFinished failed to identify the background worker");
            }

            AppState.MainWin.Dispatcher.Invoke(() =>
            {
                if (positionFound)
                {
                    _fileList[fileIndex].PositionFound = true;

                    ListBoxItem item = new ListBoxItem();
                    item.Content = Path.GetFileName(_fileList[fileIndex].FilePath);
                    item.ToolTip = _fileList[fileIndex].FilePath;
                    _parentDialog.UiLbFiles.Items.Add(item);
                }

                UpdateProgressUI();

                if (_state == ProcessState.FINISHED)
                {
                    _parentDialog.IsSearchInProgress = false;
                    _parentDialog.UiLblSearchProgress.Content = Properties.Resources.Completed;
                    _parentDialog.UiBtnStartStop.Content = Properties.Resources.Search;
                }
            });
        }

        /// <summary>
        /// Resets the state so that we no longer accept jobs.
        /// </summary>
        public void CancelAll()
        {
            if (_state != ProcessState.FINISHED)
            {
                _state = ProcessState.CANCELED;
            }
        }

        /// <summary>
        /// Updates the progress label in the UI with the name of the file currently being processed.
        /// </summary>
        private void UpdateProgressUI()
        {
            long startTime = 0;
            int indexToReport = -1;

            try
            {
                foreach (BkgSearchPosition bkgProcess in _workerPool)
                {
                    if (bkgProcess != null && bkgProcess.WorkerState == ProcessState.RUNNING)
                    {
                        if (bkgProcess.StartTime > startTime)
                        {
                            startTime = bkgProcess.StartTime;
                            indexToReport = bkgProcess.FileIndex;
                        }
                    }
                }

                if (indexToReport >= 0)
                {
                    _parentDialog.UiLblSearchProgress.Content = Path.GetFileName(_fileList[indexToReport].FilePath);
                }
            }
            catch { }
        }

        /// <summary>
        /// Starts the processing.
        /// Kicks off the initial bunch of background workers.
        /// </summary>
        private void StartProcessing()
        {
            for (int i = 0; i < _parallelWorkers; i++)
            {
                if (i >= _fileList.Count)
                {
                    break;
                }

                bool res = KickoffWorker(i, i);
                lock (_lockState)
                {
                    _filesInProgress++;
                    if (!res)
                    {
                        UpdateVariablesOnJobFinish(i);
                    }
                }
            }

            UpdateProgressUI();
        }

        /// <summary>
        /// Starts a worker at the passed index for processing
        /// of the article at the passed index.
        /// </summary>
        /// <param name="workerIndex"></param>
        /// <param name="fileIndex"></param>
        /// <returns>true if the run was started, false if the argument was null</returns>
        private bool KickoffWorker(int workerIndex, int fileIndex)
        {
            bool res = false;

            if (_fileList[fileIndex] != null)
            {
                // perform only dummy processing if the article already processed
                _workerPool[workerIndex].Run(fileIndex, _fileList[fileIndex].FilePath);
                res = true;
            }
            _lastScheduledFile = fileIndex;

            return res;
        }

        /// <summary>
        /// Starts the passed worker for processing
        /// of the article at the passed index.
        /// </summary>
        /// <param name="worker"></param>
        /// <param name="fileIndex">this is the index in the output list of articles.</param>
        /// <returns>true if the run was started, false if the argument was null</returns>
        private bool KickoffWorker(BkgSearchPosition worker, int fileIndex)
        {
            bool res = false;

            if (_fileList[fileIndex] != null)
            {
                worker.Run(fileIndex, _fileList[fileIndex].FilePath);
                res = true;
            }
            _lastScheduledFile = fileIndex;

            return res;
        }

        /// <summary>
        /// Check if there are any jobs still pending and if so
        /// take the next one and start the passed worker on it.
        /// </summary>
        /// <param name="processor"></param>
        private void ReuseWorker(BkgSearchPosition processor)
        {
            if (FilesNotStarted > 0 && _state != ProcessState.CANCELED)
            {
                // find the first unproccesed index
                for (int i = _lastScheduledFile + 1; i < _fileList.Count; ++i)
                {
                    if (!_fileList[i].IsProcessed)
                    {
                        _lastScheduledFile = i;
                        bool res = KickoffWorker(processor, i);
                        _filesInProgress++;
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
            _parallelWorkers = Math.Max(Configuration.CoreCount, 4);

            for (int i = 0; i < _parallelWorkers; i++)
            {
                _workerPool.Add(new BkgSearchPosition(this));
            }
        }

        /// <summary>
        /// Updates variables upon return from a worker.
        /// </summary>
        /// <param name="fileIndex"></param>
        private void UpdateVariablesOnJobFinish(int fileIndex)
        {
            lock (_lockState)
            {
                try
                {
                    _filesInProgress--;
                    _filesCompleted++;

                    // item at index 0 can be null if we are reading generic PGN
                    if (fileIndex > 0 || _fileList[0] != null)
                    {
                        _fileList[fileIndex].IsProcessed = true;

                        if (_filesCompleted >= _fileList.Count)
                        {
                            _state = ProcessState.FINISHED;
                            AppState.DoEvents();
                        }
                    }
                }
                catch (Exception ex)
                {
                    AppLog.Message("UpdateVariablesOnJobFinish()", ex);
                }
            }
        }
    }

    /// <summary>
    /// Attribute class for items in the list of files to process.
    /// </summary>
    public class FileToProcess
    {
        /// <summary>
        /// Path to the file to process.
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Whether the file has been processed. 
        /// </summary>
        public bool IsProcessed { get; set; }

        /// <summary>
        /// Whether the position was found in the file. This is set by the background worker after processing the file.
        /// </summary>
        public bool PositionFound { get; set; }
    }
}
