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

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for BlunderDetectionDialog.xaml
    /// </summary>
    public partial class BlunderDetectionDialog : Window
    {
        /// <summary>
        /// Constructors. Sets up initial values.
        /// </summary>
        public BlunderDetectionDialog()
        {
            InitializeComponent();

            UiTbMinDiff.Text = Configuration.BlunderDetectEvalDrop.ToString();
            UiTbMaxThresh.Text = Configuration.BlunderNoDetectThresh.ToString();
        }

        /// <summary>
        /// Updates configuration values before exit.
        /// </summary>
        private void UpdateConfigValues()
        {
            int val;

            if (int.TryParse(UiTbMinDiff.Text, out val))
            {
                Configuration.BlunderDetectEvalDrop = (uint)Math.Abs(val);
            }
            if (int.TryParse(UiTbMaxThresh.Text, out val))
            {
                Configuration.BlunderNoDetectThresh = (uint)Math.Abs(val);
            }
        }

        /// <summary>
        /// Update configuration parameters and exit.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnOk_Click(object sender, RoutedEventArgs e)
        {
            UpdateConfigValues();
            DialogResult = true;
        }

        /// <summary>
        /// Exit without updating
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        /// <summary>
        /// Link to the relevant Wiki page
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnHelp_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/czbar/ChessForge/wiki/Blunder-Detection-Dialog");
        }
    }
}
