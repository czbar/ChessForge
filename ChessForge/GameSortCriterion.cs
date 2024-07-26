namespace ChessForge
{
    /// <summary>
    /// A class for a game selection criterion.
    /// </summary>
    public class GameSortCriterion
    {
        /// <summary>
        /// Creates an object.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        public GameSortCriterion(SortItem id, string name)
        {
            ItemId = id;
            Name = name;
        }

        /// <summary>
        /// Sort related items for the combo boxes.
        /// Both, for sort criteria and sort direction.
        /// </summary>
        public enum SortItem
        {
            NONE,
            ECO,
            WHITE_NAME,
            BLACK_NAME,
            DATE,
            ROUND,

            ASCENDING,
            DESCENDING
        }

        /// <summary>
        /// Name of the criterion to show
        /// </summary>
        public string Name;

        /// <summary>
        /// Id of the criterion
        /// </summary>
        public SortItem ItemId;

        /// <summary>
        /// Name to show in the selection ListBox.
        /// </summary>
        /// <returns></returns>
        override public string ToString()
        {
            return Name;
        }
    }
}
