using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for TrainingSessionControl.xaml
    /// </summary>
    public partial class TrainingSessionControl : UserControl
    {
        /// <summary>
        /// Creates the dialog and applies the current color theme.
        /// </summary>
        public TrainingSessionControl()
        {
            InitializeComponent();
            ShowElements(false);
        }

        /// <summary>
        /// Shows and hides element per the current state of training.
        /// If the session game is on, the user has multiple options on
        /// how to exit. 
        /// Otherwise, there is only a simple "no save"
        /// exit by clicking the exit button.
        /// </summary>
        /// <param name="isGame"></param>
        public void ShowElements(bool isGame)
        {
            UiBtnExit.Visibility = isGame ? Visibility.Collapsed : Visibility.Visible;

            UiLblExit.Visibility = isGame ? Visibility.Visible : Visibility.Collapsed;
            UiBtnNoSave.Visibility = isGame ? Visibility.Visible : Visibility.Collapsed;
            UiBtnMergeMoves.Visibility = isGame ? Visibility.Visible : Visibility.Collapsed;
            UiBtnCreateGame.Visibility = isGame ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Applies the current color theme to the dialog.
        /// </summary>
        /// <param name="theme"></param>
        public void ApplyColorTheme(ThemeColorSet theme)
        {
            this.BorderBrush = theme.RtbForeground;
            UiLblExit.Foreground = theme.RtbForeground; 
        }

        /// <summary>
        /// Exit without saving. The training session did not reach the game stage.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnExit_Click(object sender, RoutedEventArgs e)
        {
            AppState.MainWin.ExitTrainingFromSessionBox(false, false);
        }

        /// <summary>
        /// Invoke the menu item handling.
        /// This is the "slow" exit when the is asked how to exit.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiLblExit_MouseDown(object sender, MouseButtonEventArgs e)
        {
            AppState.MainWin.UiMnStopTraining_Click(null, null);
        }

        /// <summary>
        /// Exit without saving anything.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnNoSave_Click(object sender, RoutedEventArgs e)
        {
            AppState.MainWin.ExitTrainingFromSessionBox(false, false);
        }

        /// <summary>
        /// Merge training moves to the source.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnMergeMoves_Click(object sender, RoutedEventArgs e)
        {
            AppState.MainWin.ExitTrainingFromSessionBox(true, false);
        }

        /// <summary>
        /// Do not merge training moves. Create a new game instead.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnCreateGame_Click(object sender, RoutedEventArgs e)
        {
            AppState.MainWin.ExitTrainingFromSessionBox(false, true);
        }
    }
}
