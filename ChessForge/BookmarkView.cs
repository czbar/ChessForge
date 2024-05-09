using ChessForge.Properties;
using ChessPosition;
using GameTree;
using System;
using System.Text;
using System.Windows.Media;

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
        /// Highlights or unhighlights the bookmark
        /// by changing the label colors.
        /// </summary>
        /// <param name="on"></param>
        public void Highlight(bool on)
        {
            SolidColorBrush br = on ? ChessForgeColors.GetHintForeground(CommentBox.HintType.ERROR) : ChessForgeColors.CurrentTheme.RtbForeground;
            ChessBoard.SetTopLabelColor(br);
            ChessBoard.SetMainLabelColor(br);
        }

        /// <summary>
        /// Builds a string to display the chapter index 
        /// above the Article's label. 
        /// </summary>
        /// <returns></returns>
        private string BuildChapterLabelText()
        {
            if (Tree == null)
            {
                return "";
            }
            else
            {
                return Properties.Resources.Chapter + " " + (ChapterIndex + 1).ToString();
            }
        }

        /// <summary>
        /// Builds a string to display as the label above the Bookmark.
        /// It includes the Article's type, index and move notation. 
        /// </summary>
        /// <returns></returns>
        private string BuildArticleLabelText()
        {
            StringBuilder sb = new StringBuilder();
            GameData.ContentType contentType = ContentType;

            switch (contentType)
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
                sb.Append(" " + (ArticleIndex + 1).ToString());
            }

            uint moveNumberOffset = 0;
            if (BookmarkWrapper.Tree != null)
            {
                moveNumberOffset = BookmarkWrapper.Tree.MoveNumberOffset;   
            }
            sb.Append(" (" + MoveUtils.BuildSingleMoveText(BookmarkWrapper.Node, true, false, moveNumberOffset) + ")");

            return sb.ToString();
        }

        /// <summary>
        /// Activates the bookmark board by setting up the position,
        /// the title (label) and full opacity.
        /// </summary>
        public void Activate()
        {
            ChessBoard.DisplayPosition(null, BookmarkWrapper.Node.Position);
            ChessBoard.SetTopLabelText(BuildChapterLabelText());
            
            string lblText = BuildArticleLabelText();
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
            ChessBoard.SetTopLabelText("");
            ChessBoard.SetLabelText(Resources.ResourceManager.GetString("Bookmark"));
            ChessBoard.BoardImgCtrl.Source = ChessBoards.ChessBoardGreySmall;
            SetOpacity(0.5);
        }
    }
}
