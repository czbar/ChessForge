using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.IO;

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

        public AppOptionsDialog()
        {
            InitializeComponent();
            EnginePath = Configuration.EngineExePath;
            ReplaySpeed = (double)Configuration.MoveSpeed / 1000.0;
            EngineTimePerMoveInGame = (double)Configuration.EngineMoveTime / 1000.0;
            EngineTimePerMoveInEvaluation = (double)Configuration.EngineEvaluationTime / 1000.0;

            _tbEngineExe.Text = EnginePath;
            _tbReplaySpeed.Text = ReplaySpeed.ToString("F1");
            _tbEngTimeInGame.Text = EngineTimePerMoveInGame.ToString("F1");
            _tbEngEvalTime.Text = EngineTimePerMoveInEvaluation.ToString("F1");
        }

        private void _btnLocateEngine_Click(object sender, RoutedEventArgs e)
        {
            string searchPath = Path.GetDirectoryName(Configuration.EngineExePath);
            string res = Configuration.SelectEngineExecutable(searchPath);
            if (!string.IsNullOrEmpty(res))
            {
                EnginePath = res;
            }
        }

        private void _btnSave_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
