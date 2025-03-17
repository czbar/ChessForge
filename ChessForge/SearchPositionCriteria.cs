using ChessPosition;
using GameTree;

namespace ChessForge
{
    public class SearchPositionCriteria
    {
        /// <summary>
        /// The position to search for.
        /// </summary>
        private TreeNode _searchNode;

        /// <summary>
        /// The position to search for.
        /// </summary>
        private BoardPosition _searchPosition;

        /// <summary>
        /// Whether to show a message if no position was found.
        /// </summary>
        private bool _reportNotFound = true;

        /// <summary>
        /// Whether to return only one matching position.
        /// </summary>
        private bool _findFirstOnly = false;

        /// <summary>
        /// Constructor
        /// </summary>
        public SearchPositionCriteria(TreeNode node)
        {
            _searchNode = node;
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
        /// Find Position mode.
        /// We only use the FIND_AND_REPORT mode.
        /// The other mode CHECK_IF_ANY was invented to search dynamically for identical positions
        /// while editing. This proved to be too slow and was abandoned.
        /// </summary>
        public FindIdenticalPositions.Mode FindMode { get; set; }

        /// <summary>
        /// The node whose position to search for.
        /// </summary>
        public TreeNode SearchNode
        {
            get => _searchNode;
            set => _searchNode = value;
        }

        /// <summary>
        /// Whether to return only one matching position.
        /// </summary>
        public bool FindFirstOnly
        {
            get => _findFirstOnly;
            set => _findFirstOnly = value;
        }

        /// <summary>
        /// The position to search for.
        /// </summary>
        public BoardPosition SearchPosition
        {
            get
            {
                if (_searchPosition != null)
                {
                    return _searchPosition;
                }
                else if (_searchNode != null)
                {
                    return _searchNode.Position;
                }
                else
                {
                    return null;
                }
            }
            set => _searchPosition = value;
        }

        /// <summary>
        /// Whether to perform a partial search.
        /// </summary>
        public bool IsPartialSearch { get; set; }

        /// <summary>
        /// Whether to show a message if no position was found.
        /// </summary>
        public bool ReportNoFind
        {
            get => _reportNotFound;
            set => _reportNotFound = value;
        }

        /// <summary>
        /// Whether to exclude the passed node from the search.
        /// </summary>
        public bool ExcludeCurrentNode { get; set; }

        /// <summary>
        /// Whether to check the dynamic properties (castling rights, e.p. whose move).
        /// </summary>
        /// <param name="check"></param>
        public void SetCheckDynamicAttrs(bool check)
        {
            CheckSideToMove = check;
            CheckEnpassant = check;
            CheckCastleRights = check;
        }

        /// <summary>
        /// Whether to check the dynamic properties (castling rights, e.p. whose move).
        /// </summary>
        /// <returns></returns>
        public bool IsCheckDynamicAttrs()
        {
            return CheckSideToMove && CheckEnpassant && CheckCastleRights;
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
