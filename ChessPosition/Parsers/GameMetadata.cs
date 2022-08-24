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
        /// Text of the game
        /// </summary>
        public string GameText {get; set;}

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

        //
        // Properties available in PGN headers.
        // (We may never use some of them.)
        //
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
