using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

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
            WORKBOOK_PREFACE,
            STUDY_TREE,
            MODEL_GAME,
            EXERCISE,
        }

        // whether this game is selected in the GUI
        private bool _isSelected;

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
        public ContentType GetContentType()
        {
            ContentType typ = Header.GetContentType(out _);

            if (typ == ContentType.GENERIC)
            {
                if (!string.IsNullOrWhiteSpace(Header.GetFenString()))
                {
                    typ = ContentType.EXERCISE;
                }
                else
                {
                    typ = ContentType.MODEL_GAME;
                }
            }

            return typ;
        }

        /// <summary>
        /// Text of the game
        /// </summary>
        public string GameText { get; set; }

        /// <summary>
        /// The property that binds in the SelectGames ListView control.
        /// </summary>
        public string GameTitle
        {
            get
            {
                return Header.BuildGameHeaderLine();
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
        /// Returns the number of the chapter or 0 if not found or invalid.
        /// </summary>
        /// <returns></returns>
        public int GetChapterNumber()
        {
            return Header.GetChapterNumber();
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
