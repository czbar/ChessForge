using System.Collections.Generic;
using System.Collections.ObjectModel;
using GameTree;
using ChessPosition;

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
        private bool _IsNewTreeLineWaiting = false;

        /// <summary>
        /// Reference to the main window hosting the board
        /// that animates the moves.
        /// </summary>
        private MainWindow _MainWin;

        /// <summary>
        /// The variation being currently animated.
        /// </summary>
        private ObservableCollection<TreeNode> _TreeLineToAnimate;

        /// <summary>
        /// The new variation that has been submitted
        /// for animation while the previous one was 
        /// being shown.
        /// </summary>
        private ObservableCollection<TreeNode> _WaitingTreeLineToAnimate;

        /// <summary>
        /// Index in WaitingLineToAnimate indicating the move
        /// from which to start animation.
        /// </summary>
        private int _WaitingLineFirstMoveIndex = 0;

        /// <summary>
        /// Flags if the replay stop was requested while the it
        /// was in progress.
        /// </summary>
        private bool StopRequested = false;

        /// <summary>
        /// Node to display after replay stop was requested
        /// </summary>
        private TreeNode NodeToShowAfterStop = null;

        private ChessBoard _chessBoard;

        /// <summary>
        /// Sets reference to the hosting window.
        /// This constructors is only called once
        /// upon initialization of the main window
        /// and the object persists across the entire session.
        /// </summary>
        /// <param name="win"></param>
        public GameReplay(MainWindow win, ChessBoard chessBoard)
        {
            _MainWin = win;
            _chessBoard = chessBoard;
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
            StopRequested = false;
            NodeToShowAfterStop = null;


            // check if we are currently replaying some line
            if (!IsReplayActive)
            {
                // if GameReplay is not active, we set this up as the active line straight away.
                _TreeLineToAnimate = line;
                // prepare and request animation
                IsReplayActive = true;
                PrepareNextMoveForAnimation(moveIndex, false);
            }
            else
            {
                // set the passed line as waiting
                _WaitingTreeLineToAnimate = line;
                _IsNewTreeLineWaiting = true;
                _WaitingLineFirstMoveIndex = moveIndex;
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

            if (StopRequested)
            {
                // we had a key press or a single click indicating user's desire
                // to switch to manual replay/review
                _chessBoard.DisplayPosition(NodeToShowAfterStop.Position);
                StopRequested = false;
                NodeToShowAfterStop = null;
                Stop();
            }
            else
            {
                // check if there is a new line waiting,
                // if so switch to it.
                if (_IsNewTreeLineWaiting && _WaitingTreeLineToAnimate != null)
                {
                    _TreeLineToAnimate = _WaitingTreeLineToAnimate;

                    _IsNewTreeLineWaiting = false;
                    _WaitingTreeLineToAnimate = null;
                    index = _WaitingLineFirstMoveIndex;
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

            _IsNewTreeLineWaiting = false;
            _WaitingLineFirstMoveIndex = 0;
            _WaitingTreeLineToAnimate = null;
        }

        public void ShowPositionAndStop(TreeNode nd)
        {
            StopRequested = true;
            NodeToShowAfterStop = nd;
        }

        public TreeNode GetNodeAt(int index)
        {
            if (index < _TreeLineToAnimate.Count)
            {
                return _TreeLineToAnimate[index];
            }
            else
            {
                return null;
            }
        }

        private void AnimateMove(int index)
        {
            if (index >= _TreeLineToAnimate.Count)
                return;

            //IsReplayActive = true;

            if (index >= _TreeLineToAnimate.Count - 1)
            {
                // if we are at the end of the line display the position
                // there is nothing to animate
                _chessBoard.DisplayPosition(_TreeLineToAnimate[index].Position);
                // we are finished, clean up
                FinalizeTreeLineAnimation();
                return;
            }

            _MainWin.SelectPlyInTextViews((int)_TreeLineToAnimate[index].Position.MoveNumber, _TreeLineToAnimate[index].ColorToMove());

            LastAnimatedMoveIndex = index + 1;
            if (LastAnimatedMoveIndex < _TreeLineToAnimate.Count)
            {
                RequestNodeAnimation(_TreeLineToAnimate[index + 1]);
            }
            else
            {
                // we are finished, clean up
                FinalizeTreeLineAnimation();
            }
        }

        /// <summary>
        /// Tidy up to indicate that we are not active anymore
        /// and available for the next replay request.
        /// </summary>
        private void FinalizeTreeLineAnimation()
        {
            // tell the GUI to remove the selection from the GridView control
            _MainWin.SelectPlyInTextViews(-1, PieceColor.White);
            // Indicate that we are not replaying anything right now.
            IsReplayActive = false;
            _MainWin.rtbBoardComment_GameReplayStop();
        }

        /// <summary>
        /// Gets the position to display from the parent node
        /// and then request the Main Window to animate the move
        /// from the current node.
        /// </summary>
        /// <param name="nd"></param>
        private void RequestNodeAnimation(TreeNode nd)
        {
            _chessBoard.DisplayPosition(nd.Parent.Position);
            _MainWin.MakeMove(nd.Position.LastMove);

        }
    }
}
