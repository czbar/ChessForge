using ChessPosition;
using System.Windows;
using System.Windows.Media;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for AppOptions.xaml
    /// </summary>
    public partial class AppOptionsDialog : Window
    {

        // the code of the language currently configured
        private string _currentConfiguredLanguage = "";

        // UseFigurines value on entry
        private bool _currentUseFigurines;

        // number of threads on entry
        private int _currentEngineThreads;

        // hash table size on entry
        private long _currentEngineHashSize;

        // MainLineCommentLF on entry
        private bool _currentMainLineCommentLF;

        // ExtraSpacing on entry
        private bool _currentExtraSpacing;

        // WideScrollbar on entry
        private bool _currentWideScrollbar;

        // LargeMenuFont on entry
        private bool _currentLargeMenuFont;

        // AutoThumbnailMoveColor on entry
        private bool _autoThumbnailMoveColor;

        /// <summary>
        /// The language selected upon exit
        /// </summary>
        public string ExitLanguage = "";

        /// <summary>
        /// Set on Exit to indicate whether the selected language
        /// differs from the one configured before the dialog was opened
        /// </summary>
        public bool LanguageChanged = false;

        /// <summary>
        /// Set on Exit to indicate whether the UseFigurines value
        /// differs from the one configured before the dialog was opened
        /// </summary>
        public bool UseFigurinesChanged = false;

        /// <summary>
        /// Set on Exit to indicate whether engine parameters changed
        /// that require updating engine options.
        /// </summary>
        public bool EngineParamsChanged = false;

        /// <summary>
        /// Set on Exit to indicate whether WideScrollbar param changed
        /// </summary>
        public bool WideScrollbarChanged = false;

        /// <summary>
        /// Set on Exit to indicate whether LargeMenuFont param changed
        /// </summary>
        public bool LargeMenuFontChanged = false;

        /// <summary>
        /// Set on Exit to indicate whether the MainLineCommentLF
        /// differs from the one configured before the dialog was opened
        /// </summary>
        public bool MainLineCommentLFChanged = false;

        /// <summary>
        /// Set on Exit to indicate whether the ExtraSpacing
        /// differs from the one configured before the dialog was opened
        /// </summary>
        public bool ExtraSpacingChanged = false;
        
        /// <summary>
        /// Configured path to the engine's executable.
        /// </summary>
        public string EnginePath;

        /// <summary>
        /// Configured Replay Speed
        /// </summary>
        public double ReplaySpeed;

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
        /// Whether mouse wheel can be used to scroll through the notation..
        /// </summary>
        public bool AllowMouseWheel;

        /// <summary>
        /// Whether main line comments should be in a separate paragraph.
        /// </summary>
        public bool MainLineCommentLF;

        /// <summary>
        /// Whether extra line spacing is used for the main line comments.
        /// </summary>
        public bool ExtraSpacing;

        /// <summary>
        /// Whether "fork tables" are shown.
        /// </summary>
        public bool ShowMovesAtFork;

        /// <summary>
        /// Whether the sound is turned on.
        /// </summary>
        public bool SoundOn;

        /// <summary>
        /// Whether to use wide the scrollbar
        /// </summary>
        public bool WideScrollbar;

        /// <summary>
        /// Whether to use larger font in the menus
        /// </summary>
        public bool LargeMenuFont;

        /// <summary>
        // Whether the engine path was changed by the user in this dialog.
        /// </summary>
        public bool ChangedEnginePath = false;

        // path to the engine as this dialog is invoked,
        private string _originalEnginePath;

        // foreground for enabled text
        private Brush _defaultForegroundColor = Brushes.Black;

        /// <summary>
        /// Creates the dialog and initializes the controls with
        /// formatted configuration values.
        /// </summary>
        public AppOptionsDialog()
        {
            InitializeComponent();
            EnginePath = Configuration.EngineExePath;
            _originalEnginePath = EnginePath;
            _defaultForegroundColor = UiCbExtraSpacing.Foreground;

            ReplaySpeed = (double)Configuration.MoveSpeed / 1000.0;
            EngineTimePerMoveInGame = (double)Configuration.EngineMoveTime / 1000.0;
            EngineTimePerMoveInEvaluation = (double)Configuration.EngineEvaluationTime / 1000.0;
            EngineMpv = Configuration.EngineMpv;
            EngineMoveAccuracy = (int)Configuration.ViableMoveCpDiff;
            EngineThreads = (int)Configuration.EngineThreads;
            EngineHashSize = (long)Configuration.EngineHashSize;
            AllowMouseWheel = Configuration.AllowMouseWheelForMoves;
            MainLineCommentLF = Configuration.MainLineCommentLF;
            ExtraSpacing = Configuration.ExtraSpacing;
            ShowMovesAtFork = Configuration.ShowMovesAtFork;
            SoundOn = Configuration.SoundOn;
            WideScrollbar = Configuration.WideScrollbar;
            LargeMenuFont = Configuration.LargeMenuFont;

            _currentUseFigurines = Configuration.UseFigurines;
            _currentEngineThreads = Configuration.EngineThreads;
            _currentEngineHashSize = Configuration.EngineHashSize;
            _currentMainLineCommentLF = Configuration.MainLineCommentLF;
            _currentExtraSpacing = Configuration.ExtraSpacing;
            _currentWideScrollbar = Configuration.WideScrollbar;
            _currentLargeMenuFont = Configuration.LargeMenuFont;

            UiTbIndexDepth.Text = Configuration.DefaultIndexDepth.ToString();

            UiTbReplaySpeed.Text = ReplaySpeed.ToString("F1");
            UiCbAllowWheel.IsChecked = (AllowMouseWheel == true);
            UiCbMainLineCommentLF.IsChecked = MainLineCommentLF == true;
            UiCbExtraSpacing.IsChecked = ExtraSpacing == true;
            UiCbShowForkMoves.IsChecked = (ShowMovesAtFork == true);
            UiCbSoundOn.IsChecked = (SoundOn == true);
            UiCbWideScrollbar.IsChecked = (WideScrollbar == true);
            UiCbLargeMenuFont.IsChecked = (LargeMenuFont == true);
            UiCbFigurines.IsChecked = (Configuration.UseFigurines == true);

            UiTbAtMove.Text = Configuration.AutoThumbnailMoveNo.ToString();

            _autoThumbnailMoveColor = Configuration.AutoThumbnailColor;
            SetAutoThumbnailColorText(_autoThumbnailMoveColor);
            SetExtraSpacingCheckBox();

            Languages.AvailableLanguages.Sort();
            foreach (Language lang in Languages.AvailableLanguages)
            {
                UiLbLanguages.Items.Add(lang);
                if (lang.IsSelected)
                {
                    _currentConfiguredLanguage = lang.Code;
                    UiLbLanguages.SelectedItem = lang;
                }
            }
        }

        /// <summary>
        /// Sets the state of the extra spacing check box.
        /// </summary>
        private void SetExtraSpacingCheckBox()
        {
            if (UiCbMainLineCommentLF.IsChecked == true)
            {
                UiCbExtraSpacing.IsEnabled = true;
                UiCbExtraSpacing.Foreground = _defaultForegroundColor;
            }
            else
            {
                UiCbExtraSpacing.IsEnabled = false;
                UiCbExtraSpacing.Foreground = Brushes.Gray;
            }
        }

        /// <summary>
        /// Sets text of the label indicationg auto-thumbnail move color.
        /// </summary>
        /// <param name="isWhite"></param>
        private void SetAutoThumbnailColorText(bool isWhite)
        {
            UiLblMoveBy.Content = isWhite ? Properties.Resources.MadeByWhite : Properties.Resources.MadeByBlack;
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

            if (double.TryParse(UiTbReplaySpeed.Text, out dval))
            {
                Configuration.MoveSpeed = (int)(dval * 1000);
            }

            int iVal;

            if (int.TryParse(UiTbIndexDepth.Text, out iVal))
            {
                Configuration.DefaultIndexDepth = iVal;
            }

            if (int.TryParse(UiTbAtMove.Text, out iVal))
            {
                Configuration.AutoThumbnailMoveNo = iVal;
            }

            Configuration.AutoThumbnailColor = _autoThumbnailMoveColor; 

            Configuration.AllowMouseWheelForMoves = (UiCbAllowWheel.IsChecked == true);
            Configuration.MainLineCommentLF = (UiCbMainLineCommentLF.IsChecked == true);
            Configuration.ExtraSpacing = (UiCbExtraSpacing.IsChecked == true);
            Configuration.ShowMovesAtFork = (UiCbShowForkMoves.IsChecked == true);
            Configuration.SoundOn = (UiCbSoundOn.IsChecked == true);
            Configuration.WideScrollbar = (UiCbWideScrollbar.IsChecked == true);
            Configuration.LargeMenuFont = (UiCbLargeMenuFont.IsChecked == true);
            Configuration.UseFigurines = (UiCbFigurines.IsChecked == true);


            MainLineCommentLFChanged = Configuration.MainLineCommentLF != _currentMainLineCommentLF;
            ExtraSpacingChanged = Configuration.ExtraSpacing != _currentExtraSpacing;
            WideScrollbarChanged = Configuration.WideScrollbar != _currentWideScrollbar;
            LargeMenuFontChanged = Configuration.LargeMenuFont != _currentLargeMenuFont;

            if (UiLbLanguages.SelectedItem != null)
            {
                ExitLanguage = (UiLbLanguages.SelectedItem as Language).Code;
            }

            if (ExitLanguage != _currentConfiguredLanguage)
            {
                LanguageChanged = true;
            }

            if (_currentUseFigurines != Configuration.UseFigurines)
            {
                UseFigurinesChanged = true;
            }

            if (_currentEngineThreads != Configuration.EngineThreads
                || _currentEngineHashSize != Configuration.EngineHashSize)
            {
                EngineParamsChanged = true;
            }

            DialogResult = true;
        }


        /// <summary>
        /// Swaps the color of the auto-thumbnail move number.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiLblSwapMoveColor_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _autoThumbnailMoveColor = !_autoThumbnailMoveColor;
            SetAutoThumbnailColorText(_autoThumbnailMoveColor);
        }

        /// <summary>
        /// Main line comment check box is checked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiCbMainLineCommentLF_Checked(object sender, RoutedEventArgs e)
        {
            SetExtraSpacingCheckBox();
        }

        /// <summary>
        /// Main line comment check box is unchecked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiCbMainLineCommentLF_Unchecked(object sender, RoutedEventArgs e)
        {
            SetExtraSpacingCheckBox();
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
            System.Diagnostics.Process.Start("https://github.com/czbar/ChessForge/wiki/Application-Options-Dialog");
        }

        /// <summary>
        /// Invokes the Engine Options dialog.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnEngine_Click(object sender, RoutedEventArgs e)
        {
            AppState.MainWin.ShowEngineOptionsDialog();
        }
    }
}
