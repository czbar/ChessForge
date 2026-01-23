using ChessForge;
using ChessPosition;
using ChessPosition.Utils;
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
        /// Parses a single PGN game
        /// </summary>
        /// <param name="pgnGametext"></param>
        /// <param name="gameTree"></param>
        public PgnGameParser(string pgnGametext, VariationTree gameTree, string fen, bool updateHeaders = true)
        {
            try
            {
                ProcessPgnGameText(gameTree, pgnGametext, fen, updateHeaders);
            }
            catch (Exception ex)
            {
                if (ex is ParserException)
                {
                    throw;
                }
                else
                {
                    LocalizedStrings.Values.TryGetValue(LocalizedStrings.StringId.PGN, out string msg);
                    throw new Exception(msg + " " + ex.Message);
                }
            }
        }

        /// <summary>
        /// This method may be invoked to process another game in the 
        /// file in which we have already processed the first game. 
        /// </summary>
        /// <param name="tree"></param>
        private void ProcessPgnGameText(VariationTree tree, string pgnGametext, string fen, bool updateHeaders = true)
        {
            _tree = tree;

            // clear Nodes, just in case
            _tree.Nodes.Clear();
            if (updateHeaders)
            {
                _tree.Header.Clear();
            }

            _runningNodeId = 0;
            _remainingGameText = ReadHeaders(pgnGametext, updateHeaders);
            ParsePgnTreeText(tree, fen);

            try
            {
                TreeUtils.SetCheckAndMates(ref _tree);
            }
            catch { }
        }

        /// <summary>
        /// Parses the PGN headers
        /// and returns the text of the game without
        /// the headers.
        /// </summary>
        /// <param name="pgnGametext"></param>
        /// <returns></returns>
        public string ReadHeaders(string pgnGametext, bool updateHeaders)
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
                        readingHeaders = ProcessHeaderLine(line, updateHeaders);
                    }

                    if (!readingHeaders)
                    {
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
        private bool ProcessHeaderLine(string line, bool processHeaders)
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
                if (processHeaders)
                {
                    ParseHeaderItem(line);
                }
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
        /// Comments start with a '{' character and end with '}'
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
                // Chess Forge requires that the rootNode's move number is 1.
                // We force it to 1 but first save the actual number from FEN to apply in the GUI when appropriate.
                if (rootNode.Position.MoveNumber >= 1)  // this should always be the case, though!
                {
                    tree.MoveNumberOffset = rootNode.Position.MoveNumber - 1;
                }
                rootNode.Position.MoveNumber = 1;
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

            if (s.StartsWith(Constants.PGN_NO_RESULT)
                || s.StartsWith(Constants.PGN_WHITE_WIN_RESULT)
                || s.StartsWith(Constants.PGN_WHITE_WIN_RESULT_EX)
                || s.StartsWith(Constants.PGN_BLACK_WIN_RESULT)
                || s.StartsWith(Constants.PGN_BLACK_WIN_RESULT_EX)
                || s.StartsWith(Constants.PGN_DRAW_SHORT_RESULT))
                return true;

            return false;
        }

        /// <summary>
        /// flags if we are reading before the first branch so that we can
        /// read correctly the Intro and also allow correct reading of the
        /// chess.com / chessbase.com
        /// </summary>
        private bool _preBranch = true;

        /// <summary>
        /// Parses a branch of the tree.
        /// </summary>
        /// <param name="parentNode"></param>
        /// <param name="tree"></param>
        private void ParseBranch(TreeNode parentNode, VariationTree tree)
        {
            string token;
            bool hasMove = false;
            string comment = "";

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
                        _preBranch = false;
                        ParseBranch(previousNode, tree);
                        break;
                    case PgnTokenType.BranchEnd:
                        return;
                    case PgnTokenType.CommentStart:
                        bool isCommentBeforeMove = !hasMove && !_preBranch;
                        comment = ProcessComment(isCommentBeforeMove ? null : parentNode);
                        if (!isCommentBeforeMove)
                        {
                            comment = null;
                        }
                        break;
                    case PgnTokenType.Move:
                        // ProcessMove() will return a new node that will then be the "parentNode"
                        // for the processing of the next move (ply)
                        TreeNode newNode = null;
                        try
                        {
                            newNode = MoveUtils.ProcessAlgMove(token, parentNode, _runningNodeId);
                        }
                        catch
                        {
                            ParserException ex = new ParserException(ParserException.ParseErrorType.PGN_INVALID_MOVE, token);
                            if (parentNode != null)
                            {
                                string previousMoveText = MoveUtils.BuildSingleMoveText(parentNode, true, true, 0);
                                ex.PreviousMove = previousMoveText;
                            }
                            throw ex;
                        }

                        // NOTE: newNode can be null if the text proved to be garbage rather than invalid move
                        // in which case the excpetion would have been thrown above.
                        // If we encountered something that looks like garbage we want to ignore it in case
                        // the PGN is corrupt but we still want to process the rest of the game.
                        if (newNode != null)
                        {
                            if (!hasMove && !string.IsNullOrEmpty(comment))
                            {
                                newNode.CommentBeforeMove = comment;
                            }
                            hasMove = true;
                            _runningNodeId++;
                            parentNode.AddChild(newNode);
                            previousNode = parentNode;
                            parentNode = newNode;
                            tree.AddNode(parentNode);
                        }
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
        /// Check that the move number is as expected.
        /// We have seen corrupt/illegal PGNs with moves missing.
        /// If the move has a wrong number do our best to find the correct parent
        /// in the already processed data.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="previousNode"></param>
        private TreeNode ProcessMoveNumber(string token, TreeNode parent)
        {
            // the token is in the format of integer followed by one or 3 dots, or no dot.
            int dotPos = token.IndexOf('.');

            string num;
            if (dotPos < 0)
            {
                num = token;
            }
            else
            {
                num = token.Substring(0, dotPos);
            }

            bool valid = uint.TryParse(num, out uint moveNo);

            if (!valid)
            {
                ParserException ex = new ParserException(ParserException.ParseErrorType.PGN_GAME_EXPECTED_MOVE_NUMBER, token);
                if (parent != null)
                {
                    string previousMoveText = MoveUtils.BuildSingleMoveText(parent, true, true, 0);
                    ex.PreviousMove = previousMoveText;
                }
                throw ex;
            }
            else
            {
                // if no dots we count it as one dot.
                int dotCount = 1;
                if (dotPos > 0)
                {
                    dotCount = token.Length - dotPos;
                }

                // don't worry about the actual number, PGNs have been known to have them wrong.
                if (parent.ColorToMove == PieceColor.White && dotCount == 1
                    || parent.ColorToMove == PieceColor.Black && dotCount >= 3)
                {
                    return null;
                }
                else
                {
                    // things seem out of order so try finding a good parent
                    TreeNode nd = FindParentForMove(parent, (int)moveNo, dotCount >= 3 ? PieceColor.Black : PieceColor.White);
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
            try
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
            catch
            {
                return null;
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
            LocalizedStrings.Values.TryGetValue(LocalizedStrings.StringId.PgnMissingMoveAfter, out string msg);
            StringBuilder sb = new StringBuilder();
            sb.Append(msg + " " + MoveUtils.BuildSingleMoveText(ndParent, true, false, _tree.MoveNumberOffset));
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
        private string ProcessComment(TreeNode node)
        {
            string comment = "";

            try
            {
                int endPos = _remainingGameText.IndexOf('}');
                // if end of comment not found, there is something wrong with the file, force end of processing.
                if (endPos < 0)
                {
                    _remainingGameText = "";
                    return "";
                }

                bool preserveCRLF = false;

                // process any Chess Forge commands
                if (node != null)
                {
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
                            if (_tree.AddChfCommand(node, command) == ChfCommands.Command.BINARY)
                            {
                                preserveCRLF = true;
                            }

                            _remainingGameText = _remainingGameText.Substring(commandEnd + 1);
                            endPos = endPos - (commandEnd + 1);
                        }
                        else
                        {
                            _remainingGameText = _remainingGameText.Substring(commandStart + 1);
                        }
                    }
                }

                // update endPos as it may have been changed above when removing commands
                endPos = _remainingGameText.IndexOf('}');

                // extract comment text
                comment = _remainingGameText.Substring(0, endPos);

                // first must check for the longer version
                if (comment.Contains(ChfCommands.CHESS_BASE_DIAGRAM_LONG))
                {
                    node.IsDiagram = true;
                    node.IsDiagramPreComment = comment.StartsWith(ChfCommands.CHESS_BASE_DIAGRAM_LONG);
                    comment = comment.Replace(ChfCommands.CHESS_BASE_DIAGRAM_LONG, "");
                }
                else if (comment.Contains(ChfCommands.CHESS_BASE_DIAGRAM))
                {
                    node.IsDiagram = true;
                    node.IsDiagramPreComment = comment.StartsWith(ChfCommands.CHESS_BASE_DIAGRAM);
                    comment = comment.Replace(ChfCommands.CHESS_BASE_DIAGRAM, "");
                }

                // check if this is a NAG disguised as comment
                string nag = GetNagMascaradingAsComment(comment);
                if (nag != null)
                {
                    AddNAGtoLastMove(_tree, nag);
                }
                else
                {
                    // trim to check if there is any comment but do not trim the comment if it is there.
                    if (comment.Trim().Length > 0)
                    {
                        if (!preserveCRLF)
                        {
                            comment = TextUtils.ReplaceCrLfInComment(comment);
                        }

                        if (node != null)
                        {
                            node.Comment = comment;
                        }
                    }
                }
                _remainingGameText = _remainingGameText.Substring(endPos + 1);
            }
            catch (Exception ex)
            {
                AppLog.Message("ProcessComment() " + comment, ex);
            }

            return comment;
        }

        /// <summary>
        /// In some case, e.g. import from lichess, NAGS are presented
        /// as comments, for example {$16}. 
        /// We will consider such entities as nags only if there
        /// is nothing beside them in the comment.
        /// </summary>
        /// <returns></returns>
        private string GetNagMascaradingAsComment(string comment)
        {
            string nag = null;
            if (comment.Length > 1 && comment[0] == '$')
            {
                if (int.TryParse(comment.Substring(1), out _))
                {
                    nag = comment;
                }
            }

            return nag;
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
                    while (_remainingGameText[charPos] == '.' && charPos < _remainingGameText.Length
                           || charPos + 2 < _remainingGameText.Length && _remainingGameText[charPos + 2] == '.')
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
            else if (char.IsLetter(c) || c == '0' || c == '-')
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

    }
}
