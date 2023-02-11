using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessForge
{
    /// <summary>
    /// Class to use in the lists for selection of the content type
    /// </summary>
    public class ContentTypeListItem
    {
        /// <summary>
        /// Content type
        /// </summary>
        public GameData.ContentType ContentType;

        /// <summary>
        /// Name to show in the lists.
        /// </summary>
        public string DisplayName;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="contentType"></param>
        /// <param name="displayName"></param>
        public ContentTypeListItem(GameData.ContentType contentType, string displayName)
        {
            ContentType = contentType;
            DisplayName = displayName;
        }

        /// <summary>
        /// Override returning the name of the content type.
        /// </summary>
        /// <returns></returns>
        override public string ToString()
        {
            return DisplayName;
        }
    }
}
