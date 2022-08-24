using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessPosition
{
    /// <summary>
    /// Represents a move (White and Black plies in one object)
    /// with the evaluation data.
    /// </summary>
    public class MoveWithEval : INotifyPropertyChanged
    {
        // Move number
        public string Number { get; set; }


        private string _whitePly;
        private string _blackPly;

        // evaluation after White's move
        private string _whiteEval;

        // evaluation after Black's move
        private string _blackEval;

        // White's move in algebraic notation
        public string WhitePly
        {
            get
            {
                return _whitePly;
            }
            set
            {
                if (value != _whitePly)
                {
                    _whitePly = value;
                    NotifyPropertyChanged();
                }
            }
        }

        // Black's move in algebraic notation
        public string BlackPly
        {
            get
            {
                return _blackPly;
            }
            set
            {
                if (value != _blackPly)
                {
                    _blackPly = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Evaluation of the position after White's
        /// move in centipawns (converted to string)
        /// </summary>
        public string WhiteEval
        {
            get
            {
                return _whiteEval;
            }
            set
            {
                if (value != _whiteEval)
                {
                    _whiteEval = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Evaluation of the position after Black's
        /// move in centipawns (converted to string)
        /// </summary>
        public string BlackEval
        {
            get
            {
                return _blackEval;
            }
            set
            {
                if (value != _blackEval)
                {
                    _blackEval = value;
                    NotifyPropertyChanged();
                }
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
