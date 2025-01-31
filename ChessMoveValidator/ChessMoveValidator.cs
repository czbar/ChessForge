using System;
using System.Linq;

namespace ChessMoveValidator
{
    /// <summary>
    /// Implements move validator as initially implemented by DeepSeek.
    /// </summary>
    class ChessMoveValidator
    {
        /// <summary>
        /// Inteface method to validate a chess move.
        /// </summary>
        /// <param name="fen"></param>
        /// <param name="move"></param>
        /// <returns></returns>
        public static bool ValidateChessMove(string fen, string move)
        {
            // Parse FEN
            string[] fenParts = fen.Split(' ');
            string boardState = fenParts[0];
            char activeColor = fenParts[1][0]; // 'w' or 'b'
            string castlingRights = fenParts[2];
            string enPassantTarget = fenParts[3];
            int halfMoveClock = int.Parse(fenParts[4]);
            int fullMoveNumber = int.Parse(fenParts[5]);

            // Parse move
            int fromFile = move[0] - 'a'; // File (column) 0-7
            int fromRank = 8 - (move[1] - '0'); // Rank (row) 0-7
            int toFile = move[2] - 'a';
            int toRank = 8 - (move[3] - '0');
            char promotion = move.Length > 4 ? move[4] : '\0'; // Promotion piece

            // Convert FEN board to a 2D array
            char[,] board = new char[8, 8];
            int row = 0, col = 0;
            foreach (char c in boardState)
            {
                if (c == '/')
                {
                    row++;
                    col = 0;
                }
                else if (char.IsDigit(c))
                {
                    col += c - '0';
                }
                else
                {
                    board[row, col] = c;
                    col++;
                }
            }

            // Get the moving piece
            char piece = board[fromRank, fromFile];
            if (piece == '\0')
                return false; // No piece at the source square

            // Check if the piece belongs to the active player
            if ((activeColor == 'w' && char.IsLower(piece)) || (activeColor == 'b' && char.IsUpper(piece)))
                return false;

            // Check for castling
            if (char.ToLower(piece) == 'k' && Math.Abs(toFile - fromFile) == 2)
            {
                return IsCastlingValid(board, fromRank, fromFile, toFile, activeColor, castlingRights);
            }

            // Check for en passant
            if (char.ToLower(piece) == 'p' && toFile != fromFile && board[toRank, toFile] == '\0')
            {
                return IsEnPassantValid(board, fromRank, fromFile, toRank, toFile, activeColor, enPassantTarget);
            }

            // Validate piece movement rules
            if (!IsValidMove(piece, fromRank, fromFile, toRank, toFile, board, promotion))
                return false;

            // Simulate the move and check if the king is in check
            char[,] newBoard = (char[,])board.Clone();
            newBoard[toRank, toFile] = piece;
            newBoard[fromRank, fromFile] = '\0';

            // Handle en passant capture
            if (char.ToLower(piece) == 'p' && toFile != fromFile && board[toRank, toFile] == '\0')
            {
                int capturedPawnRank = activeColor == 'w' ? toRank + 1 : toRank - 1;
                newBoard[capturedPawnRank, toFile] = '\0';
            }

            if (IsKingInCheck(newBoard, activeColor))
                return false;

            return true;
        }

        /// <summary>
        /// Checks if the en passant move is valid.
        /// </summary>
        /// <param name="board"></param>
        /// <param name="fromRank"></param>
        /// <param name="fromFile"></param>
        /// <param name="toRank"></param>
        /// <param name="toFile"></param>
        /// <param name="activeColor"></param>
        /// <param name="enPassantTarget"></param>
        /// <returns></returns>
        static bool IsEnPassantValid(char[,] board, int fromRank, int fromFile, int toRank, int toFile, char activeColor, string enPassantTarget)
        {
            // Check if the move matches the en passant target square
            string targetSquare = $"{(char)('a' + toFile)}{8 - toRank}";
            if (targetSquare != enPassantTarget)
                return false;

            // Check if the capturing pawn is on the correct rank
            if ((activeColor == 'w' && fromRank != 3) || (activeColor == 'b' && fromRank != 4))
                return false;

            // Check if the destination square is empty
            if (board[toRank, toFile] != '\0')
                return false;

            // Check if the opponent's pawn is beside the capturing pawn
            int opponentPawnRank = activeColor == 'w' ? toRank + 1 : toRank - 1;
            char opponentPawn = activeColor == 'w' ? 'p' : 'P';
            if (board[opponentPawnRank, toFile] != opponentPawn)
                return false;

            return true;
        }

