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
                
                ActiveTreeView.InsertOrUpdateCommentRun(nd);
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
            UpdateNagIdOnSelectedMove("+-".ToString());
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
            // for Study view
            UiBtnStPencil.Content = Constants.CHAR_PENCIL;
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
            UiBtnGmPencil.Content = Constants.CHAR_PENCIL;
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
            UiBtnExPencil.Content = Constants.CHAR_PENCIL;
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

        }
    }
}