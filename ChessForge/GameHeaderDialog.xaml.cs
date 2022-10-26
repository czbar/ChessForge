using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ChessPosition;
using GameTree;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for GameExerciseOptions.xaml
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

            UiTbEvent.Text = _tree.Header.GetEventName(out _) ?? "";
            UiTbRound.Text = _tree.Header.GetRound(out _) ?? "";

            SetDateTextBox();
            SetResultRadioButton();
        }

        /// <summary>
        /// Sets the text in the Date TextBox.
        /// </summary>
        private void SetDateTextBox()
        {
            string date = _tree.Header.GetDate(out _);
            if (string.IsNullOrEmpty(date))
            {
                UiDatePicker.Text = "";
            }
            else
            {
                UiDatePicker.Text = date;
            }
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
                else if (result.StartsWith(Constants.PGN_WHITE_WIN_RESULT))
                {
                    UiRbWhiteWin.IsChecked = true;
                }
                else if (result.StartsWith(Constants.PGN_BLACK_WIN_RESULT))
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
            _tree.Header.SetHeaderValue(PgnHeaders.KEY_EVENT, UiTbEvent.Text);

            _tree.Header.SetHeaderValue(PgnHeaders.KEY_DATE, TextUtils.AdjustDateString(UiDatePicker.Text));

            if (int.TryParse(UiTbRound.Text, out int round))
            {
                _tree.Header.SetHeaderValue(PgnHeaders.KEY_ROUND, round.ToString());
            }
            else
            {
                _tree.Header.SetHeaderValue(PgnHeaders.KEY_ROUND, "");
            }

            _tree.Header.SetHeaderValue(PgnHeaders.KEY_RESULT, CollectResultString());
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
    }
}
