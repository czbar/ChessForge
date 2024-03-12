using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChessPosition;

namespace ChessForge
{
    /// <summary>
    /// Performs comparison on two Article objects according the passed criteria.
    /// </summary>
    public class ArticleComparer : IComparer<Article>
    {
        /// <summary>
        /// The field/criterion to sort on.
        /// </summary>
        public GameSortCriterion.SortItem SortCriterion;

        /// <summary>
        /// Sort direction, ascending or descending.
        /// </summary>
        public GameSortCriterion.SortItem SortDirection;

        /// <summary>
        /// Creates an object with specified sorting criteria
        /// </summary>
        /// <param name="sortCrit"></param>
        /// <param name="direction"></param>
        public ArticleComparer(GameSortCriterion.SortItem sortCrit, GameSortCriterion.SortItem direction)
        {
            SortCriterion = sortCrit;
            SortDirection = direction;
        }

        /// <summary>
        /// Performs comparison of 2 articles.
        /// </summary>
        /// <param name="art1"></param>
        /// <param name="art2"></param>
        /// <returns></returns>
        public int Compare(Article art1, Article art2)
        {
            int res = 0;

            switch (SortCriterion)
            {
                case GameSortCriterion.SortItem.ECO:
                    res = string.Compare(art1.Tree.Header.GetECO(out _), art2.Tree.Header.GetECO(out _));
                    if (res == 0)
                    {
                        res = string.Compare(MainLineSignature(art1), MainLineSignature(art2));
                    }
                    break;
                case GameSortCriterion.SortItem.DATE:
                    res = string.Compare(art1.Tree.Header.GetDate(out _), art2.Tree.Header.GetDate(out _));
                    break;
                case GameSortCriterion.SortItem.WHITE_NAME:
                    res = string.Compare(art1.Tree.Header.GetWhitePlayer(out _), art2.Tree.Header.GetWhitePlayer(out _));
                    break;
                case GameSortCriterion.SortItem.BLACK_NAME:
                    res = string.Compare(art1.Tree.Header.GetBlackPlayer(out _), art2.Tree.Header.GetBlackPlayer(out _));
                    break;
                case GameSortCriterion.SortItem.ROUND:
                    res = SplitChapterUtils.CompareRoundNo(art1.Tree.Header.GetRound(out _), art2.Tree.Header.GetRound(out _));
                    break;
            }

            if (res != 0 && SortDirection == GameSortCriterion.SortItem.DESCENDING)
            {
                res = -1 * res;
            }

            return res;
        }

        /// <summary>
        /// Creates a line's "signature" by concatenating move strings.
        /// </summary>
        /// <param name="art"></param>
        /// <returns></returns>
        private string MainLineSignature(Article art)
        {
            StringBuilder sb = new StringBuilder("");

            List<TreeNode> lstMainLine = TreeUtils.GetMainLine(art.Tree);
            foreach (TreeNode nd in lstMainLine)
            {
                sb.Append(MoveString(nd));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Builds text for a single move to use in the line signature.
        /// </summary>
        /// <param name="nd"></param>
        /// <returns></returns>
        private string MoveString(TreeNode nd)
        {
            StringBuilder sb = new StringBuilder("");
            if (nd != null)
            {
                sb.Append(nd.MoveNumber.ToString() + '.');
                if (nd.ColorToMove == ChessPosition.PieceColor.White)
                {
                    sb.Append("..");
                }
                sb.Append(nd.LastMoveAlgebraicNotation ?? "");
            }

            return sb.ToString();
        }
    }
}
