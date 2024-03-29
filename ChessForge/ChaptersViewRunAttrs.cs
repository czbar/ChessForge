﻿using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessForge
{
    /// <summary>
    /// Holds attributes of the Run that should be brought into view
    /// when the view is refreshed.
    /// </summary>
    public class ChaptersViewRunAttrs
    {
        /// <summary>
        /// Chapter ID
        /// </summary>
        public int ChapterIndex;

        /// <summary>
        /// Content Type of the Run to bring into view
        /// </summary>
        public GameData.ContentType ContentType;

        public ChaptersViewRunAttrs(int _chapterIndex, GameData.ContentType _contentType)
        {
            ChapterIndex = _chapterIndex;
            ContentType = _contentType;
        }
    }

}
