using System.Windows;

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
        public GamesEvalDialog()
        {
            InitializeComponent();
            UiLblTimeRemaining.Content = Properties.Resources.Calculating;
        }

        /// <summary>
        /// This will be called if there was a fatal error while evaluating
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
