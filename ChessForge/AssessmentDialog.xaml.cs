using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for AssessmentDialog.xaml
    /// </summary>
    public partial class AssessmentDialog : Window
    {
        /// <summary>
        /// Coach's assessment token
        /// </summary>
        public ChfCommands.Assessment Assessment { get; set; }

        /// <summary>
        /// Comment for the move for which this dialog was invoked.
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// Whether exit occurred on user's pushing the OK button  
        /// </summary>
        public bool ExitOk = false;

        /// <summary>
        /// Constructs the dialog.
        /// Sets the values passed by the caller.
        /// </summary>
        /// <param name="ass"></param>
        /// <param name="comment"></param>
        public AssessmentDialog(TreeNode nd)
        {
            InitializeComponent();
            Assessment = ChfCommands.GetAssessment(nd.Assessment);
            SetAssessmentRadioButtons();
            UiTbComment.Text = nd.Comment ?? "";
        }

        /// <summary>
        /// Closes the dialog after user pushed the Cancel button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnCancel_Click(object sender, RoutedEventArgs e)
        {
            ExitOk = false;
            Close();
        }

        /// <summary>
        /// Closes the dialog after user pushed the OK button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnOk_Click(object sender, RoutedEventArgs e)
        {
            Comment = UiTbComment.Text;
            DetermineCheckedAssessmentValue();
            ExitOk = true;
            Close();
        }

        /// <summary>
        /// Determines which radio button is set and sets
        /// the Assessment property accordingly
        /// </summary>
        private void DetermineCheckedAssessmentValue()
        {
            if (UiRbNone.IsChecked == true)
                Assessment = ChfCommands.Assessment.NONE;
            else if (UiRbBest.IsChecked == true)
                Assessment = ChfCommands.Assessment.BEST;
            else if (UiRbBlunder.IsChecked == true)
                Assessment = ChfCommands.Assessment.BLUNDER;
            else if (UiRbDubious.IsChecked == true)
                Assessment = ChfCommands.Assessment.DUBIOUS;
            else if (UiRbBrilliant.IsChecked == true)
                Assessment = ChfCommands.Assessment.BRILLIANT;
            else if (UiRbMistake.IsChecked == true)
                Assessment = ChfCommands.Assessment.MISTAKE;
            else if (UiRbOnly.IsChecked == true)
                Assessment = ChfCommands.Assessment.ONLY;
        }

        /// <summary>
        /// Checks the radio button coresponding to the passed Assessment value
        /// </summary>
        private void SetAssessmentRadioButtons()
        {
            switch (Assessment)
            {
                case ChfCommands.Assessment.NONE:
                    UiRbNone.IsChecked = true;
                    break;
                case ChfCommands.Assessment.BEST:
                    UiRbBest.IsChecked = true;
                    break;
                case ChfCommands.Assessment.BLUNDER:
                    UiRbBlunder.IsChecked = true;
                    break;
                case ChfCommands.Assessment.DUBIOUS:
                    UiRbDubious.IsChecked = true;
                    break;
                case ChfCommands.Assessment.BRILLIANT:
                    UiRbBrilliant.IsChecked = true;
                    break;
                case ChfCommands.Assessment.MISTAKE:
                    UiRbMistake.IsChecked = true;
                    break;
                case ChfCommands.Assessment.ONLY:
                    UiRbOnly.IsChecked = true;
                    break;
            }
        }
    }
}
