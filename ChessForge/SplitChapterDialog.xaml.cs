using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace ChessForge
{
    /// <summary>
    /// Field by which to split the chapter.
    /// </summary>
    public enum SplitBy
    {
        ECO,
        DATE,
        ROUND
    }

    /// <summary>
    /// Granularity criterion for splitting.
    /// </summary>
    public enum SplitByCriterion
    {
        ECO_AE,
        ECO_A0E9,
        ECO_A00E99,

        DATE_YEAR,
        DATE_MONTH,
        DATE_DAY
    }

    /// <summary>
    /// Interaction logic for SplitChapterDialog.xaml
    /// </summary>
    public partial class SplitChapterDialog : Window
    {
        /// <summary>
        /// Whether the Move to Chapters per ECO
        /// option was selected.
        /// </summary>
        public bool MoveToChaptersPerECO;

        /// <summary>
        /// Last used split by field
        /// </summary>
        public static SplitBy LastSplitBy = SplitBy.ECO;

        /// <summary>
        /// Last used split by criterion
        /// </summary>
        public static SplitByCriterion LastSplitByCrtierion = SplitByCriterion.ECO_AE;

        /// <summary>
        /// Sets default selection and visibility
        /// </summary>
        public SplitChapterDialog(Chapter chapter)
        {
            InitializeComponent();

            UiLabelChapterTitle.Content = Properties.Resources.Chapter + ": " + chapter.GetTitle(); 
            SetSplitBySelection(LastSplitBy);
            SetDefaultCriterionButton(LastSplitBy);
            SetActiveCriterionButton(LastSplitBy);

            if (AppState.Workbook.Chapters.Count <= 1)
            {
                UiCbDistribByEco.IsEnabled = false;
            }
        }

        /// <summary>
        /// Set SplitBy radio button selection.
        /// Set Granularity GroupBox's visibility.
        /// </summary>
        /// <param name="splitBy"></param>
        private void SetSplitBySelection(SplitBy splitBy)
        {
            switch (splitBy)
            {
                case SplitBy.ECO:
                    UiRbSplitByEco.IsChecked = true;
                    UiGbEcoCrit.Visibility = Visibility.Visible;
                    UiGbDateCrit.Visibility = Visibility.Collapsed;
                    UiGbRoundCrit.Visibility = Visibility.Collapsed;
                    break;
                case SplitBy.DATE:
                    UiRbSplitByDate.IsChecked = true;
                    UiGbEcoCrit.Visibility = Visibility.Collapsed;
                    UiGbDateCrit.Visibility = Visibility.Visible;
                    UiGbRoundCrit.Visibility = Visibility.Collapsed;
                    break;
                case SplitBy.ROUND:
                    UiRbSplitByRound.IsChecked = true;
                    UiGbEcoCrit.Visibility = Visibility.Collapsed;
                    UiGbDateCrit.Visibility = Visibility.Collapsed;
                    UiGbRoundCrit.Visibility = Visibility.Visible;
                    break;
            }
        }

        /// <summary>
        /// Retrieves the current SplitBy selection.
        /// </summary>
        /// <returns></returns>
        private SplitBy CurrentSplitBy()
        {
            SplitBy splitBy;

            if (UiRbSplitByEco.IsChecked == true)
            {
                splitBy = SplitBy.ECO;
            }
            else if (UiRbSplitByDate.IsChecked == true)
            {
                splitBy = SplitBy.DATE;
            }
            else
            {
                splitBy = SplitBy.ROUND;
            }

            return splitBy;
        }

        /// <summary>
        /// Show or hide Split by and Split Criteria group boxes
        /// in response to the changing state of the Move Games check box
        /// </summary>
        /// <param name="show"></param>
        private void ShowSplitByGroups(bool show)
        {
            if (show)
            {
                SetSplitBySelection(CurrentSplitBy());
                UiGbSplitBy.Foreground = Brushes.Black;
                UiRbSplitByDate.Visibility = Visibility.Visible;
                UiRbSplitByEco.Visibility = Visibility.Visible;
                UiRbSplitByRound.Visibility = Visibility.Visible;
            }
            else
            {
                UiGbEcoCrit.Visibility = Visibility.Hidden;
                UiGbDateCrit.Visibility = Visibility.Hidden;
                UiGbRoundCrit.Visibility = Visibility.Visible;

                UiGbSplitBy.Foreground = Brushes.LightGray;
                UiRbSplitByDate.Visibility = Visibility.Hidden;
                UiRbSplitByEco.Visibility = Visibility.Hidden;
                UiRbSplitByRound.Visibility = Visibility.Hidden;
            }
        }

        /// <summary>
        /// Set the radio button selection for the selected SplitBy criterion. 
        /// </summary>
        /// <param name="splitBy"></param>
        private void SetActiveCriterionButton(SplitBy splitBy)
        {
            switch (LastSplitByCrtierion)
            {
                case SplitByCriterion.ECO_AE:
                    UiRbCritAtoE.IsChecked = true;
                    break;
                case SplitByCriterion.ECO_A0E9:
                    UiRbCritA0toE9.IsChecked = true;
                    break;
                case SplitByCriterion.ECO_A00E99:
                    UiRbCritA00toE99.IsChecked = true;
                    break;

                case SplitByCriterion.DATE_YEAR:
                    UiRbCritYear.IsChecked = true;
                    break;
                case SplitByCriterion.DATE_MONTH:
                    UiRbCritMonth.IsChecked = true;
                    break;
                case SplitByCriterion.DATE_DAY:
                    UiRbCritDay.IsChecked = true;
                    break;
            }
        }

        /// <summary>
        /// Set the radio button selection for the non-nonselected current SplitBy buttons. 
        /// </summary>
        /// <param name="splitBy"></param>
        private void SetDefaultCriterionButton(SplitBy splitBy)
        {
            switch (splitBy)
            {
                case SplitBy.ECO:
                    UiRbCritYear.IsChecked = true;
                    break;
                case SplitBy.DATE:
                    UiRbCritAtoE.IsChecked = true;
                    break;
                case SplitBy.ROUND:
                    UiRbCritYear.IsChecked = true;
                    UiRbCritAtoE.IsChecked = true;
                    break;
            }
        }

        /// <summary>
        /// The ECO split by button was clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiRbSplitByEco_Checked(object sender, RoutedEventArgs e)
        {
            SetSplitBySelection(SplitBy.ECO);
        }

        /// <summary>
        /// The Date split by button was clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiRbSplitByDate_Checked(object sender, RoutedEventArgs e)
        {
            SetSplitBySelection(SplitBy.DATE);
        }

        /// <summary>
        /// The Round split by button was clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiRbSplitByRound_Checked(object sender, RoutedEventArgs e)
        {
            SetSplitBySelection(SplitBy.ROUND);
        }

        /// <summary>
        /// Set the static constants and exit.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnOk_Click(object sender, RoutedEventArgs e)
        {
            MoveToChaptersPerECO = UiCbDistribByEco.IsChecked == true;

            if (UiRbSplitByEco.IsChecked == true)
            {
                LastSplitBy = SplitBy.ECO;
                if (UiRbCritAtoE.IsChecked == true)
                {
                    LastSplitByCrtierion = SplitByCriterion.ECO_AE;
                }
                else if (UiRbCritA0toE9.IsChecked == true)
                {
                    LastSplitByCrtierion = SplitByCriterion.ECO_A0E9;
                }
                else
                {
                    LastSplitByCrtierion = SplitByCriterion.ECO_A00E99;
                }
            }
            else if (UiRbSplitByDate.IsChecked == true)
            {
                LastSplitBy = SplitBy.DATE;
                if (UiRbCritYear.IsChecked == true)
                {
                    LastSplitByCrtierion = SplitByCriterion.DATE_YEAR;
                }
                else if (UiRbCritMonth.IsChecked == true)
                {
                    LastSplitByCrtierion = SplitByCriterion.DATE_MONTH;
                }
                else
                {
                    LastSplitByCrtierion = SplitByCriterion.DATE_DAY;
                }
            }
            else
            {
                LastSplitBy = SplitBy.ROUND;
            }


            DialogResult = true;
        }

        /// <summary>
        /// Links to the relevant Wiki page.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnHelp_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/czbar/ChessForge/wiki/Splitting-Chapter");
        }

        /// <summary>
        /// The ditribute by ECO box was checked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiCbDistribByEco_Checked(object sender, RoutedEventArgs e)
        {
            ShowSplitByGroups(false);
        }

        /// <summary>
        /// The distribute by ECO box was unchecked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiCbDistribByEco_Unchecked(object sender, RoutedEventArgs e)
        {
            ShowSplitByGroups(true);
        }
    }
}
