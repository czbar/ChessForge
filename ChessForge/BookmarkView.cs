using ChessForge.Properties;
using ChessPosition;
using GameTree;
using System;
using System.Text;

namespace ChessForge
{
    /// <summary>
    /// Manages the ChessBoard holding the Bookmark's position
    /// Uses the BookmarkWrapper reference to access Bookmark's data.
    /// </summary>
    public class BookmarkView
    {
        /// <summary>
        /// The chessboard object for the bookmark.
        /// </summary>
        public ChessBoardSmall ChessBoard;

        /// <summary>
        /// Holds the Bookmark and additional info about its parentage.
        /// </summary>
        public BookmarkWrapper BookmarkWrapper;

        /// <summary>
        /// Access to the ContentType property of the BookmarkWrapper
        /// </summary>
        public GameData.ContentType ContentType
        {
            get => BookmarkWrapper.ContentType;
        }

        /// <summary>
        /// Access to the ChapterIndex property of the BookmarkWrapper
        /// </summary>
        public int ChapterIndex
        {
            get => BookmarkWrapper.ChapterIndex;
        }

        /// <summary>
        /// Access to the Tree property of the BookmarkWrapper
        /// </summary>
        public VariationTree Tree
        {
            get => BookmarkWrapper.Tree;
        }

        /// <summary>
        /// Access to the ArticleIndex property of the BookmarkWrapper
        /// </summary>
        public int ArticleIndex
        {
            get => BookmarkWrapper.ArticleIndex;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="board"></param>
        public BookmarkView(ChessBoardSmall board)
        {
            ChessBoard = board;
        }

        /// <summary>
        /// Sets opacity (in order to "gray out" 
        /// or "activate" the board).
        /// </summary>
        /// <param name="opacity"></param>
        public void SetOpacity(double opacity)
        {
            ChessBoard.SetBoardOpacity(opacity);
        }

        /// <summary>
        /// Builds a string to display as the label above the Bookmark.
        /// It includes the Article's type, index and move notation. 
        /// </summary>
        /// <returns></returns>
        private string BuildLabelText()
        {
            StringBuilder sb = new StringBuilder();
            GameData.ContentType contetType = ContentType;

            switch (contetType)
            {
                case GameData.ContentType.STUDY_TREE:
                    sb.Append(Properties.Resources.Study);
                    break;
                case GameData.ContentType.MODEL_GAME:
                    sb.Append(Properties.Resources.Game);
                    break;
                case GameData.ContentType.EXERCISE:
                    sb.Append(Properties.Resources.Exercise);
                    break;
                default:
                    break;
            }

            if (ArticleIndex >= 0)
            {
                sb.Append(" #" + (ArticleIndex + 1).ToString());
            }

            sb.Append(" (" + MoveUtils.BuildSingleMoveText(BookmarkWrapper.Node, true, true) + ")");

            return sb.ToString();
        }

        /// <summary>
        /// Activates the bookmark board by setting up the position,
        /// the title (label) and full opacity.
        /// </summary>
        public void Activate()
        {
            ChessBoard.DisplayPosition(null, BookmarkWrapper.Node.Position);
            string lblText = BuildLabelText();
            ChessBoard.SetLabelText(lblText);
            SetOpacity(1);
        }

        /// <summary>
        /// Deactivates the bookmark by removing the pieces
        /// from the board, clearing the label
        /// and graying it out.
        /// </summary>
        public void Deactivate()
        {
            ChessBoard.ClearBoard();
            ChessBoard.SetLabelText(Resources.ResourceManager.GetString("Bookmark"));
            SetOpacity(0.5);
        }
    }
}
