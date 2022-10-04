using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ChessPosition.GameTree
{
    /// <summary>
    /// Holds game metadata obtained from a PGN file.
    /// It is bound to the ListView in SelectGames dialog.
    /// </summary>
    public class GameMetadata : INotifyPropertyChanged
    {
        /// <summary>
        /// Types of "games" that can be encountered.
        /// In the Chess Forge file, we should only encounter
        /// WORKBOOK_PREFACE, MODEL_GAME and EXERCISE.
        /// In a non Chess Forge, we only expect GENERIC_GAME
        /// and GENERIC_EXERCISE
        /// </summary>
        public enum GameType
        {
            INVALID,
            WORKBOOK_PREFACE,
            STUDY_TREE,
            MODEL_GAME,
            EXERCISE,
            GENERIC_GAME,
            GENERIC_EXERCISE
        }

        /// <summary>
        /// The list of headers. 
        /// </summary>
        private List<KeyValuePair<string, string>> _headers = new List<KeyValuePair<string, string>>();


        // whether this game is selected in the GUI
        private bool _isSelected;

        /// <summary>
        /// Creates the object and sets IsSelected to true.
        /// The games in ListView are selected by default.
        /// </summary>
        public GameMetadata()
        {
            IsSelected = true;
        }

        /// <summary>
        /// Checks if there is at least one header processed 
        /// for this game.
        /// </summary>
        /// <returns></returns>
        public bool HasAnyHeader()
        {
            return _headers.Count > 0;
        }

        /// <summary>
        /// Checks if this game represents a Study Tree.
        /// </summary>
        /// <returns></returns>
        public bool IsStudyTree()
        {
            return GetHeaderValue(PgnHeaders.NAME_CONTENT_TYPE) == PgnHeaders.VALUE_STUDY_TREE;
        }

        /// <summary>
        /// Returns the content type of this game based on the header
        /// or, in the absence of the ContentType header, based on the content.
        /// In the latter case, the game will be considered a "Model Game" unless
        /// we have a FEN header Making it an "Exercise".
        /// </summary>
        /// <returns></returns>
        public GameType GetContentType()
        {
            string value = GetHeaderValue(PgnHeaders.NAME_CONTENT_TYPE);
            switch (value)
            {
                case PgnHeaders.VALUE_STUDY_TREE:
                    return GameType.STUDY_TREE;
                case PgnHeaders.VALUE_MODEL_GAME:
                    return GameType.MODEL_GAME;
                case PgnHeaders.VALUE_EXERCISE:
                    return GameType.EXERCISE;
                default:
                    if (!string.IsNullOrWhiteSpace(GetHeaderValue(PgnHeaders.NAME_FEN)))
                    {
                        return GameType.EXERCISE;
                    }
                    else
                    {
                        return GameType.MODEL_GAME;
                    }
            }
        }

        /// <summary>
        /// Text of the game
        /// </summary>
        public string GameText { get; set; }

        /// <summary>
        /// Accessor to _isSelected.
        /// This is the only property that can be changed
        /// from the GUI.
        /// </summary>
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                _isSelected = value;
                NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Returns the value of header with a given name.
        /// Null if header not found.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string GetHeaderValue(string name)
        {
            var header = _headers.Where(kvp => kvp.Key == name).FirstOrDefault();
            return header.Value;
        }

        /// <summary>
        /// Saves header's name and value. 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void SetHeaderValue(string name, string value)
        {
            var header = _headers.Where(kvp => kvp.Key == name).FirstOrDefault();
            if (!header.Equals(default(KeyValuePair<string, string>)))
            {
                _headers.Remove(header);
            }

            header = new KeyValuePair<string, string>(name, value);
            _headers.Add(header);
        }

        /// <summary>
        /// Returns the title of the Workbook
        /// </summary>
        /// <returns></returns>
        public string GetWorkbookTitle()
        {
            return _headers.Where(kvp => kvp.Key == PgnHeaders.NAME_WORKBOOK_TITLE).FirstOrDefault().Value;
        }

        /// <summary>
        /// Returns the title of the chapter.
        /// </summary>
        /// <returns></returns>
        public string GetChapterTitle()
        {
            return _headers.Where(kvp => kvp.Key == PgnHeaders.NAME_EVENT).FirstOrDefault().Value;
        }

        /// <summary>
        /// Returns the result of a tree/game/combinattion
        /// </summary>
        /// <returns></returns>
        public string GetResult()
        {
            return _headers.Where(kvp => kvp.Key == PgnHeaders.NAME_RESULT).FirstOrDefault().Value;
        }

        /// <summary>
        /// Returns the training side value.
        /// </summary>
        /// <returns></returns>
        public string GetTrainingSide()
        {
            return _headers.Where(kvp => kvp.Key == PgnHeaders.NAME_TRAINING_SIDE).FirstOrDefault().Value;
        }

        /// <summary>
        /// Returns the number of the chapter or 0 if not found or invalid.
        /// </summary>
        /// <returns></returns>
        public int GetChapterNumber()
        {
            string sChapterNo = _headers.Where(kvp => kvp.Key == PgnHeaders.NAME_CHAPTER_NUMBER).FirstOrDefault().Value;
            if (int.TryParse(sChapterNo, out int chapterNumber))
            {
                return chapterNumber;
            }
            else
            {
                return 0;
            }
        }

        //
        // PGN header names that Chess Forge recognizes.
        //
        public string WorkbookTitle { get; set; }

        public string TrainingSide { get; set; }

        public string ContentType { get; set; }

        public string ChapterNo { get; set; }

        public string Event { get; set; }

        public string Site { get; set; }

        public string Date { get; set; }

        public string Round { get; set; }

        public string White { get; set; }

        public string Black { get; set; }

        public string Result { get; set; }

        /// <summary>
        /// Index of the first line in the PGN file where this game starts
        /// (to be precise, the first empty line after the previous game)
        /// </summary>
        public int FirstLineInFile { get; set; }

        /// <summary>
        /// Builds text for the column with the name of the game.
        /// </summary>
        public string Players
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(White + " - " + Black);
                if (!string.IsNullOrEmpty(Event) && Event != "?")
                {
                    sb.Append(" at " + Event + "");
                }
                if (!string.IsNullOrEmpty(Round) && Round != "?")
                {
                    sb.Append(" Rd." + Round + " ");
                }
                return sb.ToString();
            }
        }

        /// <summary>
        /// PropertChange event handler
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Notifies the framework of the change in the bound data.
        /// </summary>
        /// <param name="propertyName"></param>
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
