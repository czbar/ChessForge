using ChessPosition.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;

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
            MoveAssessment.AdjustConfiguration();

            UiTbBlunderMinDiff.Text = Configuration.BlunderDetectEvalDrop.ToString();
            UiTbBlunderMaxThresh.Text = Configuration.BlunderNoDetectThresh.ToString();

            UiTbMistakeMinDiff.Text = Configuration.MistakeDetectEvalDrop.ToString();
            UiTbMistakeMaxThresh.Text = Configuration.MistakeNoDetectThresh.ToString();
        }

        /// <summary>
        /// Updates configuration values before exit.
        /// </summary>
        private void UpdateConfigValues()
        {
            int val;

            if (int.TryParse(UiTbBlunderMinDiff.Text, out val))
            {
                Configuration.BlunderDetectEvalDrop = (uint)Math.Abs(val);
            }
            if (int.TryParse(UiTbBlunderMaxThresh.Text, out val))
            {
                Configuration.BlunderNoDetectThresh = (uint)Math.Abs(val);
            }

            if (int.TryParse(UiTbMistakeMinDiff.Text, out val))
            {
                Configuration.MistakeDetectEvalDrop = (uint)Math.Abs(val);
            }
            if (int.TryParse(UiTbMistakeMaxThresh.Text, out val))
            {
                Configuration.MistakeNoDetectThresh = (uint)Math.Abs(val);
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
            MoveAssessment.Initialized = false;
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
            System.Diagnostics.Process.Start("https://github.com/czbar/ChessForge/wiki/Bad-Move-Detection");
        }
    }
}
