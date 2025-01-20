using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChessPosition;

namespace GameTree
{
    /// <summary>
    /// Parsers a single half move in PGN format.
    /// Throws an exception if the move's format is invalid.
    /// Populates the destination coordinates and sets move's 
    /// other features.
    /// If there is a rank or file of the origin coordinates 
    /// specified, it will be set too.
    /// </summary>
    class PgnMoveParser
    {
        // The PGN string that is being parsed.
        public string AlgebraicMove;

        // Holds processed data
        public MoveData Move;

        /// <summary>
        /// Parses the supplied move in algebraic notation
        /// and creates a MoveData object with the extracted
        /// details.
        /// </summary>
        /// <param name="alg"></param>
        /// <param name="color"></param>
        /// <returns>Number of chars that do not belong to core algebraic notation</returns>
        public int ParseAlgebraic(string alg, PieceColor color)
        {
            Move = new MoveData();

            Move.Color = color;
            AlgebraicMove = alg;

            if (CheckForCastling(alg))
            {
                return 0;
            }

            // the first character is a capital letter
            // representing the piece type.
            // If missing, this is a pawn move
            // (since we already checked for O-O and O-O-O above)
            string subStringToProcess = GetPieceType(alg, color);

            if (Move.MovingPiece != PieceType.None)
            {
                // check if there is a capture symbol ('x'),
                // if so, mark the move as a capture and remove 'x'
                // so we can process the rest as normal
                subStringToProcess = CheckForAndRemoveCapture(subStringToProcess);

                // get the destination Xcoord and any origin hints, if present
                subStringToProcess = CheckForOriginAndDestinationXcoord(subStringToProcess);

                // Now we may be left with a digit representing the target Ycoord
                // and/or suffixes (check, mate, promotions, ?, ! etc.)
                subStringToProcess = GetDestinationYcoord(subStringToProcess);

                if (Move.Destination.Xcoord >= 0 || Move.Destination.Ycoord >= 0)
                {
                    return ProcessSuffixes(subStringToProcess);
                }
                else
                {
                    return -1;
                }
            }
            else
            {
                return -1;
            }
        }


        /// <summary>
        /// Processes the remaining part of the move string.
        /// It may contain a check ('+') or mate ('#') character
        /// possibly preceded by a promotion ("=Q"). 
        /// It may have non-coded NAGs (i.e. "?", "!" and such) at the end.
        /// </summary>
        /// <param name="subStringToProcess"></param>
        /// <returns>Number of chars that do not belong to core algebraic notation</returns>
        private int ProcessSuffixes(string subStringToProcess)
        {
            if (subStringToProcess.Length == 0)
            {
                return 0;
            }

            subStringToProcess = CheckForPromotion(subStringToProcess);
            if (subStringToProcess.Length == 0)
            {
                return 0;
            }

            if (subStringToProcess[0] == '+')
            {
                Move.IsCheck = true;
                if (subStringToProcess.Length > 1)
                {
                    subStringToProcess = subStringToProcess.Substring(1);
                }
                else
                {
                    return 0;
                }
            }

            if (subStringToProcess[0] == '#')
            {
                Move.IsCheckmate = true;
                if (subStringToProcess.Length > 1)
                {
                    subStringToProcess = subStringToProcess.Substring(1);
                }
                else
                {
                    return 0;
                }
            }

            return CheckForNags(subStringToProcess);
        }

        /// <summary>
        /// Checks if the passed string contains promotion.
        /// If so process it and return the remainder.
        /// </summary>
        /// <param name="subStringToProcess"></param>
        /// <returns></returns>
        private string CheckForPromotion(string substring)
        {
            if (substring[0] == '=' && substring.Length > 1)
            {
                FenParser.FenCharToPiece.TryGetValue(substring[1], out Move.PromoteToPiece);
                if (substring.Length > 2)
                {
                    return substring.Substring(2);
                }
                else
                {
                    return "";
                }
            }
            else
            {
                return substring;
            }
        }

        /// <summary>
        /// Some version of PGN may have a NAG in an uncoded form
        /// i.e. ?, !, ??, !!, !?, ?!
        /// </summary>
        /// <param name="substring"></param>
        /// <returns>Number of chars that do not belong to core algebraic notation</returns>
        private int CheckForNags(string substring)
        {
            int nagId = Constants.GetNagIdFromString(substring);
            if (nagId > 0)
            {
                Move.Nag = "$" + nagId.ToString();
            }

            return substring.Length;
        }

        /// <summary>
        /// Convert the first char in the supplied string to 
        /// a destination Ycoord.
        /// </summary>
        /// <param name="algSubstring"></param>
        /// <returns></returns>
        private string GetDestinationYcoord(string algSubstring)
        {
            Move.Destination.Ycoord = (int)(algSubstring[0] - '1');

            return algSubstring.Substring(1);
        }

