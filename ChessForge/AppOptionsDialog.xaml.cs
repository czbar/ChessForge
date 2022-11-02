using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using ChessPosition;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for AppOptions.xaml
    /// </summary>
    public partial class AppOptionsDialog : Window
    {
        /// <summary>
        /// True if dialog exited by clicking Save
        /// </summary>
        public bool ExitOK = false;

        public string EnginePath;

        public double ReplaySpeed;

        public double EngineTimePerMoveInGame;

        public double EngineTimePerMoveInEvaluation;

        public bool AllowMouseWheel;

        public bool ShowMovesAtFork;

        // indicates whether the engine path was changed by the user in this dialog.
        public bool ChangedEnginePath = false;

        /// <summary>
        /// Creates the dialog and initializes the controls with
        /// formatted configuration values.
        /// </summary>
        public AppOptionsDialog()
        {
            InitializeComponent();
            EnginePath = Configuration.EngineExePath;
            ReplaySpeed = (double)Configuration.MoveSpeed / 1000.0;
            EngineTimePerMoveInGame = (double)Configuration.EngineMoveTime / 1000.0;
            EngineTimePerMoveInEvaluation = (double)Configuration.EngineEvaluationTime / 1000.0;
            AllowMouseWheel = Configuration.AllowMouseWheelForMoves;
            ShowMovesAtFork = Configuration.ShowMovesAtFork;

            UiTbEngineExe.Text = EnginePath;
            UiTbReplaySpeed.Text = ReplaySpeed.ToString("F1");
            UiTbEngTimeInGame.Text = EngineTimePerMoveInGame.ToString("F1");
            UiTbEngEvalTime.Text = EngineTimePerMoveInEvaluation.ToString("F1");
            UiCbAllowWheel.IsChecked = (AllowMouseWheel == true);
            UiCbShowForkMoves.IsChecked = (ShowMovesAtFork == true);
        }

        /// <summary>
        /// Invokes the Configuration object's Select Engine dialog.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnLocateEngine_Click(object sender, RoutedEventArgs e)
        {
            string searchPath = Path.GetDirectoryName(Configuration.EngineExePath);
            string res = Configuration.SelectEngineExecutable(searchPath);
            if (!string.IsNullOrEmpty(res))
            {
                EnginePath = res;
            }
        }

        /// <summary>
        /// Saves the values in the Configuration object and exits
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (Configuration.EngineExePath == EnginePath)
            {
                ChangedEnginePath = false;
            }
            else
            {
                ChangedEnginePath = true;
                Configuration.EngineExePath = EnginePath;
            }

            double dval;

            if (double.TryParse(UiTbReplaySpeed.Text, out dval))
            {
                Configuration.MoveSpeed = (int)(dval * 1000);
            }

            if (double.TryParse(UiTbEngTimeInGame.Text, out dval))
            {
                Configuration.EngineMoveTime = (int)(dval * 1000);
            }

            if (double.TryParse(UiTbEngEvalTime.Text, out dval))
            {
                Configuration.EngineEvaluationTime = (int)(dval * 1000);
            }

            Configuration.AllowMouseWheelForMoves = (UiCbAllowWheel.IsChecked == true); 
            Configuration.ShowMovesAtFork = (UiCbShowForkMoves.IsChecked == true);

            ExitOK = true;
            this.Close();
        }

        /// <summary>
        /// Exits without saving the values.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnCancel_Click(object sender, RoutedEventArgs e)
        {
            ExitOK = false;
            Close();
        }
    }
}
