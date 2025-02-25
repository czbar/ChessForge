using ChessPosition;
using GameTree;
using System;
using System.Windows;
using System.Windows.Input;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for GameHeaderDialog.xaml
    /// </summary>
    public partial class GameHeaderDialog : Window
    {
        // indictates if the dialog was exited on user pressing OK
        public bool ExitOK = false;

        // VariationTree for this dialog to operate on
        private VariationTree _tree;

        /// <summary>
        /// Constructor to create the dialog.
        /// </summary>
        /// <param name="tree"></param>
        public GameHeaderDialog(VariationTree tree, string title)
        {
            _tree = tree;
            InitializeComponent();
            InitializeData();

            this.Title = title;
        }

        /// <summary>
        /// Sets the content of the controls.
        /// </summary>
        private void InitializeData()
        {
            UiTbWhite.Text = _tree.Header.GetWhitePlayer(out _) ?? "";
            UiTbBlack.Text = _tree.Header.GetBlackPlayer(out _) ?? "";

            UiTbWhiteElo.Text = _tree.Header.GetWhitePlayerElo(out _) ?? "";
            UiTbBlackElo.Text = _tree.Header.GetBlackPlayerElo(out _) ?? "";

            UiTbEco.Text = _tree.Header.GetECO(out _) ?? "";

            UiTbEvent.Text = _tree.Header.GetEventName(out _) ?? "";
            UiTbRound.Text = _tree.Header.GetRound(out _) ?? "";

            UiTbAnnotator.Text = _tree.Header.GetAnnotator(out _) ?? "";
            UiTbPreamble.Text = _tree.Header.BuildPreambleText();

            UiTbFirstMoveNumber.Text = (_tree.MoveNumberOffset + 1).ToString();

            SetDateControls();
            SetResultRadioButton();

            if (_tree.ContentType == GameData.ContentType.EXERCISE)
            {
                // make room for the move number offset
                UiTbPreamble.Height = UiTbPreamble.Height - 30;
                UiTbFirstMoveNumber.Visibility = Visibility.Visible;
                UiLblFirstMoveNumber.Visibility = Visibility.Visible;
            }

            UiTbWhite.Focus();
            UiTbWhite.SelectAll();

        }

        /// <summary>
        /// Sets the text in the Date TextBox.
        /// </summary>
        private void SetDateControls()
        {
            string date = TextUtils.AdjustPgnDateString(_tree.Header.GetDate(out _), out bool hasMonth, out bool hasDay);
            SetCheckBoxes(hasMonth, hasDay);

            UiTbPgnDate.Text = date;
            UiDatePicker.SelectedDate = TextUtils.GetDateFromPgnString(date);
        }

        /// <summary>
        /// Sets date parts checkboxes.
        /// </summary>
        /// <param name="hasMonth"></param>
        /// <param name="hasDay"></param>
        private void SetCheckBoxes(bool hasMonth, bool hasDay)
        {
            UiCbIgnoreMonthDay.IsChecked = !hasMonth;
            UiCbIgnoreDay.IsChecked = !hasMonth || !hasDay;
        }

        /// <summary>
        /// Selects the appropriate radio button.
        /// </summary>
        private void SetResultRadioButton()
        {
            string result = _tree.Header.GetResult(out _);
            if (string.IsNullOrEmpty(result))
            {
                UiRbNoResult.IsChecked = true;
            }
            else
            {
                result = result.Trim();
                if (result.StartsWith(Constants.PGN_DRAW_SHORT_RESULT))
                {
                    UiRbDraw.IsChecked = true;
                }
                else if (result.StartsWith(Constants.PGN_WHITE_WIN_RESULT) || result.StartsWith(Constants.PGN_WHITE_WIN_RESULT_EX))
                {
                    UiRbWhiteWin.IsChecked = true;
                }
                else if (result.StartsWith(Constants.PGN_BLACK_WIN_RESULT) || result.StartsWith(Constants.PGN_BLACK_WIN_RESULT_EX))
                {
                    UiRbBlackWin.IsChecked = true;
                }
                else
                {
                    UiRbNoResult.IsChecked = true;
                }
            }
        }

        /// <summary>
        /// Collects data from the controls and sets the properties
        /// of the Tree object being operated on.
        /// </summary>
        private void CollectData()
        {
            _tree.Header.SetHeaderValue(PgnHeaders.KEY_WHITE, UiTbWhite.Text);
            _tree.Header.SetHeaderValue(PgnHeaders.KEY_BLACK, UiTbBlack.Text);
            _tree.Header.SetHeaderValue(PgnHeaders.KEY_WHITE_ELO, UiTbWhiteElo.Text);
            _tree.Header.SetHeaderValue(PgnHeaders.KEY_BLACK_ELO, UiTbBlackElo.Text);
            _tree.Header.SetHeaderValue(PgnHeaders.KEY_ANNOTATOR, UiTbAnnotator.Text);

            _tree.Header.SetHeaderValue(PgnHeaders.KEY_ECO, UiTbEco.Text);

            _tree.Header.SetHeaderValue(PgnHeaders.KEY_EVENT, UiTbEvent.Text);

            _tree.Header.SetHeaderValue(PgnHeaders.KEY_DATE, TextUtils.AdjustPgnDateString(UiTbPgnDate.Text, out _, out _));

            if (double.TryParse(UiTbRound.Text, out double round))
            {
                _tree.Header.SetHeaderValue(PgnHeaders.KEY_ROUND, round.ToString("0.#####"));
            }
            else
            {
                _tree.Header.SetHeaderValue(PgnHeaders.KEY_ROUND, "");
            }

            _tree.Header.SetHeaderValue(PgnHeaders.KEY_RESULT, CollectResultString());

            _tree.Header.SetPreamble(UiTbPreamble.Text);

            if (uint.TryParse(UiTbFirstMoveNumber.Text, out uint firstMoveNumber))
            {
                if (firstMoveNumber > 0 && firstMoveNumber <= 1000)
                {
                    _tree.MoveNumberOffset = firstMoveNumber - 1;
                }
            }
        }

        /// <summary>
        /// Checks which Radio Button is selected and sets the result
        /// string appropriately
        /// </summary>
        /// <returns></returns>
        private string CollectResultString()
        {
            if (UiRbWhiteWin.IsChecked == true)
                return Constants.PGN_WHITE_WIN_RESULT;
            else if (UiRbBlackWin.IsChecked == true)
                return Constants.PGN_BLACK_WIN_RESULT;
            else if (UiRbDraw.IsChecked == true)
                return Constants.PGN_DRAW_RESULT;
            else
                return Constants.PGN_NO_RESULT;
        }

        /// <summary>
        /// Set data in the Tree object and exit
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnOk_Click(object sender, RoutedEventArgs e)
        {
            CollectData();
            ExitOK = true;
            Close();
        }

        /// <summary>
        /// Exits without persisting the edits.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnCancel_Click(object sender, RoutedEventArgs e)
        {
            ExitOK = false;
            Close();
        }

        /// <summary>
        /// Links to the relevant Wiki page.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnHelp_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/czbar/ChessForge/wiki/Game-Header-Editor");
        }

        /// <summary>
        /// Clears the date controls.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnClearDate_Click(object sender, RoutedEventArgs e)
        {
            UiDatePicker.Text = "";
            UiTbPgnDate.Text = TextUtils.BuildPgnDateString(null);
        }

        /// <summary>
        /// Sets the pgn text date when Date Picker loses focus.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiDatePicker_LostFocus(object sender, RoutedEventArgs e)
        {
            DateTime? dt = UiDatePicker.SelectedDate;
            UiTbPgnDate.Text = TextUtils.BuildPgnDateString(dt, UiCbIgnoreMonthDay.IsChecked == true, UiCbIgnoreDay.IsChecked == true);
        }

        /// <summary>
        /// Parses the text set in the pgn text box and sets the Date Picker accordingly.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiTbPgnDate_LostFocus(object sender, RoutedEventArgs e)
        {
            string pgnDate = TextUtils.AdjustPgnDateString(UiTbPgnDate.Text, out bool hasMonth, out bool hasDay);
            SetCheckBoxes(hasMonth, hasDay);

            UiTbPgnDate.Text = pgnDate;
            DateTime? dt = TextUtils.GetDateFromPgnString(pgnDate);
            UiDatePicker.SelectedDate = dt;
        }

        /// <summary>
        /// Handles IgnoreMonthDay check event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiCbIgnoreMonthDay_Checked(object sender, RoutedEventArgs e)
        {
            CheckedEventOccurred();
        }

        /// <summary>
        /// Handles IgnoreMonthDay uncheck event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiCbIgnoreMonthDay_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckedEventOccurred();
        }

        /// <summary>
        /// Handles IgnoreDay uncheck event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiCbIgnoreDay_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckedEventOccurred();
        }

        /// <summary>
        /// Handles IgnoreDay check event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiCbIgnoreDay_Checked(object sender, RoutedEventArgs e)
        {
            CheckedEventOccurred();
        }

        /// <summary>
        /// Adjusts date controls content after one of the "ignore month/day" boxes was checked.
        /// </summary>
        private void CheckedEventOccurred()
        {
            UiTbPgnDate.Text = TextUtils.BuildPgnDateString(UiDatePicker.SelectedDate, UiCbIgnoreMonthDay.IsChecked == true, UiCbIgnoreDay.IsChecked == true);
            DateTime? dt = TextUtils.GetDateFromPgnString(UiTbPgnDate.Text);

            if (dt != null)
            {
                int year = dt.Value.Year;
                int month = dt.Value.Month;
                int day = dt.Value.Day;

                if (UiCbIgnoreMonthDay.IsChecked == true)
                {
                    // if month got "ignored", preserve it
                    month = UiDatePicker.SelectedDate.Value.Month;
                    day = UiDatePicker.SelectedDate.Value.Day;
                }
                else if (UiCbIgnoreDay.IsChecked == true)
                {
                    // if day got "ignored" day, preserve it
                    day = UiDatePicker.SelectedDate.Value.Day;
                }

                UiDatePicker.SelectedDate = new DateTime(year, month, day);
            }
        }

        /// <summary>
        /// Check if the user pressed key combination to enter a figurine.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiTbPreamble_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (GuiUtilities.InsertFigurine(UiTbPreamble, sender, e))
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// Check if the user pressed key combination to enter a figurine.
        /// We may need it, when use White/Black/Event fields for "pseudo-names"
        /// in lines/tests etc.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiTbWhite_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (GuiUtilities.InsertFigurine(UiTbWhite, sender, e))
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// Check if the user pressed key combination to enter a figurine.
        /// We may need it, when use White/Black/Event fields for "pseudo-names"
        /// in lines/tests etc.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiTbBlack_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (GuiUtilities.InsertFigurine(UiTbBlack, sender, e))
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// Check if the user pressed key combination to enter a figurine.
        /// We may need it, when use White/Black/Event fields for "pseudo-names"
        /// in lines/tests etc.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiTbEvent_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (GuiUtilities.InsertFigurine(UiTbEvent, sender, e))
            {
                e.Handled = true;
            }
        }
    }
}
