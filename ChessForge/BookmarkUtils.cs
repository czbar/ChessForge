using ChessPosition;
using GameTree;
using System.Text;

namespace ChessForge
{
    public class BookmarkUtils
    {
        /// <summary>
        /// Builds a string to display as the label above the bookmark.
        /// It includes the article's type, index and move notation. 
        /// </summary>
        /// <param name="bookMark"></param>
        /// <returns></returns>
        public static string BuildArticleLabelText(BookmarkWrapper bookMark)
        {
            StringBuilder sb = new StringBuilder();

            switch (bookMark.ContentType)
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

            if (bookMark.ArticleIndex >= 0)
            {
                sb.Append(" " + (bookMark.ArticleIndex + 1).ToString());
            }

            uint moveNumberOffset = 0;
            if (bookMark.Tree != null)
            {
                moveNumberOffset = bookMark.Tree.MoveNumberOffset;
            }
            sb.Append(" (" + MoveUtils.BuildSingleMoveText(bookMark.Node, true, false, moveNumberOffset) + ")");

            return sb.ToString();
        }
    }
}
