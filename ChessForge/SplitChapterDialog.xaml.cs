using System.Windows;

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
        DATE_DAY,

        ROUND_ONE_NUMBER,
        ROUND_TWO_NUMBERS,
    }

    /// <summary>
    /// Interaction logic for SplitChapterDialog.xaml
    /// </summary>
    public partial class SplitChapterDialog : Window
    {
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

                case SplitByCriterion.ROUND_ONE_NUMBER:
                    UiRbCrit_12.IsChecked = true;
                    break;
                case SplitByCriterion.ROUND_TWO_NUMBERS:
                    UiRbCrit_11_12.IsChecked = true;
                    break;
            }
        }

        /// <summary>
        /// Set the radio button selection for the non-selected current SplitBy buttons. 
        /// </summary>
        /// <param name="splitBy"></param>
        private void SetDefaultCriterionButton(SplitBy splitBy)
        {
            switch (splitBy)
            {
                case SplitBy.ECO:
                    UiRbCritYear.IsChecked = true;
                    UiRbCrit_12.IsChecked = true;
                    break;
                case SplitBy.DATE:
                    UiRbCritAtoE.IsChecked = true;
                    UiRbCrit_12.IsChecked = true;
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
                if (UiRbCrit_12.IsChecked == true)
                {
                    LastSplitByCrtierion = SplitByCriterion.ROUND_ONE_NUMBER;
                }
                else
                {
                    LastSplitByCrtierion = SplitByCriterion.ROUND_TWO_NUMBERS;
                }
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
    }
}
