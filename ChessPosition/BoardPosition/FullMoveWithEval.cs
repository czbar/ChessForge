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
        /// <summary>
        /// Numer of the move (starting from 1) 
        /// that this object represents.
        /// </summary>
        public string Number { get; set; }

        /// <summary>
        /// Id of the Node for the White ply 
        /// </summary>
        public int WhiteNodeId;

        /// <summary>
        /// Id of the Node for the Blck ply 
        /// </summary>
        public int BlackNodeId;

        // White's ply move notation
        private string _whitePly;

        // Black's ply move notation
        private string _blackPly;

        // evaluation after White's move
        private string _whiteEval;

        // evaluation after Black's move
        private string _blackEval;

        /// <summary>
        /// White's ply in algebraic notation.
        /// </summary>
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

        /// <summary>
        /// Black's ply in algebraic notation.
        /// </summary>
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
                    _whiteEval = AddPlusSign(_whiteEval);
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
                    _blackEval = AddPlusSign(_blackEval);
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// PropertChange event handler
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        private string AddPlusSign(string val)
        {
            if (val != null && val.Length > 1 && Char.IsDigit(val[0]))
            {
                return "+" + val;
            }
            else
            {
                return val;
            }
        }

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
