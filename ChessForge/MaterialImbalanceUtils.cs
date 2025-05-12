using ChessPosition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChessForge
{
    /// <summary>
    /// Utility class to calculate material imbalance in a chess position.
    /// </summary>
    public class MaterialImbalanceUtils
    {
        /// <summary>
        /// Flag to indicate if the dictionaries have been initialized.
        /// </summary>
        private static bool _isInitialized = false;

        /// <summary>
        /// Dictionary to hold the difference in piece counts for each piece type.
        /// </summary>
        private static Dictionary<int, int> _dictPieceDiffs = new Dictionary<int, int>();

        /// <summary>
        /// Dictionary to map piece types to their figurine representations.
        /// </summary>
        private static Dictionary<PieceType, char> _dictPieceToFigurine = new Dictionary<PieceType, char>();

        /// <summary>
        /// Builds strings indicating material imbalance
        /// to be displayed above the Main Chessboard.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="white"></param>
        /// <param name="black"></param>
        public static void BuildMaterialImbalanceStrings(BoardPosition position, out string white, out string black)
        {
            if (!_isInitialized)
            {
                InitializeDictionaries();
            }

            StringBuilder sbWhite = new StringBuilder("+");
            StringBuilder sbBlack = new StringBuilder("+");

            CalculateImbalance(position);

            ProcessPieceImbalance(PieceType.King, sbWhite, sbBlack);
            ProcessPieceImbalance(PieceType.Queen, sbWhite, sbBlack);
            ProcessPieceImbalance(PieceType.Rook, sbWhite, sbBlack);
            ProcessPieceImbalance(PieceType.Bishop, sbWhite, sbBlack);
            ProcessPieceImbalance(PieceType.Knight, sbWhite, sbBlack);
            ProcessPieceImbalance(PieceType.Pawn, sbWhite, sbBlack);

            white = sbWhite.Length > 1 ? sbWhite.ToString() : "";
            black = sbBlack.Length > 1 ? sbBlack.ToString() : "";
        }

        /// <summary>
        /// Calculates the material imbalance in the given board position.
        /// </summary>
        /// <param name="position"></param>
        public static void CalculateImbalance(BoardPosition position)
        {
            ClearImbalanceDictionary();

            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    byte piece = position.Board[i, j];
                    if (piece != 0)
                    {
                        if ((piece & Constants.Color) != 0)
                        {
                            piece = (byte)(piece & ~Constants.Color);
                            _dictPieceDiffs[piece]++;
                        }
                        else
                        {
                            _dictPieceDiffs[piece]--;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Updates imbalance strings for a single piece.
        /// </summary>
        /// <param name="piece"></param>
        /// <param name="sbWhite"></param>
        /// <param name="sbBlack"></param>
        private static void ProcessPieceImbalance(PieceType piece, StringBuilder sbWhite, StringBuilder sbBlack)
        {
            string sub = GetStringForPiece(piece, out bool isWhiteAdvantage);
            if (sub.Length > 0)
            {
                if (isWhiteAdvantage)
                {
                    sbWhite.Append(sub);
                }
                else
                {
                    sbBlack.Append(sub);
                }
            }
        }

        /// <summary>
        /// Gets the string representation of the imbalance 
        /// for a single piece type.
        /// </summary>
        /// <param name="piece"></param>
        /// <param name="isWhiteAdvantage"></param>
        /// <returns></returns>
        private static string GetStringForPiece(PieceType piece, out bool isWhiteAdvantage)
        {
            int count = _dictPieceDiffs[Constants.PieceToFlag[piece]];
            isWhiteAdvantage = count > 0;
            return new string(_dictPieceToFigurine[piece], Math.Abs(count));
        }

        /// <summary>
        /// Initializes the dictionaries used for material imbalance calculations.
        /// </summary>
        private static void InitializeDictionaries()
        {
            _dictPieceDiffs = new Dictionary<int, int>();

            foreach (var piece in Constants.PieceToFlag.Keys)
            {
                _dictPieceDiffs.Add(Constants.PieceToFlag[piece], 0);
            }

            _dictPieceToFigurine = new Dictionary<PieceType, char>
            {
                { PieceType.King, Languages.WhiteFigurinesMapping['K'] },
                { PieceType.Queen, Languages.WhiteFigurinesMapping['Q'] },
                { PieceType.Rook, Languages.WhiteFigurinesMapping['R'] },
                { PieceType.Bishop, Languages.WhiteFigurinesMapping['B'] },
                { PieceType.Knight, Languages.WhiteFigurinesMapping['N'] },
                { PieceType.Pawn, Languages.WhiteFigurinesMapping['P'] }
            };

            _isInitialized = true;
        }

        /// <summary>
        /// Clears the imbalance dictionary.
        /// </summary>
        private static void ClearImbalanceDictionary()
        {
            foreach (var key in _dictPieceDiffs.Keys.ToList())
            {
                _dictPieceDiffs[key] = 0;
            }
        }
    }
}
