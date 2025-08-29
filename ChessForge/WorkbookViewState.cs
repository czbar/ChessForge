using ChessPosition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Windows.Input;

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
        private const string ACTIVE_TAB = "ActiveTab";
        private const string ACTIVE_CHAPTER_INDEX = "ActiveChapterIndex";
        private const string CHAPTER_INDEX = "ChapterIndex";

        // list of ChapterViewState objects
        private List<ChapterViewState> _chapterViewStates = new List<ChapterViewState>();

        /// <summary>
        /// Index of the active chapter
        /// </summary>
        public int ActiveChapterIndex = -1;

        // type of UI Tab that is open
        public TabViewType ActiveViewType;

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
                if (_workbook != null)
                {
                    StringBuilder sb = new StringBuilder();

                    TabViewType lastTabToSave = AppState.ActiveTab;
                    if (lastTabToSave == TabViewType.TRAINING)
                    {
                        lastTabToSave = WorkbookLocationNavigator.GetCurrentTab();
                    }
                    sb.AppendLine(TextUtils.BuildKeyValueLine(ACTIVE_TAB, lastTabToSave));
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
            }
            catch (Exception ex)
            {
                AppLog.Message("SaveState()", ex);
            }
        }

        /// <summary>
        /// Reads view states from the config file.
        /// </summary>
        public void ReadState()
        {
            try
            {
                int chapterIndex = -1;
                _chapterViewStates.Clear();
                string filePath = BuildViewConfigFilePath();
                if (File.Exists(filePath))
                {
                    string[] lines = File.ReadAllLines(filePath);

                    string key = "";
                    string value = "";
                    foreach (string line in lines)
                    {
                        if (ParseLine(line, out key, out value))
                        {
                            if (key == CHAPTER_INDEX)
                            {
                                chapterIndex++;
                                _chapterViewStates.Add(new ChapterViewState(_workbook.Chapters[chapterIndex], chapterIndex == ActiveChapterIndex));
                            }
                            else
                            {
                                if (chapterIndex < 0)
                                {
                                    ProcessWorkbookLine(key, value);
                                }
                                else
                                {
                                    ProcessChapterLine(chapterIndex, key, value);
                                }
                            }
                        }
                    }
                }
                ApplyStates();
            }
            catch (Exception ex)
            {
                AppLog.Message("ReadState()", ex);
            }
        }

        /// <summary>
        /// Sets selections and states on the Workbook and Chapter objects
        /// as per the processed data.
        /// </summary>
        private void ApplyStates()
        {
            for (int i = 0; i < _chapterViewStates.Count; i++)
            {
                ChapterViewState cvs = _chapterViewStates[i];
                Chapter chapter = WorkbookManager.SessionWorkbook.Chapters[i];

                if (i == ActiveChapterIndex)
                {
                    WorkbookManager.SessionWorkbook.ActiveChapter = chapter;
                }

                chapter.IsViewExpanded = cvs.IsExpanded;
                chapter.IsModelGamesListExpanded = cvs.IsGameListExpanded;
                chapter.IsExercisesListExpanded = cvs.IsExerciseListExpanded;
                chapter.ActiveModelGameIndex = chapter.VerifyGameIndex(cvs.ActiveGameIndex);
                chapter.ActiveExerciseIndex = chapter.VerifyExerciseIndex(cvs.ActiveExerciseIndex);

                chapter.VariationIndexDepth = cvs.VariationIndexDepth;
            }
        }

        /// <summary>
        /// Process a line applying the worbook 
        /// rather than a specific chapter
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        private void ProcessWorkbookLine(string key, string value)
        {
            switch (key)
            {
                case ACTIVE_TAB:
                    Enum.TryParse(value, out ActiveViewType);
                    break;
                case ACTIVE_CHAPTER_INDEX:
                    int.TryParse(value, out ActiveChapterIndex);
                    break;
            }
        }

        /// <summary>
        /// Processes a line that applies to a specific chapter.
        /// </summary>
        /// <param name="chapterIndex"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        private void ProcessChapterLine(int chapterIndex, string key, string value)
        {
            ChapterViewState cvs = _chapterViewStates[chapterIndex];
            cvs.ProcessConfigLine(key, value);
        }

        /// <summary>
        /// Parses an individual line from the cofig file.
        /// The line must be in the "key=value" format.
        /// </summary>
        /// <param name="line"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private bool ParseLine(string line, out string key, out string value)
        {
            key = "";
            value = "";

            if (string.IsNullOrEmpty(line))
            {
                return false;
            }

            string[] tokens = line.Split('=');
            if (tokens.Length != 2)
            {
                return false;
            }
            else
            {
                key = tokens[0];
                value = tokens[1];
                return true;
            }
        }

        /// <summary>
        /// Determines the path for the view configuration file by replacing
        /// the workbook's extension (".pgn") witht ".cf_"
        /// </summary>
        /// <returns></returns>
        private string BuildViewConfigFilePath()
        {
            try
            {
                string directoryPath = Path.GetDirectoryName(AppState.WorkbookFilePath);
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(AppState.WorkbookFilePath);
                return Path.Combine(directoryPath, fileNameWithoutExtension) + ".cf_";
            }
            catch
            {
                return "";
            }
        }
    }
}
