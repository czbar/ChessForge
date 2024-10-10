using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Documents;
using ChessPosition.Utils;
using GameTree;

namespace ChessPosition
{
    public class TextUtils
    {
        /// <summary>
        /// If the comment contains CR/LF replace it with a space.
        /// This is for example due to ChessBase export formatting when they insert CRLF without
        /// preserving space.
        /// </summary>
        /// <param name="comment"></param>
        /// <returns></returns>
        public static string ReplaceCrLfInComment(string comment)
        {
            if (!string.IsNullOrEmpty(comment))
            {
                comment.Replace("\r\n", " ");
            }

            return comment;
        }

        /// <summary>
        /// Returns all urls found in the passed text.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static List<string> MatchUrls(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }

            Regex urlRegex = new Regex(@"\b(https?://|www\.)\S+\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

            MatchCollection matches = urlRegex.Matches(text);
            if (matches.Count > 0)
            {
                List<string> urls = new List<string>();
                foreach (Match match in matches)
                {
                    urls.Add(match.Value);
                }
                return urls;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Replaces some special characters so that FEN strings in resource
        /// files are not corrupted.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string AdjustResourceStringForXml(string s)
        {
            int hash = Math.Abs(s.GetHashCode());
            return s.Replace('/', '_').Replace(' ', '0').Replace('-', '_') + "_" + hash.ToString();
        }

        /// <summary>
        /// Remove any trailing dots from a string.
        /// It is useful e.g. when we have a menu item with trailing "..."
        /// and want to use it as a dialog name without the dots.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string RemoveTrailingDots(string str)
        {
            if (!string.IsNullOrEmpty(str))
            {
                int trailingDotCount = 0;
                for (int i = str.Length - 1; i > 0; i--)
                {
                    if (str[i] == '.')
                    {
                        trailingDotCount++;
                    }
                    else
                    {
                        break;
                    }
                }

                if (trailingDotCount > 0)
                {
                    str = str.Substring(0, str.Length - trailingDotCount);
                }
            }

            return str;
        }

        /// <summary>
        /// Parses the supplied string into tokens split by '.'.
        /// Expects a sequence of 3 numbers in the form of 1.1.1 
        /// or the version string will be considered invalid and will return false.
        /// </summary>
        /// <param name="sVer"></param>
        /// <param name="major"></param>
        /// <param name="minor"></param>
        /// <param name="patch"></param>
        /// <returns></returns>
        public static bool GetVersionNumbers(string sVer, out int major, out int minor, out int patch)
        {
            bool result = false;
            major = 0;
            minor = 0;
            patch = 0;

            try
            {
                string[] tokens = sVer.Split('.');

                int lastPart = 0;
                for (int i = 0; i < tokens.Length; i++)
                {
                    int val;
                    if (int.TryParse(tokens[i], out val))
                    {
                        lastPart++;
                        switch (lastPart)
                        {
                            case 1:
                                major = val;
                                break;
                            case 2:
                                minor = val;
                                break;
                            case 3:
                                patch = val;
                                break;
                        }
                    }
                    else
                    {
                        if (lastPart != 0)
                        {
                            break;
                        }
                    }

                    if (lastPart == 3)
                    {
                        result = true;
                        break;
                    }
                }
            }
            catch
            {
                result = false;
            }

            return result;
        }

        /// <summary>
        /// Returns the passed string after removing invalid file name chars from it.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static string RemoveInvalidCharsFromFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return fileName;
            }

            string charsToRemove = new string(Path.GetInvalidFileNameChars());
            foreach (var c in charsToRemove)
            {
                fileName = fileName.Replace(c.ToString(), string.Empty);
            }

            return fileName;
        }

        /// <summary>
        /// Removes the "??" nag from the Run's text.
        /// </summary>
        /// <param name="txt"></param>
        /// <returns></returns>
        public static void RemoveBlunderNagFromText(Run run)
        {
            if (run != null && !string.IsNullOrEmpty(run.Text))
            {
                run.Text = run.Text.Replace("??", "");
            }
        }

