using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessPosition
{
    /// <summary>
    /// Encodes the complete information about the position, 
    /// including dynamic properties, 
    /// as a small object that will be created for each leaf
    /// in the PGN training tree.
    /// In particular, it can be used to generate FEN 
    /// and/or to check if 2 positions are identical.
    /// </summary>
    [Serializable()]
    public class BoardPosition
    {
        /// <summary>
        /// Represents the static aspect of the position
        /// i.e. location of all the pieces.
        /// In each call, MSB represents the color of the piece
        /// (1 for White, 0 for Black)
        /// and the lower 4 bits identify the piece as follows
        ///   0 - none
        ///   1 - pawn
        ///   2 - knight
        ///   3 - bishop
        ///   4 - rook
        ///   5 - queen
        ///   6 - king
        /// </summary>
        // internal representation of the board
        public byte[,] Board;

        /// <summary>
        /// The MSB indicates the color of the side to move.
        /// 1 (0x80) means White to move; 0 means Black to move.
        /// 
        /// The 4 least significant bits represent castling rights
        ///   bit 3: White can castle kingside
        ///   bit 2: White can castke queenside
        ///   bit 1: Black can castle kingside
        ///   bit 0: Black can castke queenside
        /// 
        /// </summary>
        public byte DynamicProperties;

        /// <summary>
        /// The part of DynamicProperties representing castling rights.
        /// </summary>
        public byte CastlingRights
        {
            get => (byte)(DynamicProperties & 0x0F);
        }

        /// <summary>
        /// 4 MS bits represent the xPos (column/file)
        /// 4 LS bits represent the yPos (row/rank)
        ///   both "halves" can have values 0-7
        /// 
        /// E.g. 01110010 (0x72) represents h3
        /// 
        /// This is the square "inherited" from the parent position
        /// so than we can check if en passant capture is legal
        /// in this position.
        /// </summary>
        public byte InheritedEnPassantSquare;

        /// <summary>
        /// The en passant square in the current position
        /// (i.e available to the next move)
        /// </summary>
        public byte EnPassantSquare;

        /// <summary>
        /// The number of halfmoves since the last capture or pawn advance, used for the fifty-move rule.
        /// </summary>
        public uint HalfMove50Clock;

        /// <summary>
        /// The number of the full move. It starts at 1, and is incremented after Black's move.
        /// </summary>
        public uint MoveNumber;

        /// <summary>
        /// The move that immediately led to the position in this node.
        /// If that was a White's move, the color to move in the position will be Black
        /// and vice versa.
        /// </summary>
        public MoveUI LastMove = new MoveUI();

        /// <summary>
        /// Flags a check to the king of the side on move
        /// </summary>
        public bool IsCheck = false;

        /// <summary>
        /// Flags checkmate to the side that would move next
        /// </summary>
        public bool IsCheckmate = false;

        /// <summary>
        /// Flags stalemate to the side that would move next
        /// </summary>
        public bool IsStalemate = false;

        /// <summary>
        /// Flags insufficient material to the side that would move next
        /// </summary>
        public bool IsInsufficientMaterial = false;

        /// <summary>
        /// Constructor.
        /// Creates a new board array.
        /// </summary>
        public BoardPosition()
        {
            Board = new byte[8, 8];
        }

        /// <summary>
        /// Position of the pieces on the board.
        /// </summary>
        /// <param name="board"></param>
        public BoardPosition(byte[,] board)
        {
            Board = (byte[,])board.Clone();
        }

        /// <summary>
        /// Partial copy constructor. Makes a copy of the board
        /// and some fields.
        /// </summary>
        /// <param name="position"></param>
        public BoardPosition(BoardPosition position)
        {
            Board = (byte[,])position.Board.Clone();

            this.DynamicProperties = position.DynamicProperties;
            this.InheritedEnPassantSquare = position.InheritedEnPassantSquare;
            this.EnPassantSquare = position.EnPassantSquare;
            this.HalfMove50Clock = position.HalfMove50Clock;
            this.MoveNumber = position.MoveNumber;

            this.IsCheck = position.IsCheck;
            this.IsCheckmate = position.IsCheckmate;

            this.LastMove = position.LastMove.CloneMe();
        }

        /// <summary>
        /// Color of the side that is to move in this position.
        /// This is the opposite color to that which made the LastMove. 
        /// </summary>
        public PieceColor ColorToMove
        {
            get => (DynamicProperties & 0x80) != 0 ? PieceColor.White : PieceColor.Black;
            set
            {
                DynamicProperties = (value == PieceColor.White ? DynamicProperties |= 0x80 : DynamicProperties &= 0x7F);
            }
        }
    }
}

