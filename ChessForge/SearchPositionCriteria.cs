using ChessPosition;
using GameTree;

namespace ChessForge
{
    class SearchPositionCriteria
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public SearchPositionCriteria(TreeNode node)
        {
            SearchPosition = node.Position;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="position"></param>
        public SearchPositionCriteria(BoardPosition position)
        {
            SearchPosition = position;
        }

        /// <summary>
        /// The position to search for.
        /// </summary>
        public BoardPosition SearchPosition { get; set; }

        /// <summary>
        /// Whether to perform a partial search.
        /// </summary>
        public bool IsPartialSearch { get; set; }

        /// <summary>
        /// Whether to show a message if no position was found.
        /// </summary>
        public bool ReportNoFind { get; set; }

        /// <summary>
        /// Whether to exclude the passed node from the search.
        /// </summary>
        public bool ExcludeCurrentNode { get; set; }

        /// <summary>
        /// Whether to check the dynamic properties (castling rights, e.p. whose move).
        /// </summary>
        /// <param name="check"></param>
        public void CheckDynamicAttrs(bool check)
        {
            CheckSideToMove = check;
            CheckEnpassant = check;
            CheckCastleRights = check;
        }

        /// <summary>
        /// Whether to check which side (color) is on the move.
        /// </summary>
        public bool CheckSideToMove { get; set; }

        /// <summary>
        /// Whether to check the en passant possibilities.
        /// </summary>
        public bool CheckEnpassant { get; set; }

        /// <summary>
        /// Whether to check the castling rights.
        /// </summary>
        public bool CheckCastleRights { get; set; }
    }
}