        /// <summary>
        /// Parses the middle portion of the move that make take the form of
        /// 1. e.g. "c5"   : the destination square
        /// 2. e.g. "bc5"  : the origin file and the destination square
        /// 3. e.g. "4c5"  : the origin rank and the destination square
        /// 4. e.g. "a4c5" : the origin square and the destination square 
        /// </summary>
        /// <param name="algSubstring">part of the move string to process</param>
        /// <returns></returns>
        private string CheckForOriginAndDestinationXcoord(string algSubstring)
        {
            // now the there will be one letter, 2 letters or a digit followed by
            // letter
            // if there is only one letter then that's the destination Xcoord (column or file),
            // if there are two, the first one is the Xcoord ot the origin and the second is 
            // is the destination's Xcoord
            // if there is a digit followed by a letter then the digit represents
            // the Ycoord of the origin and the letter is for the destination's Xcoord
            string first2Chars = algSubstring.Substring(0, 2);

            int lenProcessed = 0;

            // is first char a digit?
            if (Char.IsDigit(first2Chars[0]))
            {
                Move.Destination.Xcoord = first2Chars[1] - 'a';
                Move.Origin.Ycoord = first2Chars[0] - '1';
                lenProcessed = 2;
            }
            else if (!Char.IsDigit(first2Chars[1]))
            {
                Move.Destination.Xcoord = first2Chars[1] - 'a';
                Move.Origin.Xcoord = first2Chars[0] - 'a';
                lenProcessed = 2;
            }
            else
            {
                // here the 2 chars we are dealing will almost
                // always represent the target square e.g."c5"
                // However in extremely rare cases (3 pieces of the
                // same kind able to move to the destination square),
                // we may have a "full" notation like "a4c5" where
                // "a4" represents the origin square.
                // We'll check for that
                if (algSubstring.Length >= 4 && Char.IsLetter(algSubstring[2]) && Char.IsDigit(algSubstring[3]))
                {
                    Move.Origin.Xcoord = algSubstring[0] - 'a';
                    Move.Origin.Ycoord = algSubstring[1] - '1';
                    Move.Destination.Xcoord = algSubstring[2] - 'a';
                    Move.Destination.Ycoord = algSubstring[3] - '1';
                    lenProcessed = 3;
                }
                else
                {
                    Move.Destination.Xcoord = first2Chars[0] - 'a';
                    lenProcessed = 1;
                }
            }

            return algSubstring.Substring(lenProcessed);
        }

        /// <summary>
        /// Looks for 'x' which is the capture symbol.
        /// If found, marks the move as a capture and
        /// removes the 'x' symbol from the string
        /// </summary>
        /// <param name="algSubstring">Unprocessed part of the move string</param>
        /// <returns>The move string stripped of the 'x' charactre.</returns>
        private string CheckForAndRemoveCapture(string algSubstring)
        {
            // check if we have a capture i.e. the x character
            if (algSubstring.Contains('x'))
            {
                Move.IsCapture = true;
                // remove the 'x' as we no longer need it
                algSubstring = algSubstring.Replace("x","");
            }

            return algSubstring;
        }

        /// <summary>
        /// Determines the type of the piece that is making the move.
        /// An upper case latter will indicate the type of the piece.
        /// If the move starts with a lower case letter, it means that
        /// the piece type is skipped and we are dealing with a pawn move 
        /// (and that lower case letter is a start of the next section
        /// in the string). 
        /// </summary>
        /// <param name="alg"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        private string GetPieceType(string alg, PieceColor color)
        {
            string stringToProcess = alg;

            char p = alg[0];
            if (Char.IsLower(p))
            {
                Move.MovingPiece = PieceType.Pawn;
            }
            else
            {
                try
                {
                    Move.MovingPiece = FenParser.FenCharToPiece[p];
                    // remove the processed character from the string
                    stringToProcess = stringToProcess.Substring(1);
                }
                catch
                {
                    Move.MovingPiece = PieceType.None;
                }
            }

            return stringToProcess;
        }

        /// <summary>
        /// Castling is represented as O-O or O-O-O.
        /// We will accept 0-0 and 0-0-0 as well.
        /// It cannot be a capture or a promotion
        /// but could be a check, so check for that too.
        /// </summary>
        /// <param name="subStringToProcess"></param>
        /// <returns></returns>
        private bool CheckForCastling(string s)
        {
            bool isCastle = false;
            if (s.StartsWith("O-O-O") || s.StartsWith("0-0-0"))
            {
                Move.CastlingType |= Move.Color == PieceColor.White ? Constants.WhiteQueensideCastle : Constants.BlackQueensideCastle;
                isCastle = true;
            }
            else if (s.StartsWith("O-O") || s.StartsWith("0-0"))
            {
                Move.CastlingType |= Move.Color == PieceColor.White ? Constants.WhiteKingsideCastle : Constants.BlackKingsideCastle;
                isCastle = true;
            }

            if (isCastle && (s[s.Length - 1] == '+'))
            {
                Move.IsCheck = true;
            }

            return isCastle;
        }

#if false
        /// <summary>
        /// Castling has a special notation and special rules 
        /// so check for it first.
        /// </summary>
        /// <param name="move"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        private bool CheckCastling(string alg, PieceColor color)
        {
            if (alg == "O-O" || alg == "0-0")
            {
                // this is kingside castling
                if (color == PieceColor.White)
                {
                    // the white king is moving from e1 to g1 
                    Move.CastlingType = Constants.WhiteKingsideCastle;
                }
                else
                {
                    // the black king is moving from e8 to g8 
                    Move.CastlingType = Constants.BlackKingsideCastle;
                }
                return true;
            }
            else if (alg == "O-O-O" || alg == "0-0-0")
            {
                // this is quenside castling
                if (color == PieceColor.White)
                {
                    // the white king is moving from e1 to c1 
                    Move.CastlingType = Constants.WhiteQueensideCastle;
                }
                else
                {
                    // the black king is moving from e8 to c8 
                    Move.CastlingType = Constants.BlackQueensideCastle;
                }
                return true;
            }

            return false;
        }
#endif


    }
}
