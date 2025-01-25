namespace ChessPosition
{
    /// <summary>
    /// Encapsulates a string and a boolean value.
    /// </summary>
    public class SelectableString
    {
        /// <summary>
        /// The string.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// The boolean value.
        /// </summary>
        public bool IsSelected { get; set; }

        /// <summary>
        /// Creates a new instance of SelectableString.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="isSelected"></param>
        public SelectableString(string text, bool isSelected)
        {
            Text = text;
            IsSelected = isSelected;
        }
    }
}
