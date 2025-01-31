using System.Collections.Generic;
using System.Collections.ObjectModel;
using GameTree;
using ChessPosition;
using System.Reflection;

namespace ChessForge
{
    /// <summary>
    /// Control auto-replay of tree variations.
    /// </summary>
    public class GameReplay
    {
        /// <summary>
        /// Indicates whether we are currently in the process
        /// of controlling the replay animation in the GUI.
        /// </summary>
        public bool IsReplayActive = false;

        /// <summary>
        /// Index in LineToAnimate indicating the most recent move
        /// sent for animation.
        /// </summary>
        public int LastAnimatedMoveIndex = 0;

        /// <summary>
        /// Indicates that there is a new line waiting to be shown and the current replay should
        /// be stopped.
        /// </summary>
        private bool _isNewTreeLineWaiting = false;

        /// <summary>
        // Application's Main Window
        /// </summary>
        private MainWindow _mainWin;

        // Last node of the line being replayed
        private TreeNode _lastReplayNode;

        // Variation tree being replayed
        private VariationTree _activeVariationTree;

        // active type where the replay occurs
        private TabViewType _viewType;

        /// <summary>
        /// The variation being currently animated.
        /// </summary>
        private ObservableCollection<TreeNode> _treeLineToAnimate;

        /// <summary>
        /// The new variation that has been submitted
        /// for animation while the previous one was 
        /// being shown.
        /// </summary>
        private ObservableCollection<TreeNode> _waitingTreeLineToAnimate;

        /// <summary>
        /// Index in WaitingLineToAnimate indicating the move
        /// from which to start animation.
        /// </summary>
        private int _waitingLineFirstMoveIndex = 0;

        /// <summary>
        /// Flags if the replay stop was requested while the it
        /// was in progress.
        /// </summary>
        private bool _stopRequested = false;

        /// <summary>
        /// Node to display after replay stop was requested
        /// </summary>
        private TreeNode _nodeToShowAfterStop = null;

        private ChessBoard _chessBoard;

        private CommentBox _commentBox;

        /// <summary>
        /// Sets reference to the hosting window.
        /// This constructors is only called once
        /// upon initialization of the main window
        /// and the object persists across the entire session.
        /// </summary>
        /// <param name="win"></param>
        public GameReplay(MainWindow win, ChessBoard chessBoard, CommentBox commentBox)
        {
            _mainWin = win;
            _chessBoard = chessBoard;
            _commentBox = commentBox;
        }

        /// <summary>
        /// Sets up a new variation line for display/animation.
        /// </summary>
        /// <param name="wnd"></param>
        /// <param name="line"></param>
        /// <param name="isLineMode"></param>
        public void SetupTreeLineToDisplay(ObservableCollection<TreeNode> line, int moveIndex)
        {
            // since this method is invoked by a double click, the request to stop
            // would be a side effect of the first mouse-down of the double click
            // Therefore, we will ignore and reset it. 
            _stopRequested = false;
            _nodeToShowAfterStop = null;

            _lastReplayNode = _mainWin.ActiveLine.GetLastNode();
            _activeVariationTree = AppState.ActiveVariationTree;
            _viewType = AppState.ActiveTab;

            // check if we are currently replaying some line
            if (!IsReplayActive)
            {
                // if GameReplay is not active, we set this up as the active line straight away.
                _treeLineToAnimate = line;
                // prepare and request animation
                IsReplayActive = true;
                PrepareNextMoveForAnimation(moveIndex, false);
            }
            else
            {
                // set the passed line as waiting
                _waitingTreeLineToAnimate = line;
                _isNewTreeLineWaiting = true;
                _waitingLineFirstMoveIndex = moveIndex;
            }
        }

