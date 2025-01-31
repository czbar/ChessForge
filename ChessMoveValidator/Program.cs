using System;

namespace ChessMoveValidator
{
    /// <summary>
    /// Main class of the program for testing move validation.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            // Example FEN: Position where en passant is possible
            string fen = "rnbqkbnr/ppp1pppp/8/3pP3/8/8/PPPP1PPP/RNBQKBNR w KQkq d6 0 1";
            // Example move: e5d6 (en passant capture)
            string move = "e5d6";

            bool isValid = ChessMoveValidator.ValidateChessMove(fen, move);
            Console.WriteLine(isValid ? "Valid move!" : "Invalid move.");
        }

    }
}
