using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ChessForge
{
    /// <summary>
    /// Handles moves that were made on the board by the user.
    /// </summary>
    public class UserMoveProcessor
    {
        /// <summary>
        /// Invoked after the user made a move on the chessboard
        /// and released the mouse.
        /// </summary>
        /// <param name="destSquare"></param>
        public static void FinalizeUserMove(SquareCoords destSquare)
        {
            // if the move is valid swap image at destination and clear image at origin
            if (destSquare.Xcoord != DraggedPiece.Square.Xcoord || destSquare.Ycoord != DraggedPiece.Square.Ycoord)
            {
                StringBuilder moveEngCode = new StringBuilder();
                SquareCoords origSquareNorm = new SquareCoords(DraggedPiece.Square);
                SquareCoords destSquareNorm = new SquareCoords(destSquare);
                if (AppStateManager.MainWin.MainChessBoard.IsFlipped)
                {
                    origSquareNorm.Flip();
                    destSquareNorm.Flip();
                }

                bool isPromotion = false;
                PieceType promoteTo = PieceType.None;

                if (IsMoveToPromotionSquare(origSquareNorm, destSquareNorm))
                {
                    isPromotion = true;
                    promoteTo = AppStateManager.MainWin.GetUserPromoSelection(destSquareNorm);
                }

                // do not process if this was a canceled promotion
                if ((!isPromotion || promoteTo != PieceType.None))  // && AppStateManager.MainWin.MainChessBoard.GetPieceColor(destSquareNorm) != EngineGame.ColorToMove)
                {
                    moveEngCode.Append((char)(origSquareNorm.Xcoord + (int)'a'));
                    moveEngCode.Append((char)(origSquareNorm.Ycoord + (int)'1'));
                    moveEngCode.Append((char)(destSquareNorm.Xcoord + (int)'a'));
                    moveEngCode.Append((char)(destSquareNorm.Ycoord + (int)'1'));

                    // add promotion char if this is a promotion
                    if (isPromotion)
                    {
                        moveEngCode.Append(FenParser.PieceToFenChar[promoteTo]);
                    }
                    bool isCastle;
                    TreeNode nd;
                    if (ProcessMove(moveEngCode.ToString(), out nd, out isCastle))
                    {
                        // check promotion for the side who moved i.e. opposite of what we have in the new nd Node
                        ImageSource imgSrc = DraggedPiece.ImageControl.Source;
                        if (isPromotion)
                        {
                            if (nd.ColorToMove == PieceColor.Black)
                            {
                                imgSrc = AppStateManager.MainWin.MainChessBoard.GetWhitePieceRegImg(promoteTo);
                            }
                            else
                            {
                                imgSrc = AppStateManager.MainWin.MainChessBoard.GetBlackPieceRegImg(promoteTo);
                            }
                        }
                        AppStateManager.MainWin.MainChessBoard.GetPieceImage(destSquare.Xcoord, destSquare.Ycoord, true).Source = imgSrc;

                        AppStateManager.MainWin.ReturnDraggedPiece(true);
                        if (isCastle)
                        {
                            AppStateManager.MainWin.MoveCastlingRook(moveEngCode.ToString());
                        }

                        SoundPlayer.PlayMoveSound(nd.LastMoveAlgebraicNotation);

                        if (AppStateManager.CurrentLearningMode == LearningMode.Mode.ENGINE_GAME)
                        {
                            AppStateManager.ShowMoveEvaluationControls(false, false);
                        }

                        if (nd.Position.IsCheckmate)
                        {
                            AppStateManager.MainWin.BoardCommentBox.ReportCheckmate(true);
                        }
                        else if (nd.Position.IsStalemate)
                        {
                            AppStateManager.MainWin.BoardCommentBox.ReportStalemate();
                        }
                        else
                        {
                            AppStateManager.MainWin.BoardCommentBox.GameMoveMade(nd, true);
                        }
                        AppStateManager.MainWin.ColorMoveSquares(nd.LastMoveEngineNotation);
                        if (nd != null)
                        {
                            AppStateManager.MainWin.MainChessBoard.DisplayPosition(nd);
                        }
                    }
                    else
                    {
                        AppStateManager.MainWin.ReturnDraggedPiece(false);
                    }
                }
                else
                {
                    AppStateManager.MainWin.ReturnDraggedPiece(false);
                }
            }
            else
            {
                AppStateManager.MainWin.ReturnDraggedPiece(false);
            }
        }

        /// <summary>
        /// Processed the move's business logic.
        /// </summary>
        /// <param name="move"></param>
        /// <param name="nd"></param>
        /// <param name="isCastle"></param>
        /// <returns></returns>
        public static bool ProcessMove(string move, out TreeNode nd, out bool isCastle)
        {
            if (AppStateManager.CurrentLearningMode == LearningMode.Mode.MANUAL_REVIEW)
            {
                return ProcessMoveInManualReviewMode(move, out nd, out isCastle);
            }
            else
            {
                return ProcessMoveInGameMode(move, out nd, out isCastle);
            }
        }

        /// <summary>
        /// Checks if the move leads to a promotion
        /// i.e. a pawn reaching the last rank.
        /// </summary>
        /// <param name="origSquareNorm"></param>
        /// <param name="destSquareNorm"></param>
        /// <returns></returns>
        private static bool IsMoveToPromotionSquare(SquareCoords origSquareNorm, SquareCoords destSquareNorm)
        {
            if (AppStateManager.CurrentLearningMode == LearningMode.Mode.MANUAL_REVIEW)
            {
                TreeNode nd = AppStateManager.MainWin.ActiveLine.GetSelectedTreeNode();
                PieceColor pieceColor = AppStateManager.MainWin.MainChessBoard.GetPieceColor(origSquareNorm);
                if (AppStateManager.MainWin.MainChessBoard.GetPieceType(origSquareNorm) == PieceType.Pawn
                 && ((nd.ColorToMove == PieceColor.White && destSquareNorm.Ycoord == 7)
                     || (nd.ColorToMove == PieceColor.Black && destSquareNorm.Ycoord == 0)))
                {
                    return true;
                }
            }
            else if (EngineGame.GetPieceType(origSquareNorm) == PieceType.Pawn
                && ((EngineGame.ColorToMove == PieceColor.White && destSquareNorm.Ycoord == 7)
                    || (EngineGame.ColorToMove == PieceColor.Black && destSquareNorm.Ycoord == 0)))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Processes a user's move made in the context of a game
        /// with the engine.
        /// TODO: do we need a lock here so ProcessUserGameMoveEvent does not start before
        /// we finish this?
        /// </summary>
        /// <param name="move"></param>
        /// <param name="nd"></param>
        /// <param name="isCastle"></param>
        /// <returns></returns>
        private static bool ProcessMoveInGameMode(string move, out TreeNode nd, out bool isCastle)
        {
            if (CreateNewPlyNode(move, out nd, out isCastle, out bool preExist))
            {
                bool endOfGame = false;
                if (PositionUtils.IsCheckmate(nd.Position))
                {
                    endOfGame = true;
                    AppStateManager.MainWin.BoardCommentBox.ReportCheckmate(true);
                }
                else if (PositionUtils.IsStalemate(nd.Position))
                {
                    endOfGame = true;
                    AppStateManager.MainWin.BoardCommentBox.ReportStalemate();
                }

                EngineGame.Line.AddPlyAndMove(nd);
                EngineGame.SwitchToAwaitEngineMove(nd, endOfGame);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Processes a move made by the user while
        /// in Manual Review mode.
        /// </summary>
        /// <param name="move"></param>
        /// <param name="nd"></param>
        /// <param name="isCastle"></param>
        /// <returns></returns>
        private static bool ProcessMoveInManualReviewMode(string move, out TreeNode nd, out bool isCastle)
        {
            if (CreateNewPlyNode(move, out nd, out isCastle, out bool preExist))
            {
                if (PositionUtils.IsCheckmate(nd.Position))
                {
                    nd.Position.IsCheckmate = true;
                }
                else if (PositionUtils.IsStalemate(nd.Position))
                {
                    nd.Position.IsStalemate = true;
                }

                //TODO: update Workbook, ActiveLine and Workbook View
                // if the move already has a LineId, it means we found it to exist so just select the new line
                // and the move in the views

                // if the move is new but has no siblings, "inherit" line id from the parent 
                if (!preExist && string.IsNullOrEmpty(nd.LineId) && !AppStateManager.MainWin.ActiveVariationTree.NodeHasSiblings(nd.NodeId))
                {
                    nd.LineId = nd.Parent.LineId;
                }

                // if we have LineId we are done
                if (!string.IsNullOrEmpty(nd.LineId))
                {
                    if (nd.IsNewUserMove && !preExist)
                    {
                        AppStateManager.MainWin.AppendNodeToActiveLine(nd, false);
                        // in exercise this can be the first move (nd.Parent.NodeId == 0) in which case we want to call a Rebuild so we get the move number
                        if (nd.Parent == null || nd.Parent.NodeId == 0 || AppStateManager.MainWin.ActiveVariationTree.NodeHasSiblings(nd.Parent.NodeId))
                        {
                            AppStateManager.MainWin.RebuildActiveTreeView();
                        }
                        else
                        {
                            AppStateManager.MainWin.AddNewNodeToVariationTreeView(nd);
                        }
                        AppStateManager.MainWin.SelectLineAndMoveInWorkbookViews(AppStateManager.MainWin.ActiveTreeView, AppStateManager.MainWin.ActiveLine.GetLineId(), AppStateManager.MainWin.ActiveLine.GetSelectedPlyNodeIndex(false));
                    }
                    else
                    {
                        AppStateManager.MainWin.SetActiveLine(nd.LineId, nd.NodeId, false);
                        AppStateManager.MainWin.SelectLineAndMoveInWorkbookViews(AppStateManager.MainWin.ActiveTreeView, AppStateManager.MainWin.ActiveLine.GetLineId(), AppStateManager.MainWin.ActiveLine.GetSelectedPlyNodeIndex(false));
                    }
                }
                else
                {
                    // new move for which we need a new line id
                    // if it is new and has siblings, rebuild line ids
                    // Workbook view will need a full update unless TODO this node AND its parent have no siblings
                    AppStateManager.MainWin.ActiveVariationTree.SetLineIdForNewNode(nd);
                    AppStateManager.MainWin.SetActiveLine(nd.LineId, nd.NodeId, false);
                    AppStateManager.MainWin.RebuildActiveTreeView();
                    AppStateManager.MainWin.SelectLineAndMoveInWorkbookViews(AppStateManager.MainWin.ActiveTreeView, AppStateManager.MainWin.ActiveLine.GetLineId(), AppStateManager.MainWin.ActiveLine.GetSelectedPlyNodeIndex(false));
                }

                if (SolvingManager.GetAppSolvingMode() == VariationTree.SolvingMode.GUESS_MOVE)
                {
                    AppStateManager.MainWin.Timers.Start(AppTimers.TimerId.SOLVING_GUESS_MOVE_MADE);
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Process a user's move supplied in engine notation.
        /// </summary>
        /// <param name="move"></param>
        /// <param name="nd"></param>
        /// <param name="isCastle"></param>
        /// <returns></returns>
        private static bool CreateNewPlyNode(string move, out TreeNode nd, out bool isCastle, out bool preExist)
        {
            isCastle = false;
            preExist = false;

            TreeNode curr;
            if (AppStateManager.CurrentLearningMode == LearningMode.Mode.MANUAL_REVIEW)
            {
                curr = AppStateManager.MainWin.ActiveLine.GetSelectedTreeNode();
            }
            else
            {
                curr = EngineGame.GetLastGameNode();
            }

            nd = AppStateManager.MainWin.ActiveVariationTree.CreateNewChildNode(curr);
            string algMove;
            try
            {
                algMove = MoveUtils.EngineNotationToAlgebraic(move, ref nd.Position, out isCastle);
            }
            catch
            {
                algMove = "";
            }

            // check that it starts with a letter as it may be something invalid like "???"
            if (!string.IsNullOrEmpty(algMove) && char.IsLetter(algMove[0]))
            {
                nd.Position.ColorToMove = nd.Position.ColorToMove == PieceColor.White ? PieceColor.Black : PieceColor.White;
                nd.MoveNumber = nd.Position.ColorToMove == PieceColor.White ? nd.MoveNumber : nd.MoveNumber += 1;
                nd.LastMoveAlgebraicNotation = algMove;

                if (MoveUtils.IsCaptureOrPawnMove(algMove))
                {
                    nd.Position.HalfMove50Clock = 0;
                }
                else
                {
                    nd.Position.HalfMove50Clock += 1;
                }

                TreeNode sib = AppStateManager.MainWin.ActiveVariationTree.GetIdenticalSibling(nd);
                if (sib == null)
                {
                    // if this is a new move, mark as such and add to Workbook
                    if (AppStateManager.CurrentLearningMode == LearningMode.Mode.MANUAL_REVIEW)
                    {
                        nd.IsNewUserMove = true;
                        AppStateManager.IsDirty = true;
                    }
                    else
                    {
                        nd.IsNewTrainingMove = true;
                    }
                    AppStateManager.MainWin.ActiveVariationTree.AddNodeToParent(nd);
                }
                else
                {
#if false
                    // nd has en passant processed already
                    byte enPassant = nd.Position.EnPassantSquare;
                    byte inheritedEnPassant = nd.Position.InheritedEnPassantSquare;
                    nd = sib;
                    nd.Position.EnPassantSquare = enPassant;
                    nd.Position.InheritedEnPassantSquare = inheritedEnPassant;
#endif
                    preExist = true;
                    nd = sib;
                }
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
