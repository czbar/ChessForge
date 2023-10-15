using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            }

            if (res != 0 && SortDirection == GameSortCriterion.SortItem.DESCENDING)
            {
                res = -1 * res;
            }

            return res;
        }
    }
}
