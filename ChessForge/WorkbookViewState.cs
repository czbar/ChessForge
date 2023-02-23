using ChessPosition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ChessForge
{
    /// <summary>
    /// Encapsulates the state of the views of the Workbook so that it can be saved
    /// and reflected upon re-opening.
    /// </summary>
    public class WorkbookViewState
    {
        // Workbook represented by this object
        private Workbook _workbook;

        // Key strings for key/value configuration items
        private readonly string ACTIVE_TAB = "ActiveTab";
        private readonly string ACTIVE_CHAPTER_INDEX = "ActiveChapterIndex";
        private readonly string CHAPTER_INDEX = "ChapterIndex";

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="workbook"></param>
        public WorkbookViewState(Workbook workbook) 
        { 
            _workbook = workbook;
        }

        /// <summary>
        /// Saves the state of the view in the view config file.
        /// </summary>
        public void SaveState()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine(TextUtils.BuildKeyValueLine(ACTIVE_TAB, AppState.ActiveTab));
                sb.AppendLine(TextUtils.BuildKeyValueLine(ACTIVE_CHAPTER_INDEX, _workbook.ActiveChapterIndex));
                sb.AppendLine();

                for (int i = 0; i < _workbook.Chapters.Count; i++)
                {
                    sb.AppendLine(TextUtils.BuildKeyValueLine(CHAPTER_INDEX, i));
                    ChapterViewState cvs = new ChapterViewState(_workbook.Chapters[i], i == _workbook.ActiveChapterIndex);
                    sb.AppendLine(cvs.ToString());
                }

                string filePath = BuildViewConfigFilePath();
                File.WriteAllText(filePath, sb.ToString());
            }
            catch (Exception ex)
            {
                AppLog.Message("SaveState()", ex);
            }
        }

        /// <summary>
        /// Determines the path for the view configuration file by replacing
        /// the workbook's extension (".pgn") witht ".cf_"
        /// </summary>
        /// <returns></returns>
        private string BuildViewConfigFilePath()
        {
            string directoryPath = Path.GetDirectoryName(AppState.WorkbookFilePath);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(AppState.WorkbookFilePath);
            return Path.Combine(directoryPath, fileNameWithoutExtension) + ".cf_";
        }
    }
}
