using ChessForge.Properties;
using ChessPosition;
using GameTree;
using System;

namespace ChessForge
{
    /// <summary>
    /// Combines the Bookmark's content
    /// and its GUI visualization.
    /// </summary>
    public class BookmarkView : IComparable<BookmarkView>
    {

        /// <summary>
        /// The Bookmark object shown in this view.
        /// </summary>
        public Bookmark BookmarkData;

        /// <summary>
        /// Variation Tree in which the bookmark belongs.
        /// </summary>
        public VariationTree Tree;

        /// <summary>
        /// Type of Tree this bookmark is in.
        /// </summary>
        public GameData.ContentType ContentType
        {
            get => Tree.Header.GetContentType(out _);
        } 

        /// <summary>
        /// The chessboard object for the bookmark.
        /// </summary>
        private ChessBoard _guiBoard;

        public BookmarkView(ChessBoard board)
        {
            _guiBoard = board;
        }

        public BookmarkView(VariationTree tree, Bookmark bm)
        {
            _guiBoard = null;
            BookmarkData = bm;
            Tree = tree;
        }

        /// <summary>
        /// Comparator to use when sorting by move number and color.
        /// </summary>
        /// <param name="bm"></param>
        /// <returns></returns>
        public int CompareTo(BookmarkView bm)
        {
            if (bm == null)
                return -1;

            if (this.ContentType == GameData.ContentType.STUDY_TREE && bm.ContentType != GameData.ContentType.STUDY_TREE)
            {
                return -1;
            }
            else if (this.ContentType == GameData.ContentType.MODEL_GAME)
            {
                if (bm.ContentType == GameData.ContentType.STUDY_TREE)
                {
                    return 1;
                }
                else if (bm.ContentType == GameData.ContentType.EXERCISE)
                {
                    return -1;
                }
            }
            else if (this.ContentType == GameData.ContentType.EXERCISE && bm.ContentType != GameData.ContentType.EXERCISE)
            {
                return 1;
            }

            int moveNoDiff = (int)this.BookmarkData.Node.MoveNumber - (int)bm.BookmarkData.Node.MoveNumber;
            if (moveNoDiff != 0)
            {
                return moveNoDiff;
            }
            else
            {
                if (this.BookmarkData.Node.ColorToMove == PieceColor.Black)
                {
                    return -1;
                }
                else
                {
                    return 1;
                }
            }
        }

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
            _guiBoard.DisplayPosition(null, BookmarkData.Node.Position);
            _guiBoard.SetLabelText(MoveUtils.BuildSingleMoveText(BookmarkData.Node, true, true));
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
            _guiBoard.SetLabelText(Resources.ResourceManager.GetString("Bookmark"));
            SetOpacity(0.5);
        }
    }
}