        /// <summary>
        /// Builds text for variation line passed as a list
        /// of nodes. Depending on the fromIndex and toIndex arguments,
        /// an entire list or only a part of it will be included.
        /// </summary>
        /// <param name="line">The variation line to process</param>
        /// <param name="withNAG">Whether to include NAG symbols</param>
        /// <param name="fromIndex">The index to start from.</param>
        /// <param name="toIndex">The index of the last included ply.  
        /// If -1, the whole line starting as fromIndex will be included.</param>
        /// <returns></returns>
        public static string BuildLineText(List<TreeNode> line, uint moveNumberOffset, bool withNAG = false, int fromIndex = 0, int toIndex = -1)
        {
            StringBuilder sb = new StringBuilder();

            if (toIndex < 0 || toIndex >= line.Count - 1)
            {
                toIndex = line.Count - 1;
            }

            for (int i = fromIndex; i <= toIndex; i++)
            {
                TreeNode nd = line[i];

                // if NodeId is 0 this the starting position Node and we must not process it
                if (nd.NodeId != 0)
                {
                    sb.Append(" " + MoveUtils.BuildSingleMoveText(nd, i == fromIndex, withNAG, moveNumberOffset));
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Builds text for a message reporting error in processing a move.
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="algMove"></param>
        /// <returns></returns>
        public static string BuildErrortext(TreeNode nd, string algMove)
        {
            LocalizedStrings.Values.TryGetValue(LocalizedStrings.StringId.Move, out string msg);

            StringBuilder sb = new StringBuilder();
            sb.Append(msg + " " + nd.MoveNumber.ToString());
            if (nd.ColorToMove == PieceColor.White)
            {
                sb.Append("...");
            }
            else
            {
                sb.Append(".");
            }
            sb.Append(algMove);
            return sb.ToString();
        }

        /// <summary>
        /// Removes the last character of the algebraic notation if it
        /// denotes a check or a mate.
        /// This is so that these symbols are not duplicated when they get added
        /// later on.
        /// </summary>
        /// <param name="algMove"></param>
        /// <returns></returns>
        public static string StripCheckOrMateChar(string algMove)
        {

            if (algMove[algMove.Length - 1] == '#' || algMove[algMove.Length - 1] == '+')
            {
                return algMove.Substring(0, algMove.Length - 1);
            }

            return algMove;
        }


        /// <summary>
        /// Given a string, checks if it is in Chess Forge / PGN
        /// format (yyyy.mm.dd) and if not gets out of it what it can.
        /// Returns the corrected string.
        /// </summary>
        /// <param name="val"></param>
        /// <param name="hasMonth">Returns whether the string had a month included</param>
        /// <param name="hasDay">Returns whether the string had a day included</param>
        /// <returns></returns>
        public static string AdjustPgnDateString(string val, out bool hasMonth, out bool hasDay)
        {
            hasMonth = false;
            hasDay = false;

            StringBuilder sb = new StringBuilder();

            if (string.IsNullOrEmpty(val))
            {
                return Constants.EMPTY_PGN_DATE;
            }

            string[] tokens = val.Split(new char[] { '.', '-', '/' });
            if (tokens[0].Length == 4)
            {
                if (int.TryParse(tokens[0], out int year) && year > 1000)
                {
                    sb.Append(tokens[0] + '.');
                    if (tokens.Length > 1 && int.TryParse(tokens[1], out int month) && month >= 1 && month <= 12)
                    {
                        sb.Append(month.ToString("00") + '.');
                        hasMonth = true;
                        if (tokens.Length > 2 && int.TryParse(tokens[2], out int day) && day >= 1 && day <= 31)
                        {
                            sb.Append(day.ToString("00"));
                            hasDay = true;
                        }
                        else
                        {
                            sb.Append("??");
                        }
                    }
                    else
                    {
                        sb.Append("??.??");
                    }
                }
            }
            else
            {
                sb.Append(Constants.EMPTY_PGN_DATE);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Builds a PGN formatted date from a DateTiem object
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="ignoreMonthDay">whether month and day should be replaced with "??.??"</param>
        /// <param name="ignoreDay">whether day should be replaced with "??"</param>
        /// <returns></returns>
        public static string BuildPgnDateString(DateTime? dt, bool ignoreMonthDay = false, bool ignoreDay = false)
        {
            if (dt == null)
            {
                return Constants.EMPTY_PGN_DATE;
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(dt.Value.Year.ToString("0000") + '.');
                if (ignoreMonthDay)
                {
                    sb.Append("??.??");
                }
                else
                {
                    sb.Append(dt.Value.Month.ToString("00") + '.');
                    if (ignoreDay)
                    {
                        sb.Append("??");
                    }
                    else
                    {
                        sb.Append(dt.Value.Day.ToString("00"));
                    }
                }

                return sb.ToString();
            }
        }

        /// <summary>
        /// Converts a PGN string "xxxx.xx.xx" to a date.
        /// </summary>
        /// <param name="pgn"></param>
        /// <returns></returns>
        public static DateTime? GetDateFromPgnString(string pgn)
        {
            if (string.IsNullOrEmpty(pgn))
            {
                return null;
            }

            DateTime? dt = null;
            string[] tokens = pgn.Split('.');

            int year;
            int month = 1;
            int day = 1;

            if (int.TryParse(tokens[0], out year))
            {
                if (tokens.Length > 1)
                {
                    if (int.TryParse(tokens[1], out month))
                    {
                        if (tokens.Length > 2)
                        {
                            if (!int.TryParse(tokens[2], out day))
                            {
                                day = 1;
                            }
                        }
                    }
                    else
                    {
                        month = 1;
                    }
                }
                dt = new DateTime(year, month, day);
            }

            return dt;
        }

        /// <summary>
        /// Massages a pgn date string into a more display friendly value
        /// i.e. removing the question marks indicating missing value.
        /// </summary>
        /// <param name="pgn"></param>
        /// <returns></returns>
        public static string BuildDateFromDisplayFromPgnString(string pgn)
        {
            StringBuilder sb = new StringBuilder("");
            GetYearMonthDayFromPgnDate(pgn, out int year, out int month, out int day);
            if (year > 0)
            {
                sb.Append(year.ToString());
                if (month > 0)
                {
                    sb.Append("." + month.ToString());
                    if (day > 0)
                    {
                        sb.Append("." + day.ToString());
                    }
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Builds a configuration file  line in the form of key=value.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string BuildKeyValueLine(string key, string value)
        {
            return key + "=" + value;
        }

        /// <summary>
        /// Builds a configuration file line for a boolean attribute.
        /// The values of true/false will be coded as 1/0.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string BuildKeyValueLine(string key, bool value)
        {
            return BuildKeyValueLine(key, value ? "1" : "0");
        }

        /// <summary>
        /// Builds a configuration file  line in the form of key=value.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string BuildKeyValueLine(string key, object value)
        {
            return BuildKeyValueLine(key, value == null ? "" : value.ToString());
        }

        /// <summary>
        /// Parses the PGN string to extract year, month and day.
        /// For not found values, returns -1.
        /// </summary>
        /// <param name="date"></param>
        public static void GetYearMonthDayFromPgnDate(string pgn, out int year, out int month, out int day)
        {
            year = -1;
            month = -1;
            day = -1;

            if (!string.IsNullOrEmpty(pgn))
            {
                string[] tokens = pgn.Split('.');
                if (int.TryParse(tokens[0], out year))
                {
                    if (tokens.Length > 1)
                    {
                        if (int.TryParse(tokens[1], out month))
                        {
                            if (tokens.Length > 2)
                            {
                                if (!int.TryParse(tokens[2], out day))
                                {
                                    day = -1;
                                }
                            }
                        }
                        else
                        {
                            month = -1;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Extracts the integer value from a string that has it
        /// as a suffix following the last underscore.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="lastChar"></param>
        /// <returns></returns>
        public static int GetIdFromPrefixedString(string s, char lastChar = '_')
        {
            if (string.IsNullOrEmpty(s))
            {
                return -1;
            }

            int nodeId = -1;

            int lastCharPos = s.LastIndexOf(lastChar);
            if (lastCharPos >= 0 && lastCharPos < s.Length - 1)
            {
                if (!int.TryParse(s.Substring(lastCharPos + 1), out nodeId))
                {
                    nodeId = -1;
                }
            }

            return nodeId;
        }

        /// <summary>
        /// Returns prefix of the string i.e. the substring
        /// from the start to the last occurence of the separator,
        /// inclusive.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="separ"></param>
        /// <returns></returns>
        public static string GetPrefixFromPrefixedString(string str, char separ = '_')
        {
            if (string.IsNullOrEmpty(str))
            {
                return "";
            }

            int lastCharPos = str.LastIndexOf(separ);
            if (lastCharPos > 0 && lastCharPos < str.Length - 1)
            {
                return str.Substring(0, lastCharPos + 1);
            }
            else
            {
                return str;
            }
        }

        /// <summary>
        /// Assumes the input string in the form "_run_comment_article_ref_<nodeId>_<articleRef>"
        /// Splits the input string by '_' and processes the last 2 tokens.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="articleRef"></param>
        /// <returns></returns>
        public static int GetNodeIdAndArticleRefFromPrefixedString(string str, out string articleRef)
        {
            int nodeId = -1;
            articleRef = null;

            if (!string.IsNullOrEmpty(str))
            {
                string[] tokens = str.Split('_');
                int len = tokens.Length;
                if (len >= 3)
                {
                    if (!int.TryParse(tokens[len-2], out nodeId))
                    {
                        nodeId = -1;
                    }
                    else
                    {
                        articleRef = tokens[len-1];
                    }
                }
            }

            return nodeId;
        }

        /// <summary>
        /// Returns the PieceColor value corresponding to the passed string.
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static PieceColor ConvertStringToPieceColor(string color)
        {
            if (string.IsNullOrWhiteSpace(color))
            {
                return PieceColor.None;
            }

            if (color.ToLower() == "white")
            {
                return PieceColor.White;
            }
            else if (color.ToLower() == "black")
            {
                return PieceColor.Black;
            }
            else
            {
                return PieceColor.None;
            }
        }

        /// <summary>
        /// Returns a string representing with the leading spaces
        /// of the passed string.
        /// Returns an empty string if no leading spaces found.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string GetLeadingSpaces(string text)
        {
            if (text == null)
                return "";

            int spaceCount = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == ' ')
                {
                    spaceCount++;
                }
                else
                {
                    break;
                }
            }

            if (spaceCount == 0)
            {
                return "";
            }
            else
            {
                return text.Substring(0, spaceCount);
            }
        }

        /// <summary>
        /// Uses Guid to generatean alphanumeric string starting with 'R'
        /// as names in many contexts must start with a letter.
        /// </summary>
        /// <returns></returns>
        public static string GenerateRandomElementName()
        {
            return "R" + Guid.NewGuid().ToString("N");
        }

        /// <summary>
        /// Changes an old GUID to the new format.
        /// The dashes in the old GUID are breaking.
        /// </summary>
        /// <returns></returns>
        public static string ConvertOldGuid(string oldGuid)
        {
            if (!string.IsNullOrEmpty(oldGuid))
            {
                return "R" + oldGuid.Replace("-", "");
            }
            else
            {
                return "";
            }
        }
    }
}
