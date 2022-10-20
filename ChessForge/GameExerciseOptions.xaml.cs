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
using GameTree;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for GameExerciseOptions.xaml
    /// </summary>
    public partial class GameExerciseOptions : Window
    {
        // VariationTree for this dialog to operate on
        private VariationTree _tree;

        /// <summary>
        /// Constructor to create the dialog.
        /// </summary>
        /// <param name="tree"></param>
        public GameExerciseOptions(VariationTree tree)
        {
            _tree = tree;
            InitializeComponent();
            InitializeData();
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

            SetResultRadioButton();
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
                if (result.StartsWith("1/2"))
                {
                    UiRbDraw.IsChecked = true;
                }
                else if (result.StartsWith("1-0"))
                {
                    UiRbWhiteWin.IsChecked = true;
                }
                else if (result.StartsWith("0-1"))
                {
                    UiRbBlackWin.IsChecked = true;
                }
                else
                {
                    UiRbNoResult.IsChecked = true;
                }
            }
        }
    }
}
