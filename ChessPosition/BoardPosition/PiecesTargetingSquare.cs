using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessPosition
{
    /// <summary>
    /// Analyses the position to identify pieces
    /// of a specified color that attack (or can move to)
    /// a specified square.
    /// A piece will be considered a "legal" attacker if
    /// 1. it can move to the square without jumping over another piece 
    ///    (knights excepted, of course)
    /// 2. the destination square is either empty or occupied by
    ///    an opposition piece
    /// </summary>
    public class PiecesTargetingSquare
    {
        // list of pieces attacking the square
        public List<Square> Candidates = new List<Square>();

        readonly public byte TargetSquare;


        readonly public bool IsInheritedEnPassantSquare;

        // A pawn that can move to the square.
        // There could only be one such pawn or none.
        //        public List<Square> PawnMovers = new List<Square>();

        // the position being analyzed
        readonly public BoardPosition Position;

        // x coordinate of the square to attack
        readonly private byte XposTarget;
        // y coordinate of the square to attack
        readonly private byte YposTarget;

        // x coordinate of the starting square
        readonly private int XposOrigin;
        // y coordinate of the starting square
        readonly private int YposOrigin;

        // color of the attacking pieces
        readonly private PieceColor CandidateColor;

        readonly private PieceType CandidatePieceType;

        readonly private bool AttackCheckOnly;

        /// <summary>
        /// Initializes this object, analyzes the board
        /// and populates the Attackers list.
        /// The client will call this constructor
        /// and then read the attackers list. 
        /// </summary>
        /// <param name="xPosTarget"></param>
        /// <param name="yPosTarget"></param>
        /// <param name="attackerColor"></param>
        /// <param name="position"></param>
        public PiecesTargetingSquare(byte xPosTarget, byte yPosTarget, int xPosOrigin, int yPosOrigin, PieceColor attackerColor, ref BoardPosition position, PieceType moverPiece = PieceType.None, bool attackCheckOnly = false)
        {
            this.Position = position;
            this.XposTarget = xPosTarget;
            this.YposTarget = yPosTarget;
            this.XposOrigin = xPosOrigin;
            this.YposOrigin = yPosOrigin;
            this.Position = position;
            this.CandidateColor = attackerColor;
            this.CandidatePieceType = moverPiece;
            this.AttackCheckOnly = attackCheckOnly;

            this.TargetSquare = Position.Board[XposTarget, YposTarget];

            // check if the target square is occupied by a piece of attacker color, if so return
            if (IsAttackerPiece(TargetSquare))
            {
                return;
            }

            IsInheritedEnPassantSquare = position.InheritedEnPassantSquare != 0 && position.InheritedEnPassantSquare == (byte)(XposTarget << 4 | YposTarget);

            FindMoversAttackers();

            if (AttackCheckOnly)
            {
                //only bother if no attacker found yet
                if (!IsTargetSquareAttacked())
                {
                    FindPawnAttackers();
                }
            }
            else
            {
                if (CandidatePieceType == PieceType.Pawn)
                {
                    FindPawnMovers();
                }
            }
        }

        // We will look in 8 directions, rows, columns,
        // diagonals.
        //
        // If AttackCheckOnly is set to true,
        // we exit as soon as the first attacker has been found.
        // When checking for attackers we don't care which piece is
        // attacking.
        //
        // We are only checking the non-pawn pieces here 
        // which move and attack the same way (unlike pawns).
        // Pawns are handled in a another method.
        private void FindMoversAttackers()
        {
            int xIncrement = 0;
            int yIncrement = 1;

            if (AttackCheckOnly || CandidatePieceType == PieceType.Queen || CandidatePieceType == PieceType.Rook || CandidatePieceType == PieceType.King)
            {
                FindLineAttackers(xIncrement, yIncrement);
                if (IsTargetSquareAttacked() && AttackCheckOnly)
                {
                    return;
                }
            }

            xIncrement = 1;
            yIncrement = 0;

            if (AttackCheckOnly || CandidatePieceType == PieceType.Queen || CandidatePieceType == PieceType.Rook || CandidatePieceType == PieceType.King)
            {
                FindLineAttackers(xIncrement, yIncrement);
                if (IsTargetSquareAttacked() && AttackCheckOnly)
                {
                    return;
                }
            }

            xIncrement = 1;
            yIncrement = -1;

            if (AttackCheckOnly || CandidatePieceType == PieceType.Queen || CandidatePieceType == PieceType.Bishop || CandidatePieceType == PieceType.King)
            {
                FindLineAttackers(xIncrement, yIncrement);
                if (IsTargetSquareAttacked() && AttackCheckOnly)
                {
                    return;
                }
            }

            xIncrement = 1;
            yIncrement = 1;

            if (AttackCheckOnly || CandidatePieceType == PieceType.Queen || CandidatePieceType == PieceType.Bishop || CandidatePieceType == PieceType.King)
            {
                FindLineAttackers(xIncrement, yIncrement);
                if (IsTargetSquareAttacked() && AttackCheckOnly)
                {
                    return;
                }
            }

            if (AttackCheckOnly || CandidatePieceType == PieceType.Knight)
            {
                FindAttackingKnights();
            }

        }

        /// <summary>
        /// Check if we have an origin hint, and if so, whether it matches the
        /// candidate's square.
        /// </summary>
        /// <param name="xPos"></param>
        /// <param name="yPos"></param>
        private void AddCandidate(Square sq)
        {
            if ((XposOrigin < 0 || XposOrigin == sq.Location.Xcoord) && (YposOrigin < 0 || YposOrigin == sq.Location.Ycoord))
            {
                if (sq.pieceType == PieceType.None || sq.pieceType == CandidatePieceType || (CandidatePieceType == PieceType.None && AttackCheckOnly))
                {
                    Candidates.Add(sq);
                }
            }
        }

        /// <summary>
        /// A pawn can move one aquare up or 2 if it is still
        /// on the second rank and nothing sits in front of it.
        /// It can also move by executing a capture to the side and up,
        /// if there is an enemy piece there.
        /// </summary>
        private void FindPawnAttackers()
        {
            int vertIncrement = (CandidateColor == PieceColor.White) ? -1 : 1;

            // check for the pawn below and to the left
            int pawnYPos = YposTarget + vertIncrement;
            int pawnXPos = XposTarget + 1;

            if (PositionUtils.AreValidCoordinates(pawnXPos, pawnYPos))
            {
                PieceType originPiece = PositionUtils.GetPieceType(Position.Board[pawnXPos, pawnYPos]);
                if (originPiece == PieceType.Pawn && IsAttackerPiece(Position.Board[pawnXPos, pawnYPos]))
                {
                    AddCandidate(new Square(pawnXPos, pawnYPos));
                }
            }

            if (IsTargetSquareAttacked())
                return;

            // check for the pawn above and to the right
            pawnXPos = XposTarget - 1;

            if (PositionUtils.AreValidCoordinates(pawnXPos, pawnYPos))
            {
                PieceType originPiece = PositionUtils.GetPieceType(Position.Board[pawnXPos, pawnYPos]);
                if (originPiece == PieceType.Pawn && IsAttackerPiece(Position.Board[pawnXPos, pawnYPos]))
                {
                    AddCandidate(new Square(pawnXPos, pawnYPos));
                }
            }
        }

        /// <summary>
        /// Check if there are any pawns that can move to the target square.
        /// A pawn can make a move to the target square if:
        /// 1. the target is 1 square above and unoccupied.
        /// 2. the pawn is on 2nd rank, the target is 2 squares above and the 2 squares above the pawn are unoccupied
        /// 3. the target square is occupied by an opponent's piece and the pawn can capture it.
        /// 4. the pawn can execute an en passant capture.
        /// </summary>
        private void FindPawnMovers()
        {
            int vertIncrement = (CandidateColor == PieceColor.White) ? -1 : 1;

            FindPawnForwardMovers(vertIncrement);
            FindPawnCapturers(vertIncrement);
        }

        private void FindPawnForwardMovers(int vertIncrement)
        {
            int pawnYPos = YposTarget + vertIncrement;

            if (PositionUtils.AreValidCoordinates(XposTarget, pawnYPos))
            {
                PieceType originPiece = PositionUtils.GetPieceType(Position.Board[XposTarget, pawnYPos]);

                // Check for a vertical move. The target square must be unoccupied
                if (PositionUtils.GetPieceType(Position.Board[XposTarget, YposTarget]) == PieceType.None)
                {
                    if (originPiece == PieceType.Pawn && IsAttackerPiece(Position.Board[XposTarget, pawnYPos]))
                    {
                        // we have our pawn
                        AddCandidate(new Square(XposTarget, pawnYPos));
                    }
                    else
                    {
                        // If the origin square tested above is empty but was on the third rank, check if there is a pawn on the second rank
                        // First make sure that the above third rank square was empty
                        if (PositionUtils.GetPieceType(Position.Board[XposTarget, pawnYPos]) == PieceType.None)
                        {
                            if (originPiece == PieceType.None && PositionUtils.GetRankNo(pawnYPos, CandidateColor) == 3)
                            {
                                pawnYPos += vertIncrement;
                                originPiece = PositionUtils.GetPieceType(Position.Board[XposTarget, pawnYPos]);
                                if (originPiece == PieceType.Pawn && IsAttackerPiece(Position.Board[XposTarget, pawnYPos]))
                                {
                                    AddCandidate(new Square(XposTarget, pawnYPos));
                                }
                            }
                        }
                    }
                }

            }
        }

        private void FindPawnCapturers(int vertIncrement)
        {
            // check for en passant and possible capture
            // (if en passant is set, we know it is "active", i.e. the attacker pawn
            // in the right position can go there, with a capture)
            if (IsInheritedEnPassantSquare || IsDefenderPiece(TargetSquare))
            {
                // check for the pawn below and to the left
                int pawnYPos = YposTarget + vertIncrement;
                int pawnXPos = XposTarget + 1;

                if (PositionUtils.AreValidCoordinates(pawnXPos, pawnYPos))
                {
                    PieceType originPiece = PositionUtils.GetPieceType(Position.Board[pawnXPos, pawnYPos]);
                    if (originPiece == PieceType.Pawn && IsAttackerPiece(Position.Board[pawnXPos, pawnYPos]))
                    {
                        AddCandidate(new Square(pawnXPos, pawnYPos));
                    }
                }

                // check for the pawn above and to the right
                pawnXPos = XposTarget - 1;

                if (PositionUtils.AreValidCoordinates(pawnXPos, pawnYPos))
                {
                    PieceType originPiece = PositionUtils.GetPieceType(Position.Board[pawnXPos, pawnYPos]);
                    if (originPiece == PieceType.Pawn && IsAttackerPiece(Position.Board[pawnXPos, pawnYPos]))
                    {
                        AddCandidate(new Square(pawnXPos, pawnYPos));
                    }
                }
            }
        }

        /// <summary>
        /// This method will be called 4 times as
        /// there are 4 lines of possible attack:
        /// horizontal, vertical and 2 diagonals.
        /// Each call will check both vectors for
        /// the given line.
        /// </summary>
        /// <param name="xIncrement"></param>
        /// <param name="yIncrement"></param>
        private void FindLineAttackers(int xIncrement, int yIncrement)
        {
            FindVectorAttackers(xIncrement, yIncrement);
            FindVectorAttackers(-1 * xIncrement, -1 * yIncrement);
        }

        /// <summary>
        /// Each line of attack has two directions (vectors) from
        /// which the attack may be coming.
        /// </summary>
        /// <param name="xIncrement"></param>
        /// <param name="yIncrement"></param>
        /// <returns></returns>
        private void FindVectorAttackers(int xIncrement, int yIncrement)
        {

            int x = (int)XposTarget;
            int y = (int)YposTarget;

            // checking for pawn attackers only in the first iteration so flag it
            bool firstIter = true;
            while (true)
            {
                x = x + xIncrement;
                y = y + yIncrement;
                if (!PositionUtils.AreValidCoordinates(y, x))
                    break;

                byte square = this.Position.Board[x, y];

                // if square is empty then skip to the next iteration
                // otherwise see if we have an attacker or defender
                // and what type
                if (square != 0)
                {

                    if (!IsAttackerPiece(square))
                    {
                        // we encountered a friendly piece
                        break;
                    }

                    // we have an enemy piece so check if this is the kind that can attack us on the current vector 
                    // we encountered an enemy piece so let's check the type
                    if (xIncrement == 0 || yIncrement == 0)
                    {
                        // we are checking a row or a column so we are looking
                        // for a rook or a queen
                        if ((square & Constants.PieceToFlag[PieceType.Queen]) != 0
                            || (square & Constants.PieceToFlag[PieceType.Rook]) != 0)
                        {
                            AddCandidate(new Square(x, y, PositionUtils.GetPieceType(square)));
                            break;
                        }
                    }
                    else
                    {
                        // we are checking a diagonal so we are looking
                        // for a bishop or a queen
                        if ((square & Constants.PieceToFlag[PieceType.Queen]) != 0
                            || (square & Constants.PieceToFlag[PieceType.Bishop]) != 0)
                        {
                            AddCandidate(new Square(x, y, PositionUtils.GetPieceType(square)));
                            break;
                        }

                        // check for attack by the pawn (only in this branch since it has to be on a diagonal)
                        if (firstIter)
                        {
                            if ((xIncrement == -1 || xIncrement == 1) && yIncrement == -1 && CandidateColor == PieceColor.White
                                ||
                                (xIncrement == -1 || xIncrement == 1) && yIncrement == 1 && CandidateColor == PieceColor.Black)
                            {
                                if ((square & Constants.PieceToFlag[PieceType.Pawn]) != 0)
                                {
                                    AddCandidate(new Square(x, y, PieceType.Pawn));
                                    // if the target square is occupied by a defender piece or is an en passant square,
                                    // the pawn can move there too
                                    if (IsDefenderPiece(TargetSquare) || IsInheritedEnPassantSquare)
                                    {
                                        AddCandidate(new Square(x, y, PieceType.Pawn));
                                    }
                                    break;
                                }
                            }
                        }

                    }

                    // check for attack by the king
                    if (firstIter)
                    {
                        if ((square & Constants.PieceToFlag[PieceType.King]) != 0)
                        {
                            AddCandidate(new Square(x, y, PieceType.King));
                            break;
                        }
                    }
                    // there was something on the square which prevents us from considering
                    // any pieces behind it.
                    // Exit the loop
                    break;
                }
                firstIter = false;
            }

        }

        /// <summary>
        /// There are max. 8 squares from which a knight can
        /// attack a given square.
        /// We need to move 2 squares away in 4 directions (N,E,S,W) and then
        /// 1 square to the side for each, ending up with 8 squares (NW, NE, EN, ES etc.)
        /// </summary>
        private void FindAttackingKnights()
        {
            CheckForKnight(2, 1);
            CheckForKnight(2, -1);
            CheckForKnight(-2, 1);
            CheckForKnight(-2, -1);
            CheckForKnight(1, 2);
            CheckForKnight(1, -2);
            CheckForKnight(-1, 2);
            CheckForKnight(-1, -2);
        }

        /// <summary>
        /// Checks if the knight of the attacker color
        /// can be found at a specified offset.
        /// </summary>
        /// <param name="xOffset"></param>
        /// <param name="yOffset"></param>
        private void CheckForKnight(int xOffset, int yOffset)
        {
            int x = (int)XposTarget + xOffset;
            int y = (int)YposTarget + yOffset;

            if (PositionUtils.AreValidCoordinates(x, y))
            {
                byte square = this.Position.Board[x, y];

                // is square non-empty
                if (square != 0)
                {
                    // does it have a knight of the attacking color on it
                    if (
                        ((square & Constants.PieceToFlag[PieceType.Knight]) != 0)
                        &&
                            (
                        ((square & Constants.Color) != 0) && this.CandidateColor == PieceColor.White
                        ||
                        (((square & Constants.Color) == 0) && this.CandidateColor == PieceColor.Black)
                        )
                        )
                    {
                        AddCandidate(new Square(x, y, PositionUtils.GetPieceType(square)));
                    }
                }
            }
        }

        public bool IsTargetSquareAttacked()
        {
            if (Candidates.Count > 0)
                return true;

            return false;
        }

        /// <summary>
        /// Checks if the piece defined by the passed
        /// byte value describes an attacker piece.
        /// In other words, is of the "attacking color".
        /// </summary>
        /// <param name="square"></param>
        /// <returns></returns>
        private bool IsAttackerPiece(byte square)
        {
            if (square == 0)
                return false;

            if (((square & Constants.Color) != 0) && this.CandidateColor == PieceColor.White
               ||
               ((square & Constants.Color) == 0) && this.CandidateColor == PieceColor.Black)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool IsDefenderPiece(byte square)
        {
            if (square == 0)
                return false;

            if (((square & Constants.Color) == 0) && this.CandidateColor == PieceColor.White
               ||
               ((square & Constants.Color) != 0) && this.CandidateColor == PieceColor.Black)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

    }
}
