using ChessPosition.Utils;
using GameTree;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChessPosition
{
    public class MoveUtils
    {
        /// <summary>
        /// Moves a piece from one square to another.
        /// </summary>
        /// <param name="xOrig"></param>
        /// <param name="yOrig"></param>
        /// <param name="xDest"></param>
        /// <param name="yDest"></param>
        /// <param name="board"></param>
        public static void MovePiece(byte xOrig, byte yOrig, byte xDest, byte yDest, ref byte[,] board)
        {
            board[xDest, yDest] = board[xOrig, yOrig];
            board[xOrig, yOrig] = 0;
        }

        /// <summary>
        /// Creates a new node in the game tree for the next move
        /// to be stored with the position.
        /// TODO: remove dupe from PgnGameParser
        /// </summary>
        /// <param name="algMove"></param>
        /// <param name="move"></param>
        /// <param name="parentNode"></param>
        /// <param name="parentSideToMove"></param>
        /// <returns></returns>
        public static TreeNode CreateNewNode(string algMove, MoveData move, TreeNode parentNode, PieceColor parentSideToMove, int nodeId)
        {
            TreeNode newNode = new TreeNode(parentNode, algMove, nodeId);

            // copy the board from the parent
            newNode.Position.Board = (byte[,])parentNode.Position.Board.Clone();

            if (parentSideToMove == PieceColor.White)
            {
                newNode.Position.MoveNumber = parentNode.Position.MoveNumber + 1;
                newNode.Position.ColorToMove = PieceColor.Black;
            }
            else
            {
                newNode.Position.MoveNumber = parentNode.Position.MoveNumber;
                newNode.Position.ColorToMove = PieceColor.White;
            }

            if (move.IsCaptureOrPawnMove())
            {
                newNode.Position.HalfMove50Clock = 0;
            }
            else
            {
                newNode.Position.HalfMove50Clock += 1;
            }

            if (!string.IsNullOrEmpty(move.Nag))
            {
                newNode.AddNag(move.Nag);
            }

            return newNode;
        }

        /// <summary>
        /// Checks if the passed algebraic move notations
        /// indicates a capture or a pawn move.
        /// </summary>
        /// <param name="alg"></param>
        /// <returns></returns>
        public static bool IsCaptureOrPawnMove(string alg)
        {
            if (string.IsNullOrEmpty(alg) || alg.Length < 2)
            {
                return false;
            }

            if (alg.IndexOf('x') > 0 || Char.IsLower(alg[0]))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Creates a TreeNode for the passed move, givent the parent's node/position.
        /// </summary>
        /// <param name="algMove"></param>
        /// <param name="parentNode"></param>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        public static TreeNode ProcessAlgMove(string algMove, TreeNode parentNode, int nodeId)
        {
            PieceColor parentSideToMove = parentNode.ColorToMove;

            MoveData move;
            bool nullMove = false;
            bool proceed = true;

            if (algMove == "Z0" || algMove == Constants.NULL_MOVE_NOTATION)
            {
                algMove = Constants.NULL_MOVE_NOTATION;
                move = new MoveData();
                move.Color = parentSideToMove;
                nullMove = true;
            }
            else
            {
                // check for bad PGN with '0-0' instead of 'O-O' castling
                if (algMove.Length > 1 && algMove[0] == '0')
                {
                    algMove = algMove.Replace('0', 'O');
                }

                PgnMoveParser pmp = new PgnMoveParser();
                int suffixLen = pmp.ParseAlgebraic(algMove, parentSideToMove);
                if (suffixLen < 0)
                {
                    proceed = false;
                    move = null;
                }
                else
                {
                    // remove suffix from algMove
                    algMove = algMove.Substring(0, algMove.Length - suffixLen);
                    move = pmp.Move;
                    algMove = TextUtils.StripCheckOrMateChar(algMove);
                }
            }

            if (proceed)
            {
                // create a new node
                TreeNode newNode = CreateNewNode(algMove, move, parentNode, parentSideToMove, nodeId);
                // the below is strictly not necessary since CleanupNullMove() will set it, so just future proofing
                newNode.IsNullMove = nullMove;

                if (!nullMove)
                {
                    SetDynamicAttrs(move, newNode);
                }
                else
                {
                    CleanupNullMove(ref newNode);
                }

                return newNode;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Sets the dynamic attributes of the position after the move has been made.
        /// Updates the position by performing the move on the board.
        /// </summary>
        /// <param name="move"></param>
        /// <param name="node"></param>
        /// <exception cref="Exception"></exception>
        private static void SetDynamicAttrs(MoveData move, TreeNode node)
        {
            if (move.IsCheckmate)
            {
                node.Position.IsCheckmate = true;
            }
            else if (move.IsCheck)
            {
                node.Position.IsCheck = true;
            }

            try
            {
                // Make the move on it
                MakeMove(node.Position, move);
            }
            catch
            {
                throw new Exception(TextUtils.BuildErrortext(node, node.LastMoveAlgebraicNotation));
            }

            // do the postprocessing
            PositionUtils.UpdateCastlingRights(ref node.Position, move, false);
            PositionUtils.SetEnpassantSquare(ref node.Position, move);
        }

        /// <summary>
        /// Resets certain attributes as appropriate for the null move.
        /// </summary>
        /// <param name="nd"></param>
        public static void CleanupNullMove(ref TreeNode nd)
        {
            if (nd != null)
            {
                nd.LastMoveAlgebraicNotation = Constants.NULL_MOVE_NOTATION;
                nd.IsNullMove = true;
                nd.LastMoveEngineNotation = "";
                nd.Position.InheritedEnPassantSquare = 0;
                nd.Position.EnPassantSquare = 0;
                nd.Position.IsCheck = false;
                nd.Position.IsCheckmate = false;
                nd.Position.HalfMove50Clock += 1;
            }
        }

        /// <summary>
        /// Checks if the passed node is a null move leaf (i.e. a null move with no children).
        /// </summary>
        /// <param name="nd"></param>
        /// <returns></returns>
        public static bool IsNullLeafMove(TreeNode nd)
        {
            if (nd != null && nd.IsNullMove && nd.Children.Count == 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if 2 algebraic notations are identical (having removed any check or mate signs)
        /// </summary>
        /// <param name="move1"></param>
        /// <param name="move2"></param>
        /// <returns></returns>
        public static bool AreAlgMovesIdentical(string move1, string move2)
        {
            return TextUtils.StripCheckOrMateChar(move1) == TextUtils.StripCheckOrMateChar(move2);
        }

        /// <summary>
        /// Makes the passed move on the supplied Position after
        /// verifying that it is legal and not ambiguous.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public static void MakeMove(BoardPosition position, MoveData move)
        {
            // for each piece, that can potentially move, verify the legality of the move
            List<SquareCoords> goodOrigins = new List<SquareCoords>();
            List<byte[,]> goodOriginBoards = new List<byte[,]>();

            byte[,] boardBeforeMove = (byte[,])position.Board.Clone();

            //special case of castling
            if (move.CastlingType != 0)
            {
                PerformCastling(position, move);
            }
            else
            {
                PiecesTargetingSquare sa = new PiecesTargetingSquare((byte)move.Destination.Xcoord, (byte)move.Destination.Ycoord, move.Origin.Xcoord, move.Origin.Ycoord, move.Color, ref position, move.MovingPiece);

                foreach (Square sq in sa.Candidates)
                {
                    position.Board = (byte[,])boardBeforeMove.Clone();

                    bool isEnPassant = PositionUtils.IsEnPassantCapture(position, move);

                    //place the candidate piece on the destination square
                    //If this is a promotion, set the piece type appropriately
                    PositionUtils.PlacePieceOnBoard(move.PromoteToPiece == PieceType.None ? move.MovingPiece : move.PromoteToPiece,
                                      move.Color, (byte)move.Destination.Xcoord, (byte)move.Destination.Ycoord, ref position.Board);

                    if (isEnPassant)
                    {
                        // remove the pawn captured en passant, 
                        // it is the pawn one rank below the target (en passant) square
                        PositionUtils.ClearSquare((byte)move.Destination.Xcoord, (byte)(move.Destination.Ycoord + ((move.Color == PieceColor.White) ? -1 : 1)), ref position.Board);
                    }

                    //and remove the current one.
                    PositionUtils.ClearSquare((byte)sq.Location.Xcoord, (byte)sq.Location.Ycoord, ref position.Board);

                    if (PositionUtils.IsKingSafe(position, move.Color))
                    {
                        goodOrigins.Add(sq.Location);
                        goodOriginBoards.Add((byte[,])position.Board.Clone());
                    }
                }

                // if there is just one, we are good,
                // if none, then the move is invalid
                // if more than one, the move is ambiguous
                if (goodOrigins.Count == 0)
                {
                    LocalizedStrings.Values.TryGetValue(LocalizedStrings.StringId.CannotIdentifyPiece, out string msg);
                    throw new Exception(msg);
                }
                else if (goodOrigins.Count > 1)
                {
                    LocalizedStrings.Values.TryGetValue(LocalizedStrings.StringId.AmbiguousMove, out string msg);
                    throw new Exception(msg);
                }
                else
                {
                    position.Board = (byte[,])goodOriginBoards[0].Clone();
                    move.Origin.Xcoord = goodOrigins[0].Xcoord;
                    move.Origin.Ycoord = goodOrigins[0].Ycoord;

                    position.LastMove.Origin = new SquareCoords(move.Origin.Xcoord, move.Origin.Ycoord);
                    position.LastMove.Destination = new SquareCoords(move.Destination.Xcoord, move.Destination.Ycoord);

                    position.LastMove.PiecePromotedTo = move.PromoteToPiece;
                }
            }
        }

        /// <summary>
        /// Builds a string for a move either standalone e.g. "9. Na4" or "9... Na5" 
        /// or as part of game text where Black's moves come without the number and elipsis.
        /// </summary>
        /// <param name="nd"></param>
        /// <returns></returns>
        public static string BuildSingleMoveText(TreeNode nd, bool isStandalone, bool withNAGs, uint moveNumberOffset)
        {
            if (nd == null)
            {
                return "";
            }

            if (nd.NodeId == 0)
            {
                // special case that may occur in evaluation
                LocalizedStrings.Values.TryGetValue(LocalizedStrings.StringId.StartingPosition, out string msg);
                return msg;
            }

            StringBuilder sb = new StringBuilder();
            if (nd.ColorToMove == PieceColor.Black)
            {
                sb.Append((nd.MoveNumber + moveNumberOffset).ToString() + ".");
            }
            else if (isStandalone)
            {
                sb.Append((nd.MoveNumber + moveNumberOffset).ToString() + "...");
            }

            sb.Append(nd.GetGuiPlyText(withNAGs));
            return sb.ToString();
        }

        /// <summary>
        /// Builds move text string from the passed move parameters.
        /// </summary>
        /// <param name="algMove"></param>
        /// <param name="moveNumber"></param>
        /// <param name="colorToMove"></param>
        /// <returns></returns>
        public static string BuildStandaloneMoveText(string algMove, uint moveNumber, PieceColor colorToMove)
        {
            if (string.IsNullOrEmpty(algMove))
            {
                return "";
            }

            string sMove;
            if (colorToMove == PieceColor.White)
            {
                sMove = moveNumber.ToString() + "." + algMove;
            }
            else
            {
                sMove = moveNumber.ToString() + "..." + algMove;
            }

            sMove = Languages.MapPieceSymbols(sMove, colorToMove);
            return sMove;
        }

        /// <summary>
        /// Given a move with a number in front of it,
        /// returns the number and the color.
        /// Returns -1 if fails to parse.
        /// </summary>
        /// <param name="move"></param>
        /// <returns></returns>
        public static int ExtractMoveNumber(string move, out PieceColor color)
        {
            int number = -1;
            color = PieceColor.None;

            if (string.IsNullOrEmpty(move))
            {
                return number;
            }

            // find the first dot
            int dot = move.IndexOf('.');
            if (dot > 0)
            {
                if (dot == move.Length - 1 || move[dot + 1] != '.')
                {
                    color = PieceColor.White;
                }
                else
                {
                    color = PieceColor.Black;
                }
                if (int.TryParse(move.Substring(0, dot), out number) == false)
                {
                    number = -1;
                }
            }

            return number;
        }

        /// <summary>
        /// Builds text of the line leading to the passed node.
        /// </summary>
        /// <param name="nd"></param>
        /// <returns></returns>
        public static string BuildStemText(TreeNode nd, uint moveNumberOffset)
        {
            StringBuilder sb = new StringBuilder();
            while (nd != null && nd.Parent != null)
            {
                sb.Insert(0, BuildSingleMoveText(nd, nd.Parent.NodeId == 0, false, moveNumberOffset) + " ");
                nd = nd.Parent;
            }

            return sb.ToString();
        }

        /// <summary>
        /// Builds text of the main line from the first move after the passed node
        /// to the end.
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="plyCount"></param>
        /// <returns></returns>
        public static string BuildTailText(TreeNode nd, uint moveNumberOffset, out int plyCount)
        {
            plyCount = 0;

            if (nd == null || nd.Children.Count == 0)
            {
                return "";
            }

            nd = nd.Children[0];

            StringBuilder sb = new StringBuilder();
            while (nd != null)
            {
                sb.Insert(0, BuildSingleMoveText(nd, nd.Parent.NodeId == 0 || plyCount == 0, false, moveNumberOffset) + " ");
                plyCount++;

                if (nd.Children.Count > 0)
                {
                    nd = nd.Children[0];
                }
                else
                {
                    nd = null;
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Performs castling on the passed BoardPosition object.
        /// Throws an exception if castling is illegal.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="move"></param>
        /// <exception cref="Exception"></exception>
        public static void PerformCastling(BoardPosition position, MoveData move)
        {
            // the legality must be checked BEFORE making the move
            // so that we don't allow castling when king is in check or
            // there are pieces on any of the square the king and rook move over.
            bool legal = PositionUtils.IsCastlingLegal(move.CastlingType, move.Color, position);
            if (!legal)
            {
                LocalizedStrings.Values.TryGetValue(LocalizedStrings.StringId.IllegalCastling, out string msg);
                throw new Exception(msg);
            }
            if (move.Color == PieceColor.White)
            {
                PositionUtils.ClearSquare(4, 0, ref position.Board);
                if ((move.CastlingType & Constants.WhiteKingsideCastle) != 0)
                {
                    PositionUtils.PlacePieceOnBoard(PieceType.King, move.Color, 6, 0, ref position.Board);
                    PositionUtils.PlacePieceOnBoard(PieceType.Rook, move.Color, 5, 0, ref position.Board);
                    PositionUtils.ClearSquare(7, 0, ref position.Board);

                    position.LastMove.Origin = new SquareCoords(4, 0);
                    position.LastMove.Destination = new SquareCoords(6, 0);

                    position.LastMove.OriginSecondary = new SquareCoords(7, 0);
                    position.LastMove.DestinationSecondary = new SquareCoords(5, 0);
                }
                else
                {
                    PositionUtils.PlacePieceOnBoard(PieceType.King, move.Color, 2, 0, ref position.Board);
                    PositionUtils.PlacePieceOnBoard(PieceType.Rook, move.Color, 3, 0, ref position.Board);
                    PositionUtils.ClearSquare(0, 0, ref position.Board);

                    position.LastMove.Origin = new SquareCoords(4, 0);
                    position.LastMove.Destination = new SquareCoords(2, 0);

                    position.LastMove.OriginSecondary = new SquareCoords(7, 0);
                    position.LastMove.DestinationSecondary = new SquareCoords(3, 0);
                }
            }
            else
            {
                PositionUtils.ClearSquare(4, 7, ref position.Board);
                if ((move.CastlingType & Constants.BlackKingsideCastle) != 0)
                {
                    PositionUtils.PlacePieceOnBoard(PieceType.King, move.Color, 6, 7, ref position.Board);
                    PositionUtils.PlacePieceOnBoard(PieceType.Rook, move.Color, 5, 7, ref position.Board);
                    PositionUtils.ClearSquare(7, 7, ref position.Board);

                    position.LastMove.Origin = new SquareCoords(4, 7);
                    position.LastMove.Destination = new SquareCoords(6, 7);

                    position.LastMove.OriginSecondary = new SquareCoords(7, 7);
                    position.LastMove.DestinationSecondary = new SquareCoords(5, 7);
                }
                else
                {
                    PositionUtils.PlacePieceOnBoard(PieceType.King, move.Color, 2, 7, ref position.Board);
                    PositionUtils.PlacePieceOnBoard(PieceType.Rook, move.Color, 3, 7, ref position.Board);
                    PositionUtils.ClearSquare(0, 7, ref position.Board);

                    position.LastMove.Origin = new SquareCoords(4, 7);
                    position.LastMove.Destination = new SquareCoords(2, 7);

                    position.LastMove.OriginSecondary = new SquareCoords(7, 7);
                    position.LastMove.DestinationSecondary = new SquareCoords(3, 7);
                }
            }
        }


        /// <summary>
        /// Given an engine style notation of origin square and destination square
        /// like "g1f3" (followed by a piece symbol if promoting e.g. g2g1q), produce
        /// a short algebraic notation string
        /// </summary>
        /// <param name="engMove"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        ///
        public static string EngineNotationToAlgebraic(string engMove, ref BoardPosition pos, out bool isCastle)
        {
            isCastle = false;

            if (engMove.Length < 4 || engMove.Length > 5)
            {
                LocalizedStrings.Values.TryGetValue(LocalizedStrings.StringId.InvalidEngineMoveReceived, out string msg);
                throw new Exception(msg + ": " + engMove);
            }

            string engOrig = engMove.Substring(0, 2);
            string engDest = engMove.Substring(2, 2);
            char engOriginFile = engMove[0];
            char engOriginRank = engMove[1];
            char engDestFile = engMove[2];
            char engDestRank = engMove[3];

            char promoteToChar = ' ';

            PieceType promoteToPiece = PieceType.None;
            if (engMove.Length == 5)
            {
                promoteToChar = char.ToUpper(engMove[4]);
                promoteToPiece = FenParser.FenCharToPiece[promoteToChar];
            }

            MoveData move = new MoveData();
            move.Origin.Xcoord = (int)engOriginFile - (int)'a';
            move.Origin.Ycoord = (int)engOriginRank - (int)'1';

            move.Destination.Xcoord = (int)engDestFile - (int)'a';
            move.Destination.Ycoord = (int)engDestRank - (int)'1';

            move.Color = pos.ColorToMove;

            SquareCoords orig = PositionUtils.ConvertAlgebraicToXY(engOrig);
            SquareCoords dest = PositionUtils.ConvertAlgebraicToXY(engDest);

            // find out the piece type
            PieceType piece = PositionUtils.GetPieceType(pos.Board[orig.Xcoord, orig.Ycoord]);
            move.MovingPiece = piece;
            bool isCapture = PositionUtils.GetPieceType(pos.Board[dest.Xcoord, dest.Ycoord]) == PieceType.None ? false : true;
            move.IsCapture = isCapture;

            string alg = "";

            // check castling first
            if (piece == PieceType.King)
            {
                switch (engMove)
                {
                    case "e1g1":
                    case "e8g8":
                    case "e1h1": // lichess 960 
                    case "e8h8": // lichess 960
                        alg = "O-O";
                        move.CastlingType = move.Color == PieceColor.White ? Constants.WhiteKingsideCastle : Constants.BlackKingsideCastle;
                        isCastle = true;
                        break;
                    case "e1c1":
                    case "e8c8":
                    case "e1a1": // lichess 960 
                    case "e8a8": // lichess 960 
                        alg = "O-O-O";
                        move.CastlingType = move.Color == PieceColor.White ? Constants.WhiteQueensideCastle : Constants.BlackQueensideCastle;
                        isCastle = true;
                        break;
                }
            }

            if (alg.Length == 0)
            {
                if (piece == PieceType.None)
                {
                    alg = "???";
                }
                else if (piece == PieceType.Pawn && engOriginFile != engDestFile)
                {
                    // pawn capture (including en passant)
                    alg = engOriginFile + "x" + engDest;
                    if (promoteToPiece != PieceType.None)
                    {
                        alg = alg + '='.ToString() + promoteToChar.ToString();
                        move.PromoteToPiece = promoteToPiece;
                    }
                }
                else
                {
                    // how many pieces of this type can come to the destination
                    PiecesTargetingSquare sa = new PiecesTargetingSquare((byte)dest.Xcoord, (byte)dest.Ycoord, -1, -1, pos.ColorToMove, ref pos, piece);
                    if (sa.Candidates.Count == 1)
                    {
                        alg = BuildAlgebraicMove(piece, promoteToPiece, engDest, isCapture, "");
                    }
                    else if (sa.Candidates.Count > 1)
                    {
                        // check if all are valid and determine if origin hint must be included
                        List<SquareCoords> validOrigs = LegalOrigins(ref pos, piece, promoteToPiece, dest, sa.Candidates);
                        if (validOrigs.Count == 0)
                        {
                        }
                        else if (validOrigs.Count == 1)
                        {
                            alg = BuildAlgebraicMove(piece, promoteToPiece, engDest, isCapture, "");
                        }
                        else
                        {
                            // more than 1 option so need to disambiguate
                            string algOrig = BuildDisambiguationPrefix(orig, validOrigs);
                            alg = BuildAlgebraicMove(piece, promoteToPiece, engDest, isCapture, algOrig);
                        }
                    }
                    else
                    {
                        alg = "???";
                    }
                }
            }

            move.PromoteToPiece = promoteToPiece;
            MakeMove(pos, move);
            pos.EnPassantSquare = 0;
            PositionUtils.SetEnpassantSquare(ref pos, move);
            PositionUtils.UpdateCastlingRights(ref pos, move, true);

            return alg;
        }

        /// <summary>
        /// Converts engine notation to origin and destination coordinates.
        /// It only needs the first 4 characters of the move.
        /// </summary>
        /// <param name="engMove"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <exception cref="Exception"></exception>
        public static void EngineNotationToCoords(string engMove, out SquareCoords from, out SquareCoords to, out PieceType promoteTo)
        {
            promoteTo = PieceType.None;

            if (engMove.Length < 4 || engMove.Length > 5)
            {
                LocalizedStrings.Values.TryGetValue(LocalizedStrings.StringId.InvalidEngineMoveReceived, out string msg);
                throw new Exception(msg + ": " + engMove);
            }

            char engOriginFile = engMove[0];
            char engOriginRank = engMove[1];
            char engDestFile = engMove[2];
            char engDestRank = engMove[3];

            int xFrom = (int)engOriginFile - (int)'a';
            int yFrom = (int)engOriginRank - (int)'1';
            from = new SquareCoords(xFrom, yFrom);

            int xTo = (int)engDestFile - (int)'a';
            int yTo = (int)engDestRank - (int)'1';
            to = new SquareCoords(xTo, yTo);

            if (engMove.Length == 5)
            {
                promoteTo = FenParser.FenCharToPiece[char.ToUpper(engMove[4])];
            }
        }

        /// <summary>
        /// Returns the color opposite to the one passed.
        /// </summary>
        /// <param name="col"></param>
        /// <returns></returns>
        public static PieceColor ReverseColor(PieceColor col)
        {
            if (col == PieceColor.None)
                return col;

            return col == PieceColor.White ? PieceColor.Black : PieceColor.White;
        }

        /// <summary>
        /// Given the actual origin and a list of origins to disambiguate from with a shortest notation, checks whether 
        /// it is sufficient to:
        /// 1. indicate the origin file
        /// 2. indicate the origin rank
        /// 3. indicate both (i.e. provide full notation)
        /// </summary>
        /// <param name="origs"></param>
        /// <returns></returns>
        private static string BuildDisambiguationPrefix(SquareCoords orig, List<SquareCoords> origs)
        {
            string disamb = "";
            int origFile = orig.Xcoord;
            int origRank = orig.Ycoord;
            int count = 0;
            foreach (SquareCoords or in origs)
            {
                if (or.Xcoord == origFile)
                    count++;
            }
            if (count > 1)
            {
                count = 0;
                foreach (SquareCoords or in origs)
                {
                    if (or.Ycoord == origRank)
                        count++;
                }
                if (count > 1)
                {
                    // need full notation
                    disamb = ((char)(orig.Xcoord + (int)'a')).ToString() +
                             ((char)(orig.Ycoord + (int)'1')).ToString();
                }
                else
                {
                    disamb = ((char)(orig.Ycoord + (int)'1')).ToString();
                }
            }
            else
            {
                disamb = ((char)(orig.Xcoord + (int)'a')).ToString();
            }

            return disamb;
        }

        private static string BuildAlgebraicMove(PieceType piece, PieceType promoteTo, string engDest, bool isCapture, string disambPrefix)
        {
            //TODO implement check and mate signalling too
            char pieceChar = FenParser.PieceToFenChar[piece];
            string alg = (piece != PieceType.Pawn ? pieceChar.ToString() : "") + disambPrefix + (isCapture ? "x" : "")
                + engDest + (promoteTo == PieceType.None ? "" : ("=" + (FenParser.PieceToFenChar[promoteTo]).ToString()));
            return alg;
        }

        /// <summary>
        /// Find all origin squares from which a piece of a given type can go to the
        /// specified destination producing a legal position.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="movingPiece"></param>
        /// <param name="promoteToPiece"></param>
        /// <param name="dest"></param>
        /// <param name="candidateOrigins"></param>
        /// <returns></returns>
        private static List<SquareCoords> LegalOrigins(ref BoardPosition position, PieceType movingPiece, PieceType promoteToPiece, SquareCoords dest, List<Square> candidateOrigins)
        {
            List<SquareCoords> goodOrigins = new List<SquareCoords>();

            byte[,] boardBeforeMove = (byte[,])position.Board.Clone();

            foreach (Square sq in candidateOrigins)
            {
                position.Board = (byte[,])boardBeforeMove.Clone();

                //place the candidate piece on the destination square
                //If this is a promotion, set the piece type appropriately
                PositionUtils.PlacePieceOnBoard(promoteToPiece == PieceType.None ? movingPiece : promoteToPiece,
                                  position.ColorToMove, (byte)dest.Xcoord, (byte)dest.Ycoord, ref position.Board);

                //and remove the current one.
                PositionUtils.ClearSquare((byte)sq.Location.Xcoord, (byte)sq.Location.Ycoord, ref position.Board);

                if (PositionUtils.IsKingSafe(position, position.ColorToMove))
                {
                    goodOrigins.Add(sq.Location);
                }
            }

            position.Board = (byte[,])boardBeforeMove.Clone();
            return goodOrigins;
        }

    }
}