        /// <summary>
        /// Invoked from the GUI to prepare the next move for animation and send it back.
        /// We need to check if a new line was requested in the meantime. 
        /// If so, we need to abandon the current one and switch to the new one.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="newLine"></param>
        public void PrepareNextMoveForAnimation(int index, bool newLine)
        {
            if (!IsReplayActive)
                return;

            if (_lastReplayNode != _mainWin.ActiveLine.Line.GetLastNode() 
                || _activeVariationTree != AppState.ActiveVariationTree 
                || _viewType != AppState.ActiveTab)
            {
                Stop();
                return;
            }

            if (_stopRequested)
            {
                // we had a key press or a single click indicating user's desire
                // to switch to manual replay/review
                _mainWin.DisplayPosition(_nodeToShowAfterStop);
                _stopRequested = false;
                _nodeToShowAfterStop = null;
                Stop();
            }
            else
            {
                // check if there is a new line waiting,
                // if so switch to it.
                if (_isNewTreeLineWaiting && _waitingTreeLineToAnimate != null)
                {
                    _treeLineToAnimate = _waitingTreeLineToAnimate;

                    _isNewTreeLineWaiting = false;
                    _waitingTreeLineToAnimate = null;
                    index = _waitingLineFirstMoveIndex;
                }
                AnimateMove(index);
            }
        }

        /// <summary>
        /// Resets all active animation settings.
        /// </summary>
        public void Stop()
        {
            IsReplayActive = false;

            _isNewTreeLineWaiting = false;
            _waitingLineFirstMoveIndex = 0;
            _waitingTreeLineToAnimate = null;
        }

        /// <summary>
        /// Stops the current animation process.
        /// Sets the stop request flag and the position to show
        /// for the Stop() method.
        /// </summary>
        /// <param name="nd"></param>
        public void ShowPositionAndStop(TreeNode nd)
        {
            _stopRequested = true;
            _nodeToShowAfterStop = nd;

            _mainWin.StopMoveAnimation();
            Stop();
        }

        public TreeNode GetNodeAt(int index)
        {
            if (index < _treeLineToAnimate.Count)
            {
                return _treeLineToAnimate[index];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Sets up animation of a single move.
        /// </summary>
        /// <param name="index"></param>
        private void AnimateMove(int index)
        {
            if (index >= _treeLineToAnimate.Count)
            {
                _mainWin.BoardCommentBox.RestoreTitleMessage();
                return;
            }

            if (index >= _treeLineToAnimate.Count - 1)
            {
                // if we are at the end of the line display the position
                // there is nothing to animate
                _mainWin.DisplayPosition(_treeLineToAnimate[index]);
                // we are finished, clean up
                FinalizeTreeLineAnimation();
                return;
            }

            _mainWin.ActiveLine.SelectPly((int)_treeLineToAnimate[index].Position.MoveNumber, _treeLineToAnimate[index].ColorToMove);

            LastAnimatedMoveIndex = index + 1;
            if (LastAnimatedMoveIndex < _treeLineToAnimate.Count)
            {
                _mainWin.ActiveTreeView.SelectLineAndMoveInWorkbookViews(_mainWin.ActiveLine.GetLineId(), LastAnimatedMoveIndex, true);
                RequestNodeAnimation(_treeLineToAnimate[LastAnimatedMoveIndex]);
            }
            else
            {
                // we are finished, clean up
                FinalizeTreeLineAnimation();
            }
        }

        /// <summary>
        /// This is called when the line being replayed ended.
        /// Selects the last move in the calling view.
        /// </summary>
        private void FinalizeTreeLineAnimation()
        {
            IsReplayActive = false;
            _commentBox.RestoreTitleMessage();
            _mainWin.ActiveTreeView.SelectLineAndMoveInWorkbookViews(_mainWin.ActiveLine.Line.GetLineId(), _treeLineToAnimate.Count - 1, true);
        }

        /// <summary>
        /// Gets the position to display from the parent node
        /// and then request the Main Window to animate the move
        /// from the current node.
        /// </summary>
        /// <param name="nd"></param>
        private void RequestNodeAnimation(TreeNode nd)
        {
            _mainWin.DisplayPosition(nd.Parent);
            _mainWin.RequestMoveAnimation(nd.Position.LastMove);

        }
    }
}
