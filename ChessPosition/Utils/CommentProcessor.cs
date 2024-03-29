﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessPosition.Utils
{
    public class CommentProcessor
    {
        /// <summary>
        /// Returns the list of parts of the comment if there are any URLs found.
        /// There are parts before the urls, urls themselves and comments after the urls.
        /// If there are no urls, the output argument will return the original comment
        /// so that we save performance by not creating a list where it is not necessary.
        /// </summary>
        /// <param name="comment"></param>
        /// <returns>A list of comment parts that can be empty but never null.</returns>
        public static List<CommentPart> SplitCommentTextAtUrls(string comment)
        {
            List<CommentPart> parts = new List<CommentPart>();

            if (!string.IsNullOrEmpty(comment))
            {
                try
                {
                    List<string> urls = TextUtils.MatchUrls(comment);
                    if (urls == null || urls.Count == 0)
                    {
                        parts.Add(new CommentPart(CommentPartType.TEXT, comment));
                    }
                    else
                    {
                        int firstUnprocessedChar = 0;
                        for (int i = 0; i < urls.Count; i++)
                        {
                            int pos_start = comment.IndexOf(urls[i]);
                            int pos_end = pos_start + urls[i].Length - 1;
                            if (pos_start > firstUnprocessedChar)
                            {
                                parts.Add(new CommentPart(CommentPartType.TEXT, comment.Substring(firstUnprocessedChar, pos_start - firstUnprocessedChar)));
                            }
                            parts.Add(new CommentPart(CommentPartType.URL, urls[i]));
                            firstUnprocessedChar = pos_end + 1;
                        }
                        if (firstUnprocessedChar < comment.Length)
                        {
                            parts.Add(new CommentPart(CommentPartType.TEXT, comment.Substring(firstUnprocessedChar)));
                        }
                    }
                }
                catch
                {
                    parts.Clear();
                }
            }

            return parts;
        }
    }
}
