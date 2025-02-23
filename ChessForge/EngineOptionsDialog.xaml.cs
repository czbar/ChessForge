using ChessPosition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for EngineOptionsDialog.xaml
    /// </summary>
    public partial class EngineOptionsDialog : Window
    {
        // number of threads on entry
        private int _currentEngineThreads;

        // hash table size on entry
        private long _currentEngineHashSize;

        /// <summary>
        /// Set on Exit to indicate whether engine parameters changed
        /// that require updating engine options.
        /// </summary>
        public bool EngineParamsChanged = false;

        /// <summary>
        /// Configured path to the engine's executable.
        /// </summary>
        public string EnginePath;

        /// <summary>
        /// Configured time for engine to think on move
        /// while in a game.
        /// </summary>
        public double EngineTimePerMoveInGame;

        /// <summary>
        /// Configured time for engine to think on move
        /// while evaluating.
        /// </summary>
        public double EngineTimePerMoveInEvaluation;

        /// <summary>
        /// Number of lines returned by the engine
        /// </summary>
        public double EngineMpv;

        /// <summary>
        /// Whether to show engine analysis depth in the engine lines window.
        /// </summary>
        public bool EngineShowAnalysisDepth;

        /// <summary>
        /// Tolerance of engine move selection in centipawns
        /// while evaluating.
        /// </summary>
        public int EngineMoveAccuracy;

        /// <summary>
        /// Number of threads for the engine to use.
        /// </summary>
        public int EngineThreads;

        /// <summary>
        /// Hash table size.
        /// </summary>
        public long EngineHashSize;

        /// <summary>
        // Whether the engine path was changed by the user in this dialog.
        /// </summary>
        public bool ChangedEnginePath = false;

        // path to the engine as this dialog is invoked,
        private string _originalEnginePath;


        // flag to block sync'ing loop between the slider and the text box
        private bool _blockAccuracySync = false;

        /// <summary>
        /// Constructor.
        /// </summary>
        public EngineOptionsDialog()
        {
            InitializeComponent();
            EnginePath = Configuration.EngineExePath;
            _originalEnginePath = EnginePath;

            EngineTimePerMoveInGame = (double)Configuration.EngineMoveTime / 1000.0;
            EngineTimePerMoveInEvaluation = (double)Configuration.EngineEvaluationTime / 1000.0;
            EngineMpv = Configuration.EngineMpv;
            EngineShowAnalysisDepth = Configuration.ShowEngineAnalysisDepth;
            EngineMoveAccuracy = (int)Configuration.ViableMoveCpDiff;
            EngineThreads = (int)Configuration.EngineThreads;
            EngineHashSize = (long)Configuration.EngineHashSize;

            _currentEngineThreads = Configuration.EngineThreads;
            _currentEngineHashSize = Configuration.EngineHashSize;

            UiTbEngineExe.Text = EnginePath;
            UiTbEngTimeInGame.Text = EngineTimePerMoveInGame.ToString("F1");
            UiTbEngEvalTime.Text = EngineTimePerMoveInEvaluation.ToString("F1");
            UiTbMultiPv.Text = EngineMpv.ToString();
            UiCbShowDepth.IsChecked = EngineShowAnalysisDepth;
            UiTbMoveAcc.Text = EngineMoveAccuracy.ToString();
            UiTbThreads.Text = EngineThreads.ToString();
            UiTbHashSize.Text = EngineHashSize.ToString();

            UiSldAccuracy.Minimum = 0;
            UiSldAccuracy.Maximum = 100;

            _blockAccuracySync = true;
            SetSliderPosition(GuiUtilities.ConvertCentipawnsToAccuracy((uint)EngineMoveAccuracy));
            _blockAccuracySync = false;
        }

        /// <summary>
        /// Sets the slider's position according to the passed
        /// accuracy value.
        /// </summary>
        /// <param name="accuracy"></param>
        private void SetSliderPosition(uint accuracy)
        {
            UiSldAccuracy.Value = accuracy;
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
                UiTbEngineExe.Text = EnginePath;
            }
        }

        /// <summary>
        /// Saves the values in the Configuration object and exits
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (_originalEnginePath == EnginePath)
            {
                ChangedEnginePath = false;
            }
            else
            {
                ChangedEnginePath = true;
                Configuration.EngineExePath = EnginePath;
            }

            double dval;
            int iVal;
            long lVal;

            if (double.TryParse(UiTbEngTimeInGame.Text, out dval))
            {
                Configuration.EngineMoveTime = (int)(dval * 1000);
            }

            if (double.TryParse(UiTbEngEvalTime.Text, out dval))
            {
                Configuration.EngineEvaluationTime = (int)(dval * 1000);
            }

            if (int.TryParse(UiTbMultiPv.Text, out iVal))
            {
                Configuration.EngineMpv = iVal;
            }

            Configuration.ShowEngineAnalysisDepth = UiCbShowDepth.IsChecked == true;

            if (int.TryParse(UiTbMoveAcc.Text, out iVal))
            {
                Configuration.ViableMoveCpDiff = iVal;
            }

            if (int.TryParse(UiTbThreads.Text, out iVal))
            {
                Configuration.EngineThreads = iVal;
            }

            if (long.TryParse(UiTbHashSize.Text, out lVal))
            {
                Configuration.EngineHashSize = lVal;
            }

            if (_currentEngineThreads != Configuration.EngineThreads
                || _currentEngineHashSize != Configuration.EngineHashSize)
            {
                EngineParamsChanged = true;
            }

            DialogResult = true;
        }

        /// <summary>
        /// Exits without saving the values.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        /// <summary>
        /// Links to the relevant Wiki page.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnHelp_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/czbar/ChessForge/wiki/Engine-Options-Dialog");
        }

        /// <summary>
        /// Handles the change in the slider's value.
        /// Updates the accuracy text box while blocking sync loop.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiSldAccuracy_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_blockAccuracySync)
            {
                return;
            }

            _blockAccuracySync = true;
            UiTbMoveAcc.Text = GuiUtilities.ConvertAccuracyToCentipawns((uint)(UiSldAccuracy.Value)).ToString();
            _blockAccuracySync = false;
        }

        /// <summary>
        /// Handles the change in the accuracy text box.
        /// Updates the accuracy slider while blocking sync loop.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiTbMoveAcc_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_blockAccuracySync)
            {
                return;
            }

            _blockAccuracySync = true;
            if (uint.TryParse(UiTbMoveAcc.Text, out uint uiVal))
            {
                UiSldAccuracy.Value = GuiUtilities.ConvertCentipawnsToAccuracy(uiVal);
            }
            _blockAccuracySync = false;
        }
    }
}
