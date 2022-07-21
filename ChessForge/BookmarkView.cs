using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameTree;
using ChessPosition;

namespace ChessForge
{
    /// <summary>
    /// Combines the Bookmark's content
    /// and its GUI visualization.
    /// </summary>
    public class BookmarkView
    {
        public BookmarkView(ChessBoard board)
        {
            _guiBoard = board;
        }

        /// <summary>
        /// The Bookmark object shown in this view.
        /// </summary>
        public Bookmark BookmarkData;

        /// <summary>
        /// The chessboard object for the bookmark.
        /// </summary>
        private ChessBoard _guiBoard;

        /// <summary>
        /// Sets opacity (in order to "gray out" 
        /// or "activate" the board).
        /// </summary>
        /// <param name="opacity"></param>
        public void SetOpacity(double opacity)
        {
            _guiBoard.SetBoardOpacity(opacity);
        }

        /// <summary>
        /// Activates the bookmark board by setting up the position,
        /// the title (lable) and full opacity.
        /// </summary>
        public void Activate()
        {
            _guiBoard.DisplayPosition(BookmarkData.Node.Position);
            _guiBoard.SetLabelText(BookmarkData.Node.GetPlyText(true));
            SetOpacity(1);
        }

        /// <summary>
        /// Deactivates the bookmark by removing the pieces
        /// from the board, clearing the label
        /// and graying it out.
        /// </summary>
        public void Deactivate()
        {
            _guiBoard.ClearBoard();
            _guiBoard.SetLabelText("Bookmark");
            SetOpacity(0.5);
        }
    }
}
