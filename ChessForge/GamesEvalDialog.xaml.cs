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

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for GamesEvalDialog.xaml
    /// </summary>
    public partial class GamesEvalDialog : Window
    {
        /// <summary>
        /// Initializes the dialog
        /// </summary>
        /// <param name="plyCountToEvaluate"></param>
        /// <param name="estEvaluationTime"></param>
        public GamesEvalDialog(int plyCountToEvaluate, long estEvaluationTime)
        {
            InitializeComponent();
        }

        /// <summary>
        /// This will be called if there was a fatal error while evalating
        /// </summary>
        public void AbandonEvaluation()
        {
            GamesEvaluationManager.IsEvaluationInProgress = false;
            DialogResult = false;
        }

        /// <summary>
        /// The user requested that the evaluation be stopped.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnCancel_Click(object sender, RoutedEventArgs e)
        {
            AppState.MainWin.StopEvaluation(true);
            GamesEvaluationManager.IsEvaluationInProgress = false;
            DialogResult = false;
        }
    }
}
