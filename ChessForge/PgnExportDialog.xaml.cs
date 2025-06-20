using ChessPosition;
using System.Windows;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for RtfExportDialog.xaml
    /// </summary>
    public partial class PgnExportDialog : Window
    {
        /// <summary>
        /// Scope of export selected by the user.
        /// </summary>
        public static PrintScope Scope;

        /// <summary>
        /// Variables the hold last state of Workbook Items CheckBoxes before they were greyed out
        /// </summary>
        private bool _lastCbContent = false;
        private bool _lastCbGameIndex = false;
        private bool _lastCbExerciseIndex = false;

        /// <summary>
        /// Variables the hold last state of Chapter Items CheckBoxes before they were greyed out
        /// </summary>
        private bool _lastCbIntro = false;
        private bool _lastCbStudy = false;
        private bool _lastCbGames = false;
        private bool _lastCbExercises = false;

        /// <summary>
        /// Variables the hold last state of inclusion CheckBoxes before they were greyed out
        /// </summary>
        private bool _lastCbComments = false;
        private bool _lastCbEvaluations = false;

        /// <summary>
        /// Initializes the data.
        /// </summary>
        public PgnExportDialog()
        {
            InitializeComponent();

            Title = Properties.Resources.ExportPgn;

            Scope = ConfigurationRtfExport.GetScope();

            // check if current view is printable, if not, disable the radio button and change scope if CurrentViewSelected
            TabViewType vt = AppState.ActiveTab;
            if (vt != TabViewType.INTRO && vt != TabViewType.STUDY && vt != TabViewType.MODEL_GAME && vt != TabViewType.EXERCISE)
            {
                UiRbCurrentItem.IsEnabled = false;
                if (Scope == PrintScope.ARTICLE)
                {
                    Scope = PrintScope.CHAPTER;
                }
            }

            SetControlStates();
        }


        /// <summary>
        /// Sets control states based on the current configuration values.
        /// </summary>
        private void SetControlStates()
        {
            bool bVal;

            bVal = ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.INCLUDE_INTRO);
            _lastCbIntro = bVal;
            bVal = ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.INCLUDE_STUDY);
            _lastCbStudy = bVal;
            bVal = ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.INCLUDE_GAMES);
            _lastCbGames = bVal;
            bVal = ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.INCLUDE_EXERCISES);
            _lastCbExercises = bVal;

            EnableChapterItems(true, true);

            bVal = ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.KEEP_COMMENTS);
            _lastCbComments = bVal;
            bVal = ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.KEEP_EVALUATIONS);
            _lastCbEvaluations = bVal;

            UiCbComments.IsChecked = _lastCbComments;
            UiCbEngineEvals.IsChecked = _lastCbEvaluations; 

            // the Scope button must only be set now so that the controls that will be disabled have their values preserved.
            SetScopeButton(Scope);
        }

        /// <summary>
        /// Enable/disable chapter items check boxes.
        /// </summary>
        /// <param name="enabled"></param>
        /// <param name="force"></param>
        private void EnableChapterItems(bool enabled, bool force = false)
        {
            // if already in the expected state exit, unless force == true
            // otherwise the values will get all mixed up
            if (UiGbChapterItems.IsEnabled == enabled && !force)
            {
                return;
            }

            UiGbChapterItems.IsEnabled = enabled;
            if (!enabled)
            {
                _lastCbIntro = UiCbIntro.IsChecked == true;
                _lastCbStudy = UiCbStudy.IsChecked == true;
                _lastCbGames = UiCbGames.IsChecked == true;
                _lastCbExercises = UiCbExercises.IsChecked == true;

                UiCbIntro.IsChecked = false;
                UiCbStudy.IsChecked = false;
                UiCbGames.IsChecked = false;
                UiCbExercises.IsChecked = false;
            }
            else
            {
                UiCbIntro.IsChecked = _lastCbIntro;
                UiCbStudy.IsChecked = _lastCbStudy;
                UiCbGames.IsChecked = _lastCbGames;
                UiCbExercises.IsChecked = _lastCbExercises;
            }

        }

        /// <summary>
        /// Updates configuration per the UI controls' states.
        /// </summary>
        private void SaveConfiguration()
        {
            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.CFG_SCOPE, GetSelectedScopeString());

            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.INCLUDE_CONTENTS, _lastCbContent);
            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.INCLUDE_GAME_INDEX, _lastCbGameIndex);
            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.INCLUDE_EXERCISE_INDEX, _lastCbExerciseIndex);

            if (UiGbChapterItems.IsEnabled)
            {
                _lastCbIntro = UiCbIntro.IsChecked == true;
                _lastCbStudy = UiCbStudy.IsChecked == true;
                _lastCbGames = UiCbGames.IsChecked == true;
                _lastCbExercises = UiCbExercises.IsChecked == true;
            }

            _lastCbComments = UiCbComments.IsChecked == true;
            _lastCbEvaluations = UiCbEngineEvals.IsChecked == true;

            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.INCLUDE_INTRO, _lastCbIntro);
            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.INCLUDE_STUDY, _lastCbStudy);
            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.INCLUDE_GAMES, _lastCbGames);
            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.INCLUDE_EXERCISES, _lastCbExercises);

            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.KEEP_COMMENTS, _lastCbComments);
            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.KEEP_EVALUATIONS, _lastCbEvaluations);

            Configuration.WriteOutConfiguration();
        }

        /// <summary>
        /// Sets the radio button corresponding to the passed value.
        /// </summary>
        /// <param name="sVal"></param>
        private void SetScopeButton(PrintScope scope)
        {
            if (scope == PrintScope.CHAPTER)
            {
                UiRbCurrentChapter.IsChecked = true;
            }
            else if (scope == PrintScope.ARTICLE && UiRbCurrentItem.IsEnabled == true)
            {
                UiRbCurrentItem.IsChecked = true;
            }
            else
            {
                UiRbWorkbook.IsChecked = true;
            }
        }

        /// <summary>
        /// Returns a string representing the current state
        /// of scope radio buttons.
        /// </summary>
        /// <returns></returns>
        private string GetSelectedScopeString()
        {
            string scopeString = ConfigurationRtfExport.WorkbookScopeCoded;

            if (UiRbCurrentChapter.IsChecked == true)
            {
                scopeString = ConfigurationRtfExport.ChapterScopeCoded;
            }
            else if (UiRbCurrentItem.IsChecked == true)
            {
                scopeString = ConfigurationRtfExport.ArticleScopeCoded;
            }

            return scopeString;
        }

        /// <summary>
        /// Proceed with the export as configured.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnOk_Click(object sender, RoutedEventArgs e)
        {
            SaveConfiguration();
            DialogResult = true;
        }

        /// <summary>
        /// Scope radio button Workbook was clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiRbWorkbook_Checked(object sender, RoutedEventArgs e)
        {
            EnableChapterItems(true);
        }

        /// <summary>
        /// Scope radio button Chapter was clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiRbCurrentChapter_Checked(object sender, RoutedEventArgs e)
        {
            EnableChapterItems(true);
        }

        /// <summary>
        /// Scope radio button Current View was clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiRbCurrentItem_Checked(object sender, RoutedEventArgs e)
        {
            EnableChapterItems(false);
        }

    }
}
