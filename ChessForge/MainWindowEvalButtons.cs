using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace ChessForge
{
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Processes a request to handle a single Nag Id
        /// for the currently selected node.
        /// If the NagId is already set on the node, it will be removed.
        /// If not, it will be added or will replace the one of the same type
        /// (move or position) currently set.
        /// Also, if the nag is of type move and assessment is set, it will be cleared.
        /// </summary>
        /// <param name="nagId"></param>
        private void UpdateNagIdOnSelectedMove(string nag)
        {
            int nagId = Constants.GetNagIdFromString(nag);
            TreeNode nd = ActiveLine.GetSelectedTreeNode();

            EditOperation op = null;
            if (AppState.ActiveVariationTree != null)
            {
                op = new EditOperation(EditOperation.EditType.UPDATE_ANNOTATION, nd);
            }

            if (nd != null && nagId > 0)
            {
                int positionNag = NagUtils.GetPositionEvalNagId(nd.Nags);
                int moveNag = NagUtils.GetMoveEvalNagId(nd.Nags);

                if (NagUtils.IsPositionNag(nagId))
                {
                    if (nagId == positionNag)
                    {
                        positionNag = 0;
                    }
                    else
                    {
                        positionNag = nagId;
                    }
                }
                else if (NagUtils.IsMoveNag(nagId))
                {
                    if (nagId == moveNag)
                    {
                        moveNag = 0;
                    }
                    else
                    {
                        moveNag = nagId;
                    }
                    nd.Assessment = 0;
                }

                // put the nags back together and update
                string nags = NagUtils.BuildNagsString(moveNag, positionNag);
                nd.SetNags(nags);

                if (op != null)
                {
                    AppState.ActiveVariationTree.OpsManager.PushOperation(op);
                }

                ActiveTreeView.InsertOrUpdateCommentRun(nd);
                ActiveLine.UpdateMoveText(nd);
                
                AppState.IsDirty = true;
            }
        }

        /// <summary>
        /// The "pencil" button was clicked.  Invoke the Annotation dialog.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnPencil_Click(object sender, RoutedEventArgs e)
        {
            TreeNode nd = ActiveLine.GetSelectedTreeNode();
            if (InvokeAnnotationsDialog(nd))
            {
                ActiveTreeView.InsertOrUpdateCommentRun(nd);
            }
        }

        /// <summary>
        /// The "comment before move" button was clicked.  Invoke the Comment Before Move dialog.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnCommentBeforeMove_Click(object sender, RoutedEventArgs e)
        {
            TreeNode nd = ActiveLine.GetSelectedTreeNode();
            if (InvokeCommentBeforeMoveDialog(nd))
            {
                ActiveTreeView.InsertOrUpdateCommentBeforeMoveRun(nd);
            }
        }

        /// <summary>
        /// The "+-" button was clicked.
        /// Updated NAGs and assessment.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnEvalWin_Click(object sender, RoutedEventArgs e)
        {
            UpdateNagIdOnSelectedMove("+-");
        }

        /// <summary>
        /// The "±" button was clicked.
        /// Updated NAGs and assessment.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnEvalPlusMinus_Click(object sender, RoutedEventArgs e)
        {
            UpdateNagIdOnSelectedMove('±'.ToString());
        }

        /// <summary>
        /// The "⩲" button was clicked.
        /// Updated NAGs and assessment.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnEvalPlusEqual_Click(object sender, RoutedEventArgs e)
        {
            UpdateNagIdOnSelectedMove('⩲'.ToString());
        }

        /// <summary>
        /// The "=" button was clicked.
        /// Updated NAGs and assessment.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnEvalEqual_Click(object sender, RoutedEventArgs e)
        {
            UpdateNagIdOnSelectedMove("=");
        }

        /// <summary>
        /// The "∞" button was clicked.
        /// Updated NAGs and assessment.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnEvalUnclear_Click(object sender, RoutedEventArgs e)
        {
            UpdateNagIdOnSelectedMove('∞'.ToString());
        }

        /// <summary>
        /// The "⩱" button was clicked.
        /// Updated NAGs and assessment.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnEvalEqualPlus_Click(object sender, RoutedEventArgs e)
        {
            UpdateNagIdOnSelectedMove('⩱'.ToString());
        }

        /// <summary>
        /// The "∓" button was clicked.
        /// Updated NAGs and assessment.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnEvalMinusPlus_Click(object sender, RoutedEventArgs e)
        {
            UpdateNagIdOnSelectedMove('∓'.ToString());
        }

        /// <summary>
        /// The "+-" button was clicked.
        /// Updated NAGs and assessment.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnEvalLoss_Click(object sender, RoutedEventArgs e)
        {
            UpdateNagIdOnSelectedMove("-+".ToString());
        }

        /// <summary>
        /// The "!!" button was clicked.
        /// Updated NAGs and assessment.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnEvalExclamExclam_Click(object sender, RoutedEventArgs e)
        {
            UpdateNagIdOnSelectedMove("!!");
        }

        /// <summary>
        /// The "!" button was clicked.
        /// Updated NAGs and assessment.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnEvalExclam_Click(object sender, RoutedEventArgs e)
        {
            UpdateNagIdOnSelectedMove("!");
        }

        /// <summary>
        /// The "!?" button was clicked.
        /// Updated NAGs and assessment.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnEvalExclamQuest_Click(object sender, RoutedEventArgs e)
        {
            UpdateNagIdOnSelectedMove("!?");
        }

        /// <summary>
        /// The "?!" button was clicked.
        /// Updated NAGs and assessment.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnEvalQuestExclam_Click(object sender, RoutedEventArgs e)
        {
            UpdateNagIdOnSelectedMove("?!");
        }

        /// <summary>
        /// The "?" button was clicked.
        /// Updated NAGs and assessment.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnEvalQuest_Click(object sender, RoutedEventArgs e)
        {
            UpdateNagIdOnSelectedMove("?");
        }

        /// <summary>
        /// The "??" button was clicked.
        /// Updated NAGs and assessment.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnEvalQuestQuest_Click(object sender, RoutedEventArgs e)
        {
            UpdateNagIdOnSelectedMove("??");
        }

        /// <summary>
        /// Sets labels on the evaluation buttons
        /// </summary>
        private void SetEvaluationLabels()
        {
            string btnAnnotationsText = Constants.CHAR_SMALL_T.ToString() + Constants.CHAR_PENCIL.ToString() + Constants.CHAR_EXCLAM_QUESTION.ToString();
            string btnCommentText = Constants.CHAR_PENCIL.ToString() + Constants.CHAR_SMALL_T.ToString();

            // for Study view
            UiBtnStPencil.Content = btnAnnotationsText;
            UiBtnStCommentBeforeMove.Content = btnCommentText;
            UiBtnStEvalWin.Content = "+-";
            UiBtnStEvalPlusMinus.Content = Constants.CHAR_WHITE_ADVANTAGE;
            UiBtnStEvalPlusEqual.Content = Constants.CHAR_WHITE_EDGE;
            UiBtnStEvalEqual.Content = "=";
            UiBtnStEvalUnclear.Content = Constants.CHAR_POSITION_UNCLEAR;
            UiBtnStEvalEqualPlus.Content = Constants.CHAR_BLACK_EDGE;
            UiBtnStEvalMinusPlus.Content = Constants.CHAR_BLACK_ADVANTAGE;
            UiBtnStEvalLoss.Content = "-+";

            UiBtnStEvalExclamExclam.Content = "!!";
            UiBtnStEvalExclam.Content = "!";
            UiBtnStEvalExclamQuest.Content = "!?";
            UiBtnStEvalQuestExclam.Content = "?!";
            UiBtnStEvalQuest.Content = "?";
            UiBtnStEvalQuestQuest.Content = "??";


            // for Games view
            UiBtnGmPencil.Content = btnAnnotationsText;
            UiBtnGmCommentBeforeMove.Content = btnCommentText;
            UiBtnGmEvalWin.Content = "+-";
            UiBtnGmEvalPlusMinus.Content = Constants.CHAR_WHITE_ADVANTAGE;
            UiBtnGmEvalPlusEqual.Content = Constants.CHAR_WHITE_EDGE;
            UiBtnGmEvalEqual.Content = "=";
            UiBtnGmEvalUnclear.Content = Constants.CHAR_POSITION_UNCLEAR;
            UiBtnGmEvalEqualPlus.Content = Constants.CHAR_BLACK_EDGE;
            UiBtnGmEvalMinusPlus.Content = Constants.CHAR_BLACK_ADVANTAGE;
            UiBtnGmEvalLoss.Content = "-+";

            UiBtnGmEvalExclamExclam.Content = "!!";
            UiBtnGmEvalExclam.Content = "!";
            UiBtnGmEvalExclamQuest.Content = "!?";
            UiBtnGmEvalQuestExclam.Content = "?!";
            UiBtnGmEvalQuest.Content = "?";
            UiBtnGmEvalQuestQuest.Content = "??";


            // for Exercises view
            UiBtnExPencil.Content = btnAnnotationsText;
            UiBtnExCommentBeforeMove.Content = btnCommentText;
            UiBtnExEvalWin.Content = "+-";
            UiBtnExEvalPlusMinus.Content = Constants.CHAR_WHITE_ADVANTAGE;
            UiBtnExEvalPlusEqual.Content = Constants.CHAR_WHITE_EDGE;
            UiBtnExEvalEqual.Content = "=";
            UiBtnExEvalUnclear.Content = Constants.CHAR_POSITION_UNCLEAR;
            UiBtnExEvalEqualPlus.Content = Constants.CHAR_BLACK_EDGE;
            UiBtnExEvalMinusPlus.Content = Constants.CHAR_BLACK_ADVANTAGE;
            UiBtnExEvalLoss.Content = "-+";

            UiBtnExEvalExclamExclam.Content = "!!";
            UiBtnExEvalExclam.Content = "!";
            UiBtnExEvalExclamQuest.Content = "!?";
            UiBtnExEvalQuestExclam.Content = "?!";
            UiBtnExEvalQuest.Content = "?";
            UiBtnExEvalQuestQuest.Content = "??";

            //
            // Tooltips
            //

            // for Study view
            UiBtnStPencil.ToolTip = Properties.Resources.EditAnnotations;
            UiBtnStCommentBeforeMove.ToolTip = Properties.Resources.EditCommentBeforeMove;
            UiBtnStEvalWin.ToolTip = Properties.Resources.TooltipWhiteWinning;
            UiBtnStEvalPlusMinus.ToolTip = Properties.Resources.TooltipWhiteAdvantage;
            UiBtnStEvalPlusEqual.ToolTip = Properties.Resources.TooltipWhiteEdge;
            UiBtnStEvalEqual.ToolTip = Properties.Resources.TooltipPositionEqual;
            UiBtnStEvalUnclear.ToolTip = Properties.Resources.TooltipPositionUnclear;
            UiBtnStEvalEqualPlus.ToolTip = Properties.Resources.TooltipBlackEdge;
            UiBtnStEvalMinusPlus.ToolTip = Properties.Resources.TooltipBlackAdvantage;
            UiBtnStEvalLoss.ToolTip = Properties.Resources.TooltipBlackWinning;

            UiBtnStEvalExclamExclam.ToolTip = Properties.Resources.TooltipGreatMove;
            UiBtnStEvalExclam.ToolTip = Properties.Resources.TooltipVeryGoodMove;
            UiBtnStEvalExclamQuest.ToolTip = Properties.Resources.TooltipInterestingMove;
            UiBtnStEvalQuestExclam.ToolTip = Properties.Resources.TooltipDubiousMove;
            UiBtnStEvalQuest.ToolTip = Properties.Resources.TooltipPoorMove;
            UiBtnStEvalQuestQuest.ToolTip = Properties.Resources.TooltipBlunder;


            // for Games view
            UiBtnGmPencil.ToolTip = Properties.Resources.EditAnnotations;
            UiBtnGmCommentBeforeMove.ToolTip = Properties.Resources.EditCommentBeforeMove;
            UiBtnGmEvalWin.ToolTip = Properties.Resources.TooltipWhiteWinning;
            UiBtnGmEvalPlusMinus.ToolTip = Properties.Resources.TooltipWhiteAdvantage;
            UiBtnGmEvalPlusEqual.ToolTip = Properties.Resources.TooltipWhiteEdge;
            UiBtnGmEvalEqual.ToolTip = Properties.Resources.TooltipPositionEqual;
            UiBtnGmEvalUnclear.ToolTip = Properties.Resources.TooltipPositionUnclear;
            UiBtnGmEvalEqualPlus.ToolTip = Properties.Resources.TooltipBlackEdge;
            UiBtnGmEvalMinusPlus.ToolTip = Properties.Resources.TooltipBlackAdvantage;
            UiBtnGmEvalLoss.ToolTip = Properties.Resources.TooltipBlackWinning;

            UiBtnGmEvalExclamExclam.ToolTip = Properties.Resources.TooltipGreatMove;
            UiBtnGmEvalExclam.ToolTip = Properties.Resources.TooltipVeryGoodMove;
            UiBtnGmEvalExclamQuest.ToolTip = Properties.Resources.TooltipInterestingMove;
            UiBtnGmEvalQuestExclam.ToolTip = Properties.Resources.TooltipDubiousMove;
            UiBtnGmEvalQuest.ToolTip = Properties.Resources.TooltipPoorMove;
            UiBtnGmEvalQuestQuest.ToolTip = Properties.Resources.TooltipBlunder;


            // for Exercises view
            UiBtnExPencil.ToolTip = Properties.Resources.EditAnnotations;
            UiBtnExCommentBeforeMove.ToolTip = Properties.Resources.EditCommentBeforeMove;
            UiBtnExEvalWin.ToolTip = Properties.Resources.TooltipWhiteWinning;
            UiBtnExEvalPlusMinus.ToolTip = Properties.Resources.TooltipWhiteAdvantage;
            UiBtnExEvalPlusEqual.ToolTip = Properties.Resources.TooltipWhiteEdge;
            UiBtnExEvalEqual.ToolTip = Properties.Resources.TooltipPositionEqual;
            UiBtnExEvalUnclear.ToolTip = Properties.Resources.TooltipPositionUnclear;
            UiBtnExEvalEqualPlus.ToolTip = Properties.Resources.TooltipBlackEdge;
            UiBtnExEvalMinusPlus.ToolTip = Properties.Resources.TooltipBlackAdvantage;
            UiBtnExEvalLoss.ToolTip = Properties.Resources.TooltipBlackWinning;

            UiBtnExEvalExclamExclam.ToolTip = Properties.Resources.TooltipGreatMove;
            UiBtnExEvalExclam.ToolTip = Properties.Resources.TooltipVeryGoodMove;
            UiBtnExEvalExclamQuest.ToolTip = Properties.Resources.TooltipInterestingMove;
            UiBtnExEvalQuestExclam.ToolTip = Properties.Resources.TooltipDubiousMove;
            UiBtnExEvalQuest.ToolTip = Properties.Resources.TooltipPoorMove;
            UiBtnExEvalQuestQuest.ToolTip = Properties.Resources.TooltipBlunder;
        }
    }
}