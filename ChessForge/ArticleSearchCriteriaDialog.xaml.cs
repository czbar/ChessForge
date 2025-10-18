using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for ArticleSearchCriteriaDialog.xaml
    /// </summary>
    public partial class ArticleSearchCriteriaDialog : Window
    {
        //**** values persisted between closing and re-opening the dialog. ***

        private static string _whiteName = "";
        private static string _blackName = "";

        private static bool? _ignoreColors = false;

        private static string _minMoves = "";
        private static string _maxMoves = "";
        private static string _minYear = "";
        private static string _maxYear = "";
        private static bool? _includeEmptyYear = false;

        private static string _minECO = "";
        private static string _maxECO = "";

        private static bool? _resultWhiteWin = false;
        private static bool? _resultBlackWin = false;
        private static bool? _resultDraw = false;
        private static bool? _resultNone = false;


        /// <summary>
        /// Constructor.
        /// </summary>
        public ArticleSearchCriteriaDialog()
        {
            InitializeComponent();
            RestoreLastValues();
            UiTbWhite.Focus();
            UiTbWhite.SelectAll();
        }

        /// <summary>
        /// Set initial values as per the last session
        /// </summary>
        private void RestoreLastValues()
        {
            UiTbWhite.Text = _whiteName;
            UiTbBlack.Text = _blackName;

            UiCbIgnoreColors.IsChecked = _ignoreColors;

            UiTbMinMoves.Text = _minMoves;
            UiTbMaxMoves.Text = _maxMoves;
            UiTbMinYear.Text = _minYear;
            UiTbMaxYear.Text = _maxYear;
            UiCbEmptyYear.IsChecked = _includeEmptyYear;

            UiTbMinEco.Text = _minECO;
            UiTbMaxEco.Text = _maxECO;

            UiCbWhiteWin.IsChecked = _resultWhiteWin;
            UiCbWhiteLoss.IsChecked = _resultBlackWin;
            UiCbDraw.IsChecked = _resultDraw;
            UiCbNoResult.IsChecked = _resultNone;
        }

        /// <summary>
        /// Saves values on exit to use when initializing the next time.
        /// </summary>
        private void SaveValuesOnExit()
        {
            _whiteName = UiTbWhite.Text;
            _blackName = UiTbBlack.Text;

            _ignoreColors = UiCbIgnoreColors.IsChecked;

            _minMoves = UiTbMinMoves.Text;
            _maxMoves = UiTbMaxMoves.Text;
            _minYear = UiTbMinYear.Text;
            _maxYear = UiTbMaxYear.Text;
            _includeEmptyYear = UiCbEmptyYear.IsChecked;

            _minECO = UiTbMinEco.Text;
            _maxECO = UiTbMaxEco.Text;

            _resultWhiteWin = UiCbWhiteWin.IsChecked;
            _resultBlackWin = UiCbWhiteLoss.IsChecked;
            _resultDraw = UiCbDraw.IsChecked;
            _resultNone = UiCbNoResult.IsChecked;
        }

        /// <summary>
        /// The user clicked OK to exit dialog.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnOk_Click(object sender, RoutedEventArgs e)
        {
            SaveValuesOnExit();
            DialogResult = true;
        }

        /// <summary>
        /// Links to the relevant Wiki page.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnHelp_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/czbar/ChessForge/wiki/Finding-Games");
        }
    }
}
