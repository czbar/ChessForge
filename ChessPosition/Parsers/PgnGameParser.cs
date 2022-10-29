using ChessPosition;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace GameTree
{
    /// <summary>
    /// Parsers a PGN parsers
    /// allowing to build a game/variation tree
    /// </summary>
    public class PgnGameParser
    {
        // Remaining text of the file, yet to be processed
        private string _remainingGameText;

        // id of the node currently being processed
        private int _runningNodeId = 0;

        // the tree for which this parser was called
        private VariationTree _tree;

        /// <summary>
        /// Types of PGN/CHF token
        /// </summary>
        enum PgnTokenType
        {
            Unknown,
            Move,
            MoveNumber,
            CommentStart,
            CommentEnd,
            BranchStart,
            BranchEnd,
            NAG
        }

        /// <summary>
        /// Special characters
        /// </summary>
        private char[] SingleCharTokens = new char[] { '{', '}', '(', ')' };

        // whether debug information is to be logged 
        private bool DEBUG_MODE = false;

        /// <summary>
        /// The constructor takes the entire game notation as a string.
        /// </summary>
        /// <param name="tree"></param>
        public PgnGameParser(string pgnGametext, VariationTree tree, out bool multiGame, bool debugMode = false)
        {
            try
            {
                multiGame = false;

                if (debugMode)
                {
                    DEBUG_MODE = true;
                }

                ProcessPgnGameText(tree, pgnGametext);

                if (_remainingGameText.IndexOf("[White") >= 0)
                {
                    multiGame = true;
                }
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Parses a single PGN game
        /// </summary>
        /// <param name="pgnGametext"></param>
        /// <param name="gameTree"></param>
        public PgnGameParser(string pgnGametext, VariationTree gameTree, string fen = null)
        {
            try
            {
                ProcessPgnGameText(gameTree, pgnGametext, fen);
            }
            catch (Exception ex)
            {
                throw new Exception("Exception in PgnGameParser: " + ex.Message);
            }
        }

        /// <summary>
        /// This method may be invoked to process another game in the 
        /// file in which we have already processed the first game. 
        /// </summary>
        /// <param name="tree"></param>
        private void ProcessPgnGameText(VariationTree tree, string pgnGametext, string fen = null)
        {
            _tree = tree;

            // clear Nodes, just in case
            _tree.Nodes.Clear();
            _tree.Header.Clear();

            _runningNodeId = 0;
            _remainingGameText = ReadHeaders(pgnGametext);
            ParsePgnTreeText(tree, fen);
        }

        /// <summary>
        /// Parses the PGN headers
        /// and returns the text of the game without
        /// the headers.
        /// </summary>
        /// <param name="pgnGametext"></param>
        /// <returns></returns>
        public string ReadHeaders(string pgnGametext)
        {
            bool readingHeaders = true;

            string line;
            StringBuilder sb = new StringBuilder();
            using (StringReader reader = new StringReader(pgnGametext))
            {
                while ((line = reader.ReadLine()) != null)
                {
                    if (readingHeaders)
                    {
                        readingHeaders = ProcessHeaderLine(line);
                    }

                    if (!readingHeaders)
                    {
                        //                        sb.Append(line + " ");
                        sb.AppendLine(line);
                    }
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Processes the PGN Headers and returns the game text 
        /// without them.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private bool ProcessHeaderLine(string line)
        {
            line = line.Trim();
            if (line.Length == 0)
            {
                // if empty line, return true
                // as there may be a header line still
                // following
                return true;
            }

            if (line[0] == '[')
            {
                ParseHeaderItem(line);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// The header is in the form
        /// [Name "Value"]
        /// so we will strip the brackets and split by \"
        /// </summary>
        /// <param name="line"></param>
        private void ParseHeaderItem(string line)
        {
            // process only of the first and last char are square brackets
            if (line[0] == '[' && line[line.Length - 1] == ']')
            {
                line = line.Substring(1, line.Length - 2);
                string[] tokens = line.Split('\"');
                if (tokens.Length >= 2)
                {
                    _tree.Header.SetHeaderValue(tokens[0].Trim(), tokens[1].Trim());
                }
            }
        }

        /// <summary>
        /// The game text consists of White and Black moves with each White move
        /// preceded by the move number in the form of "N." where N is a positive integer.
        /// Each game must start with "1." or a comment.
        /// Comments strat with a '{' character and end with '}'
        /// A Black move follows the White move with a space character between them. If there is an intervening
        /// branch after the White move, the Black move after the return from the branch will be preceded by
        /// "N..." where N is the last White move number.
        /// Branches can be found after any move and are surrounded by parenthesis '(' and ')'.
        /// </summary>
        /// <param name="tree"></param>
        private void ParsePgnTreeText(VariationTree tree, string fen)
        {
            // create a root node
            TreeNode rootNode = new TreeNode(null, "", _runningNodeId);
            _runningNodeId++;

            if (string.IsNullOrEmpty(fen))
            {
                rootNode.Position = PositionUtils.SetupStartingPosition();
            }
            else
            {
                FenParser.ParseFenIntoBoard(fen, ref rootNode.Position);
                BackShiftOnePly(ref rootNode);
            }
            tree.AddNode(rootNode);

            if (DEBUG_MODE)
            {
                DebugUtils.PrintPosition(rootNode.Position);
            }

            //TreeNode
            ParseBranch(rootNode, tree);
        }

        /// <summary>
        /// Shifts the Move Number down by one.
        /// This is required when we adjust the position from the passed FEN.
        /// </summary>
        /// <param name="nd"></param>
        private void BackShiftOnePly(ref TreeNode nd)
        {
            if (nd.ColorToMove == PieceColor.White)
            {
                nd.MoveNumber -= 1;
            }
        }

        /// <summary>
        /// Checks if the passed string matches any of the
        /// strings that mark the end of the game/tree text.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private bool IsGameTerminationMarker(string s)
        {
            if (string.IsNullOrEmpty(s))
                return true;

            if (s == Constants.PGN_NO_RESULT
                || s == Constants.PGN_WHITE_WIN_RESULT
                || s == Constants.PGN_BLACK_WIN_RESULT
                || s.StartsWith(Constants.PGN_DRAW_SHORT_RESULT))
                return true;

            return false;
        }

        /// <summary>
        /// Parses a branch of the tree.
        /// </summary>
        /// <param name="parentNode"></param>
        /// <param name="tree"></param>
        private void ParseBranch(TreeNode parentNode, VariationTree tree)
        {
            string token;

            TreeNode previousNode = parentNode;

            while ((token = GetNextToken()) != "")
            {
                if (IsGameTerminationMarker(token))
                    break;

                PgnTokenType gtt = GetTokenType(token);

                switch (gtt)
                {
                    // if this is a new branch then invoke this method again
                    case PgnTokenType.BranchStart:
                        ParseBranch(previousNode, tree);
                        break;
                    case PgnTokenType.BranchEnd:
                        return;
                    case PgnTokenType.CommentStart:
                        ProcessComment(parentNode);
                        break;
                    case PgnTokenType.Move:
                        // ProcessMove() will return a new node
                        // that will then be the "parentNode" for
                        // the processing of the next move (ply)
                        TreeNode newNode = ProcessMove(token, parentNode);
                        parentNode.AddChild(newNode);
                        previousNode = parentNode;
                        parentNode = newNode;
                        tree.AddNode(parentNode);
                        //if (DEBUG_MODE)
                        //{
                        //    DebugUtils.PrintPosition(newNode.Position);
                        //}

                        break;
                    case PgnTokenType.MoveNumber:
                        TreeNode adjustedParent = ProcessMoveNumber(token, parentNode);
                        // did we have to adjust the parent
                        if (adjustedParent != null)
                        {
                            previousNode = adjustedParent;
                            parentNode = adjustedParent;
                        }
                        break;
                    case PgnTokenType.NAG:
                        // add to the last processed move
                        AddNAGtoLastMove(tree, token);
                        break;
                }
            }
        }

        /// <summary>
        /// Adds an encountered NAG character to the last processed move.
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="nag"></param>
        private void AddNAGtoLastMove(VariationTree tree, string nag)
        {
            TreeNode nd = tree.Nodes[tree.Nodes.Count - 1];
            nd.AddNag(nag);
        }

        /// <summary>
        /// This is the core processing method.
        /// It creates a new node in the game tree, parses the
        /// move text and creates a position in the new node
        /// by making the parsed move on the board of the parent position.
        /// </summary>
        /// <param name="algMove"></param>
        /// <param name="charIndex"></param>
        /// <param name="previousNode"></param>
        /// <returns></returns>
        private TreeNode ProcessMove(string algMove, TreeNode parentNode)
        {
            PieceColor parentSideToMove = parentNode.ColorToMove;

            // check for bad PGN with '0-0' instead of 'O-O' castling
            if (algMove.Length > 1 && algMove[0] == '0')
            {
                algMove = algMove.Replace('0', 'O');
            }

            PgnMoveParser pmp = new PgnMoveParser();
            int suffixLen = pmp.ParseAlgebraic(algMove, parentSideToMove);
            // remove suffix from algMove
            algMove = algMove.Substring(0, algMove.Length - suffixLen);
            MoveData move = pmp.Move;
            algMove = StripCheckOrMateChar(algMove);
            if (DEBUG_MODE)
            {
                DebugUtils.PrintMove(move, algMove);
            }

            // create a new node
            TreeNode newNode = CreateNewNode(algMove, move, parentNode, parentSideToMove);
            if (move.IsCheckmate)
            {
                newNode.Position.IsCheckmate = true;
            }
            else if (move.IsCheck)
            {
                newNode.Position.IsCheck = true;
            }

            try
            {
                // Make the move on it
                MoveUtils.MakeMove(newNode.Position, move);
            }
            catch
            {
                //MessageBox.Show("Failed to parse move " + newNode.MoveNumber.ToString() + ". " + algMove + " : " + ex.Message, 
                //    "PGN Parsing Error", MessageBoxButton.OK, MessageBoxImage.Error);
                throw new Exception(BuildErrortext(newNode, algMove));
            }

            // do the postprocessing
            PositionUtils.UpdateCastlingRights(ref newNode.Position, move, false);
            PositionUtils.SetEnpassantSquare(ref newNode.Position, move);

            return newNode;
        }

        /// <summary>
        /// Builds text for a message reporting error in processing a move.
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="algMove"></param>
        /// <returns></returns>
        private string BuildErrortext(TreeNode nd, string algMove)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Move " + nd.MoveNumber.ToString());
            if (nd.ColorToMove == PieceColor.White)
            {
                sb.Append("...");
            }
            else
            {
                sb.Append(".");
            }
            sb.Append(algMove);
            return sb.ToString();
        }

        /// <summary>
        /// Creates a new node in the game tree for the next move
        /// to be stored with the position.
        /// </summary>
        /// <param name="algMove"></param>
        /// <param name="move"></param>
        /// <param name="parentNode"></param>
        /// <param name="parentSideToMove"></param>
        /// <returns></returns>
        private TreeNode CreateNewNode(string algMove, MoveData move, TreeNode parentNode, PieceColor parentSideToMove)
        {
            TreeNode newNode = new TreeNode(parentNode, algMove, _runningNodeId);
            _runningNodeId++;

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
        /// Check that the move number is as expected.
        /// We have seen corrupt/illegal PGNs with moves missing.
        /// If the move has a wrong number do our best to find the correct parent
        /// in the already processed data.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="previousNode"></param>
        private TreeNode ProcessMoveNumber(string token, TreeNode parent)
        {
            // the token is in the format of integer followed by one or 3 dots
            int dotPos = token.IndexOf('.');
            string num = token.Substring(0, dotPos);
            uint moveNo = uint.Parse(num);
            int dotCount = token.Length - dotPos;

            if (parent.ColorToMove == PieceColor.White && dotCount == 1 && moveNo == parent.MoveNumber + 1
                || parent.ColorToMove == PieceColor.Black && dotCount == 3 && moveNo == parent.MoveNumber)
            {
                return null;
            }
            else
            {
                // bad wrong number, see we can find the right parent
                TreeNode nd = FindParentForMove(parent, (int)moveNo, dotCount == 3 ? PieceColor.Black : PieceColor.White);
                if (nd != null)
                {
                    return nd;
                }
                else
                {
                    throw new Exception(BuildMissingMoveErrorText(parent));
                }
            }
        }

        /// <summary>
        /// Attempts to find the right parent based on the coming move number
        /// and color as well as "presumed" parent which proved to be not from an 
        /// immediately preceding move.
        /// </summary>
        /// <param name="presumedParent"></param>
        /// <param name="moveNo"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        private TreeNode FindParentForMove(TreeNode presumedParent, int moveNo, PieceColor color)
        {
            int moveDistance = CalculateMoveDistance(presumedParent, moveNo, color);
            if (moveDistance == 2)
            {
                return presumedParent.Children[0];
            }
            else
            {
                TreeNode nd = presumedParent;
                if (moveDistance < 2)
                {
                    for (int i = 1; i > moveDistance; i--)
                    {
                        if (nd != null)
                        {
                            nd = nd.Parent;
                        }
                    }
                }
                return nd;
            }
        }

        /// <summary>
        /// Calculates the distance, in half moves, between a Node and a move
        /// identified by its number and color.
        /// 
        /// This distance should normally be 1, but in bad PGN it may be 0, 2 
        /// or something else e.g.:
        /// -1 : 7... Be7 7.f4
        /// 0  : 7... Be7 7...Nf6
        /// 1  : 7... Be7 8.Bg5 or 7...Be7 8.Bg5 ( 8.Bc4
        /// 2  : 7... Be7 ( 8.Bg5
        /// </summary>
        /// <param name="fromMoveNode"></param>
        /// <param name="toMoveNumber"></param>
        /// <param name="toMoveColor"></param>
        /// <returns></returns>
        private int CalculateMoveDistance(TreeNode fromMoveNode, int toMoveNumber, PieceColor toMoveColor)
        {
            int moveDiff = (int)(toMoveNumber - fromMoveNode.MoveNumber) * 2;
            int colorDiff = 0;
            if (toMoveColor == PieceColor.White && fromMoveNode.ColorToMove == PieceColor.White)
            {
                colorDiff = -1;
            }
            else if (toMoveColor == PieceColor.Black && fromMoveNode.ColorToMove == PieceColor.Black)
            {
                colorDiff = 1;
            }

            return moveDiff + colorDiff;
        }


        /// <summary>
        /// Builds text for a message reporting a "missing move" error.
        /// </summary>
        /// <param name="ndParent"></param>
        /// <returns></returns>
        private string BuildMissingMoveErrorText(TreeNode ndParent)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Missing move after " + MoveUtils.BuildSingleMoveText(ndParent, true));
            return sb.ToString();
        }

        /// <summary>
        /// If the comment inludes Chess Forge commands, the commands
        /// will be stored with the node.
        /// Chess Forge commands are in the form [%chf-<cmd> <params>] and must be at the start
        /// of the comment, with any (optional) free text comment following.
        /// The comment will also be stored with the node, stripped of the Chess Forge commands.
        /// </summary>
        /// <param name="node"></param>
        private void ProcessComment(TreeNode node)
        {
            int endPos = _remainingGameText.IndexOf('}');
            // if end of comment not found, there is something wrong with the file, force end of processing.
            if (endPos < 0)
            {
                _remainingGameText = "";
                return;
            }

            // process any Chess Forge commands
            while (true)
            {
                int commandStart = _remainingGameText.IndexOf("[%", 0, endPos);
                if (commandStart < 0)
                    break;

                int commandEnd = _remainingGameText.IndexOf(']', 0, endPos);
                if (commandEnd > 0)
                {
                    string command = _remainingGameText.Substring(commandStart + 1, commandEnd - (commandStart + 1));
                    if (command.Trim().Length > 0)
                    {
                        // remove CRLF
                        command = command.Replace("\r", "");
                        command = command.Replace("\n", "");
                    }
                    _tree.AddChfCommand(node, command);
                    _remainingGameText = _remainingGameText.Substring(commandEnd + 1);
                    endPos = endPos - (commandEnd + 1);
                }
            }

            // update endPos as it may have been changed above when removing commands
            endPos = _remainingGameText.IndexOf('}');

            string comment = _remainingGameText.Substring(0, endPos);
            // trim to check if there is any comment but do not trim the comment if it is there.
            if (comment.Trim().Length > 0)
            {
                // remove CRLF
                comment = comment.Replace("\r", "");
                comment = comment.Replace("\n", "");

                node.Comment = comment;
            }
            _remainingGameText = _remainingGameText.Substring(endPos + 1);
        }

        /// <summary>
        /// Find the next token in the remaining text.
        /// </summary>
        /// <returns></returns>
        private string GetNextToken()
        {
            if (_remainingGameText.Length == 0)
            {
                return Constants.PGN_NO_RESULT; // this is unexpected, return game termination token to prevent crash
            }

            int charPos = 0;
            string token = "";

            // first skip the spaces
            while (_remainingGameText[charPos] == ' ' || _remainingGameText[charPos] == '\r' || _remainingGameText[charPos] == '\n')
            {
                charPos++;
                // if there is no end-of-game char we will get a bad index
                if (charPos >= _remainingGameText.Length)
                {
                    return "";
                }
            }

            int tokenStartIndex = charPos;

            char c = _remainingGameText[charPos];

            if (SingleCharTokens.Contains(c))
            {
                charPos++;
                token += c;
            }
            else
            {
                // go to the next space or closing parenthesis or a dot
                while (charPos < _remainingGameText.Length
                    && _remainingGameText[charPos] != ' '
                    && _remainingGameText[charPos] != ')'
                    && _remainingGameText[charPos] != '.'
                    && _remainingGameText[charPos] != '\r'
                    && _remainingGameText[charPos] != '\n'
                    )
                {
                    charPos++;
                }
                // if the last was a dot, check if there are more dots
                if (charPos < _remainingGameText.Length && _remainingGameText[charPos] == '.')
                {
                    while (_remainingGameText[charPos] == '.' && charPos < _remainingGameText.Length)
                    {
                        charPos++;
                    }
                }

                token = _remainingGameText.Substring(tokenStartIndex, charPos - tokenStartIndex);
            }


            _remainingGameText = _remainingGameText.Substring(charPos);
            return token;
        }

        /// <summary>
        /// Returns the type of the passed token.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private PgnTokenType GetTokenType(string token)
        {
            if (token.Length == 0)
            {
                return PgnTokenType.Unknown;
            }

            PgnTokenType gtt = PgnTokenType.Unknown;

            char c = token[0];
            if (char.IsDigit(c) && c != '0')  // guard against bad PGN with '0-0' instead of 'O-O' castling
            {
                gtt = PgnTokenType.MoveNumber;
            }
            else if (char.IsLetter(c) || c == '0')
            {
                gtt = PgnTokenType.Move;
            }
            else
            {
                switch (c)
                {
                    case '(':
                        gtt = PgnTokenType.BranchStart;
                        break;
                    case ')':
                        gtt = PgnTokenType.BranchEnd;
                        break;
                    case '{':
                        gtt = PgnTokenType.CommentStart;
                        break;
                    case '}':
                        gtt = PgnTokenType.CommentEnd;
                        break;
                    case '$':
                        gtt = PgnTokenType.NAG;
                        break;
                }
            }

            return gtt;
        }

        /// <summary>
        /// Removes the last character of the algebraic notation if it
        /// denotes a check or a mate.
        /// This is so that these symbols are not duplicated when they get added
        /// later on.
        /// </summary>
        /// <param name="algMove"></param>
        /// <returns></returns>
        private string StripCheckOrMateChar(string algMove)
        {

            if (algMove[algMove.Length - 1] == '#' || algMove[algMove.Length - 1] == '+')
            {
                return algMove.Substring(0, algMove.Length - 1);
            }

            return algMove;
        }

    }
}