        /// <summary>
        /// Checks if the castling move is valid.
        /// </summary>
        /// <param name="board"></param>
        /// <param name="kingRank"></param>
        /// <param name="kingFile"></param>
        /// <param name="toFile"></param>
        /// <param name="activeColor"></param>
        /// <param name="castlingRights"></param>
        /// <returns></returns>
        static bool IsCastlingValid(char[,] board, int kingRank, int kingFile, int toFile, char activeColor, string castlingRights)
        {
            // Determine the side (kingside or queenside)
            bool isKingside = toFile > kingFile;

            // Check if castling rights are available
            char castlingSide = isKingside ? (activeColor == 'w' ? 'K' : 'k') : (activeColor == 'w' ? 'Q' : 'q');
            if (!castlingRights.Contains(castlingSide))
                return false;

            // Check if the squares between the king and rook are empty
            int start = isKingside ? kingFile + 1 : toFile + 1;
            int end = isKingside ? toFile - 1 : kingFile - 1;
            for (int file = start; file <= end; file++)
            {
                if (board[kingRank, file] != '\0')
                    return false;
            }

            // Check if the king is in check or moves through an attacked square
            for (int file = kingFile; file != toFile; file += Math.Sign(toFile - kingFile))
            {
                if (IsSquareAttacked(board, kingRank, file, activeColor))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if the square is attacked by any opponent's piece.
        /// </summary>
        /// <param name="board"></param>
        /// <param name="rank"></param>
        /// <param name="file"></param>
        /// <param name="activeColor"></param>
        /// <returns></returns>
        static bool IsSquareAttacked(char[,] board, int rank, int file, char activeColor)
        {
            // Check if any opponent's piece attacks the square
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    char piece = board[i, j];
                    if (piece != '\0' && (activeColor == 'w' ? char.IsLower(piece) : char.IsUpper(piece)))
                    {
                        if (IsValidMove(piece, i, j, rank, file, board, '\0'))
                            return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if the move is valid for the given piece.
        /// </summary>
        /// <param name="piece"></param>
        /// <param name="fromRank"></param>
        /// <param name="fromFile"></param>
        /// <param name="toRank"></param>
        /// <param name="toFile"></param>
        /// <param name="board"></param>
        /// <param name="promotion"></param>
        /// <returns></returns>
        static bool IsValidMove(char piece, int fromRank, int fromFile, int toRank, int toFile, char[,] board, char promotion)
        {
            int rankDiff = Math.Abs(toRank - fromRank);
            int fileDiff = Math.Abs(toFile - fromFile);

            switch (char.ToLower(piece))
            {
                case 'p': // Pawn
                    int direction = char.IsUpper(piece) ? -1 : 1; // White moves up, black moves down
                    if (fromFile == toFile)
                    {
                        // Normal move
                        if (toRank == fromRank + direction && board[toRank, toFile] == '\0')
                            return true;
                        // Double move from starting position
                        if ((fromRank == 1 && char.IsLower(piece)) || (fromRank == 6 && char.IsUpper(piece)))
                        {
                            if (toRank == fromRank + 2 * direction && board[toRank, toFile] == '\0' && board[fromRank + direction, toFile] == '\0')
                                return true;
                        }
                    }
                    else if (fileDiff == 1 && rankDiff == 1)
                    {
                        // Capture
                        if (board[toRank, toFile] != '\0')
                            return true;
                    }
                    // Promotion
                    if (toRank == 0 || toRank == 7)
                    {
                        if (promotion == '\0')
                            return false;
                        return "qrbn".Contains(char.ToLower(promotion));
                    }
                    return false;

                case 'n': // Knight
                    return (rankDiff == 2 && fileDiff == 1) || (rankDiff == 1 && fileDiff == 2);

                case 'b': // Bishop
                    if (rankDiff != fileDiff)
                        return false;
                    return IsPathClear(fromRank, fromFile, toRank, toFile, board);

                case 'r': // Rook
                    if (rankDiff != 0 && fileDiff != 0)
                        return false;
                    return IsPathClear(fromRank, fromFile, toRank, toFile, board);

                case 'q': // Queen
                    if (rankDiff != fileDiff && rankDiff != 0 && fileDiff != 0)
                        return false;
                    return IsPathClear(fromRank, fromFile, toRank, toFile, board);

                case 'k': // King
                    if (rankDiff <= 1 && fileDiff <= 1)
                        return true;
                    return false;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Checks if the path between two squares is clear.
        /// </summary>
        /// <param name="fromRank"></param>
        /// <param name="fromFile"></param>
        /// <param name="toRank"></param>
        /// <param name="toFile"></param>
        /// <param name="board"></param>
        /// <returns></returns>
        static bool IsPathClear(int fromRank, int fromFile, int toRank, int toFile, char[,] board)
        {
            int rankStep = Math.Sign(toRank - fromRank);
            int fileStep = Math.Sign(toFile - fromFile);
            int steps = Math.Max(Math.Abs(toRank - fromRank), Math.Abs(toFile - fromFile));

            for (int i = 1; i < steps; i++)
            {
                int rank = fromRank + i * rankStep;
                int file = fromFile + i * fileStep;
                if (board[rank, file] != '\0')
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Checks if the king is in check.
        /// </summary>
        /// <param name="board"></param>
        /// <param name="activeColor"></param>
        /// <returns></returns>
        static bool IsKingInCheck(char[,] board, char activeColor)
        {
            // Find the king's position
            int kingRank = -1, kingFile = -1;
            char king = activeColor == 'w' ? 'K' : 'k';

            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    if (board[i, j] == king)
                    {
                        kingRank = i;
                        kingFile = j;
                        break;
                    }
                }
                if (kingRank != -1)
                    break;
            }

            // Check if any opponent's piece attacks the king
            return IsSquareAttacked(board, kingRank, kingFile, activeColor);
        }
    }
}
