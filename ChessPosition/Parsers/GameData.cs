using ChessPosition;
using ChessPosition.Utils;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GameTree
{
    /// <summary>
    /// Holds game metadata obtained from a PGN file.
    /// It is bound to the ListView in SelectGames dialog.
    /// </summary>
    public class GameData : INotifyPropertyChanged
    {
        /// <summary>
        /// Types of "games" that can be encountered.
        /// In the Chess Forge file, we should only encounter
        /// WORKBOOK_PREFACE, STUDY_TREE, MODEL_GAME and EXERCISE.
        /// In a non Chess Forge, we only expect GENERIC.
        /// </summary>
        public enum ContentType
        {
            NONE,
            GENERIC,
            STUDY_TREE,
            MODEL_GAME,
            EXERCISE,
            INTRO,
            UNKNOWN,
            ANY             // special to use when want to handle both GAMES and EXCERCISES
        }

        // whether this game is selected in the GUI
        private bool _isSelected;

        /// <summary>
        /// Flags whether the GameText has already been processed.
        /// </summary>
        public bool IsProcessed = false;

        /// <summary>
        /// Creates the object and sets IsSelected to true.
        /// The games in ListView are selected by default.
        /// </summary>
        public GameData()
        {
            IsSelected = true;
        }

        /// <summary>
        /// The header data for this game. 
        /// </summary>
        public GameHeader Header = new GameHeader();

        /// <summary>
        /// Checks if there is at least one header processed 
        /// for this game.
        /// </summary>
        /// <returns></returns>
        public bool HasAnyHeader()
        {
            return Header.HasAnyHeader();
        }

        /// <summary>
        /// Checks if this game represents a Study Tree.
        /// </summary>
        /// <returns></returns>
        public bool IsStudyTree()
        {
            return Header.GetContentType(out _) == GameData.ContentType.STUDY_TREE;
        }

        /// <summary>
        /// Returns the content type of this game based on the header
        /// or, in the absence of the ContentType header, based on the content.
        /// In the latter case, the game will be considered a "Model Game" unless
        /// we have a FEN header Making it an "Exercise".
        /// </summary>
        /// <returns></returns>
        public ContentType GetContentType(bool set)
        {
            ContentType typ = Header.GetContentType(out _);

            if (typ == ContentType.GENERIC)
            {
                if (Header.IsExercise())
                {
                    typ = ContentType.EXERCISE;
                }
                else
                {
                    typ = ContentType.MODEL_GAME;
                    if (set)
                    {
                        Header.SetContentType(typ);
                    }
                }
            }

            return typ;
        }

        /// <summary>
        /// Text of the game
        /// </summary>
        public string GameText { get; set; }

        /// <summary>
        /// Number that the client may set if it wants to have displayed back.
        /// </summary>
        public string OrderNo { get; set; }

        /// <summary>
        /// The property that binds in the SelectGames ListView control.
        /// </summary>
        public string GameTitleForList
        {
            get
            {
                ContentType typ = GetContentType(false);
                string prefix = string.Empty;
                if (typ == ContentType.GENERIC || typ == ContentType.MODEL_GAME)
                {
                    LocalizedStrings.Values.TryGetValue(LocalizedStrings.StringId.Game, out string msg);
                    prefix = msg + ": ";
                }
                else if (typ == ContentType.EXERCISE)
                {
                    LocalizedStrings.Values.TryGetValue(LocalizedStrings.StringId.Exercise, out string msg);
                    prefix = msg + ": ";
                }
                return prefix + Header.BuildGameHeaderLine(typ != ContentType.EXERCISE);
            }
        }

        /// <summary>
        /// Game title without prefix for display
        /// </summary>
        public string GameTitle
        {
            get
            {
                return Header.BuildGameHeaderLine(true, true, false);
            }
        }

        /// <summary>
        /// Date string for display
        /// </summary>
        public string Date
        {
            get
            {
                string date = Header.GetDate(out _);
                return TextUtils.BuildDateFromDisplayFromPgnString(date);
            }
        }

        /// <summary>
        /// ECO code for display
        /// </summary>
        public string ECO
        {
            get
            {
                return Header.GetECO(out _);
            }
        }

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
        /// Returns the title of the Workbook
        /// </summary>
        /// <returns></returns>
        public string GetWorkbookTitle()
        {
            return Header.GetWorkbookTitle();
        }

        /// <summary>
        /// Returns the name of the Annotator's name.
        /// This is only called for the first "game"
        /// in the PGN (i.e. Workbook data)
        /// </summary>
        /// <returns></returns>
        public string GetAnnotator()
        {
            return Header.GetAnnotator();
        }

        /// <summary>
        /// Returns the version of the Workbook
        /// </summary>
        /// <returns></returns>
        public string GetWorkbookVersion()
        {
            return Header.GetWorkbookVersion();
        }

        /// <summary>
        /// Returns the Guid
        /// </summary>
        /// <returns></returns>
        public string GetGuid()
        {
            return Header.GetGuid(out _);
        }

        /// <summary>
        /// Index of the first line in the PGN file where this game starts
        /// (to be precise, the first empty line after the previous game)
        /// </summary>
        public int FirstLineInFile { get; set; }

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
