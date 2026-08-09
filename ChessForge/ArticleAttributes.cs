namespace ChessForge
{
    /// <summary>
    /// Holds Article Guid along with some article attributes.
    /// This has been created to support Undo for DeleteSideLines and Comments.
    /// </summary>
    public class ArticleAttributes
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="article"></param>
        public ArticleAttributes(Article article)
        {
            Guid = article.Guid;
            Annotator = article.Tree.Header.GetAnnotator();
            Preamble = article.Tree.Header.BuildPreambleText();
        }

        /// <summary>
        /// Article Guid
        /// </summary>
        public string Guid { get; set; }

        /// <summary>
        /// Article Annotator/Author.
        /// </summary>
        public string Annotator { get; set; }

        /// <summary>
        /// Preamble of the article.
        /// </summary>
        public string Preamble { get; set; }
    }
}
