namespace ChessForge
{
    /// <summary>
    /// A class of objects that will be passed to and from the background worker
    /// </summary>
    public class BkgSearchPositionData
    {
        /// <summary>
        /// Path of the file to process.
        /// </summary>
        public string FilePath;

        /// <summary>
        /// Identifies the processor using this objects
        /// </summary>
        public int FileIndex;

        /// <summary>
        /// Results of the search. If PositionFound is true, this field contains the position found in the file.
        /// </summary>
        public bool PositionFound;
    }
}
