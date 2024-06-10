using System.Windows;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for RtfExportDialog.xaml
    /// </summary>
    public partial class RtfExportDialog : Window
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
        /// Initializes the data.
        /// </summary>
        public RtfExportDialog()
        {
            InitializeComponent();

            Scope = ConfigurationRtfExport.GetScope();
            SetControlStates();
        }


        /// <summary>
        /// Sets control states based on the current configuration values.
        /// </summary>
        private void SetControlStates()
        {
            string sVal;
            bool bVal;

            bVal = ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.INCLUDE_CONTENTS);
            _lastCbContent = bVal;
            bVal = ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.INCLUDE_GAME_INDEX);
            _lastCbGameIndex = bVal;
            bVal = ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.INCLUDE_EXERCISE_INDEX);
            _lastCbExerciseIndex = bVal;
            EnableWorkbookItems(true, true);

            bVal = ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.INCLUDE_INTRO);
            _lastCbIntro = bVal;
            bVal = ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.INCLUDE_STUDY);
            _lastCbStudy = bVal;
            bVal = ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.INCLUDE_GAMES);
            _lastCbGames = bVal;
            bVal = ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.INCLUDE_EXERCISES);
            _lastCbExercises = bVal;
            EnableChapterItems(true, true);

            bVal = ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.TWO_COLUMN_INTRO);
            UiCbIntro2Col.IsChecked = bVal;
            bVal = ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.TWO_COLUMN_STUDY);
            UiCbStudy2Col.IsChecked = bVal;
            bVal = ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.TWO_COLUMN_GAMES);
            UiCbGames2Col.IsChecked = bVal;
            bVal = ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.TWO_COLUMN_EXERCISES);
            UiCbExercises2Col.IsChecked = bVal;

            sVal = ConfigurationRtfExport.GetStringValue(ConfigurationRtfExport.CUSTOM_TERM_STUDY);
            UiTbStudyCustom.Text = sVal;
            sVal = ConfigurationRtfExport.GetStringValue(ConfigurationRtfExport.CUSTOM_TERM_GAMES);
            UiTbGamesCustom.Text = sVal;
            sVal = ConfigurationRtfExport.GetStringValue(ConfigurationRtfExport.CUSTOM_TERM_GAME);
            UiTbGameCustom.Text = sVal;
            sVal = ConfigurationRtfExport.GetStringValue(ConfigurationRtfExport.CUSTOM_TERM_EXERCISES);
            UiTbExercisesCustom.Text = sVal;
            sVal = ConfigurationRtfExport.GetStringValue(ConfigurationRtfExport.CUSTOM_TERM_EXERCISE);
            UiTbExerciseCustom.Text = sVal;

            bVal = ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.USE_CUSTOM_STUDY);
            if (bVal)
                UiCbStudyCustom_Checked(null, null);
            else
                UiCbStudyCustom_Unchecked(null, null);

            bVal = ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.USE_CUSTOM_GAMES);
            if (bVal)
                UiCbGamesCustom_Checked(null, null);
            else
                UiCbGamesCustom_Unchecked(null, null);
            
            bVal = ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.USE_CUSTOM_GAME);
            if (bVal)
                UiCbGameCustom_Checked(null, null);
            else
                UiCbGameCustom_Unchecked(null, null);

            bVal = ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.USE_CUSTOM_EXERCISES);
            if (bVal)
                UiCbExercisesCustom_Checked(null, null);
            else
                UiCbExercisesCustom_Unchecked(null, null);

            bVal = ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.USE_CUSTOM_EXERCISE);
            if (bVal)
                UiCbExerciseCustom_Checked(null, null);
            else
                UiCbExerciseCustom_Unchecked(null, null);

            // the Scope button must only be set now so that the controls that will be disabled have their values preserved.
            sVal = ConfigurationRtfExport.GetStringValue(ConfigurationRtfExport.CFG_SCOPE);
            SetScopeButton(sVal);
        }

        /// <summary>
        /// Enable/disable workbook items check boxes.
        /// </summary>
        /// <param name="enabled"></param>
        /// <param name="force"></param>
        private void EnableWorkbookItems(bool enabled, bool force = false)
        {
            // if already in the expected state exit, unless force == true
            // otherwise the values will get all mixed up
            if (UiGbWorkbookItems.IsEnabled == enabled && !force)
            {
                return;
            }

            UiGbWorkbookItems.IsEnabled = enabled;
            if (!enabled)
            {
                _lastCbContent = UiCbContents.IsChecked == true;
                _lastCbGameIndex = UiCbGameIndex.IsChecked == true;
                _lastCbExerciseIndex = UiCbExerciseIndex.IsChecked == true;

                UiCbContents.IsChecked = false;
                UiCbGameIndex.IsChecked = false;
                UiCbExerciseIndex.IsChecked = false;
            }
            else
            {
                UiCbContents.IsChecked = _lastCbContent;
                UiCbGameIndex.IsChecked = _lastCbGameIndex;
                UiCbExerciseIndex.IsChecked = _lastCbExerciseIndex;
            }
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

            if (UiGbWorkbookItems.IsEnabled)
            {
                _lastCbContent = UiCbContents.IsChecked == true;
                _lastCbGameIndex = UiCbGameIndex.IsChecked == true;
                _lastCbExerciseIndex = UiCbExerciseIndex.IsChecked == true;
            }
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
            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.INCLUDE_INTRO, _lastCbIntro);
            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.INCLUDE_STUDY, _lastCbStudy);
            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.INCLUDE_GAMES, _lastCbGames);
            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.INCLUDE_EXERCISES, _lastCbExercises);

            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.TWO_COLUMN_INTRO, UiCbIntro2Col.IsChecked == true);
            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.TWO_COLUMN_STUDY, UiCbStudy2Col.IsChecked == true);
            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.TWO_COLUMN_GAMES, UiCbGames2Col.IsChecked == true);
            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.TWO_COLUMN_EXERCISES, UiCbExercises2Col.IsChecked == true);

            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.USE_CUSTOM_STUDY, UiCbStudyCustom.IsChecked == true);
            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.USE_CUSTOM_GAMES, UiCbGamesCustom.IsChecked == true);
            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.USE_CUSTOM_GAME, UiCbGameCustom.IsChecked == true);
            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.USE_CUSTOM_EXERCISES, UiCbExercisesCustom.IsChecked == true);
            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.USE_CUSTOM_EXERCISE, UiCbExerciseCustom.IsChecked == true);

            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.CUSTOM_TERM_STUDY, UiTbStudyCustom.Text);
            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.CUSTOM_TERM_GAMES, UiTbGamesCustom.Text);
            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.CUSTOM_TERM_GAME, UiTbGameCustom.Text);
            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.CUSTOM_TERM_EXERCISES, UiTbExercisesCustom.Text);
            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.CUSTOM_TERM_EXERCISE, UiTbExerciseCustom.Text);

            Configuration.WriteOutConfiguration();
        }

        /// <summary>
        /// Sets the radio button corresponding to the passed value.
        /// </summary>
        /// <param name="sVal"></param>
        private void SetScopeButton(string sVal)
        {
            if (sVal == ConfigurationRtfExport.ChapterScopeCoded)
            {
                UiRbCurrentChapter.IsChecked = true;
            }
            else if (sVal == ConfigurationRtfExport.ArticleScopeCoded)
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
            EnableWorkbookItems(true);
            EnableChapterItems(true);
        }

        /// <summary>
        /// Scope radio button Chapter was clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiRbCurrentChapter_Checked(object sender, RoutedEventArgs e)
        {
            EnableWorkbookItems(false);
            EnableChapterItems(true);
        }

        /// <summary>
        /// Scope radio button Current View was clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiRbCurrentItem_Checked(object sender, RoutedEventArgs e)
        {
            if (AppState.ActiveTab != ChessPosition.TabViewType.CHAPTERS)
            {
                EnableWorkbookItems(false);
            }
            else
            {
                EnableWorkbookItems(true);
            }

            EnableChapterItems(false);
        }

        /// <summary>
        /// Make the Main TextBox visible and the Dummy hidden.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiCbStudyCustom_Checked(object sender, RoutedEventArgs e)
        {
            UiTbStudyCustom.Visibility = Visibility.Visible;
            UiTbStudyCustomDummy.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// Make the Main TextBox hidden and the Dummy visible.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiCbStudyCustom_Unchecked(object sender, RoutedEventArgs e)
        {
            UiTbStudyCustom.Visibility = Visibility.Hidden;
            UiTbStudyCustomDummy.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Make the Main TextBox visible and the Dummy hidden.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiCbGamesCustom_Checked(object sender, RoutedEventArgs e)
        {
            UiTbGamesCustom.Visibility = Visibility.Visible;
            UiTbGamesCustomDummy.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// Make the Main TextBox hidden and the Dummy visible.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiCbGamesCustom_Unchecked(object sender, RoutedEventArgs e)
        {
            UiTbGamesCustom.Visibility = Visibility.Hidden;
            UiTbGamesCustomDummy.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Make the Main TextBox visible and the Dummy hidden.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiCbGameCustom_Checked(object sender, RoutedEventArgs e)
        {
            UiTbGameCustom.Visibility = Visibility.Visible;
            UiTbGameCustomDummy.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// Make the Main TextBox hidden and the Dummy visible.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiCbGameCustom_Unchecked(object sender, RoutedEventArgs e)
        {
            UiTbGameCustom.Visibility = Visibility.Hidden;
            UiTbGameCustomDummy.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Make the Main TextBox visible and the Dummy hidden.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiCbExercisesCustom_Checked(object sender, RoutedEventArgs e)
        {
            UiTbExercisesCustom.Visibility = Visibility.Visible;
            UiTbExercisesCustomDummy.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// Make the Main TextBox hidden and the Dummy visible.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiCbExercisesCustom_Unchecked(object sender, RoutedEventArgs e)
        {
            UiTbExercisesCustom.Visibility = Visibility.Hidden;
            UiTbExercisesCustomDummy.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Make the Main TextBox visible and the Dummy hidden.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiCbExerciseCustom_Checked(object sender, RoutedEventArgs e)
        {
            UiTbExerciseCustom.Visibility = Visibility.Visible;
            UiTbExerciseCustomDummy.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// Make the Main TextBox hidden and the Dummy visible.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiCbExerciseCustom_Unchecked(object sender, RoutedEventArgs e)
        {
            UiTbExerciseCustom.Visibility = Visibility.Hidden;
            UiTbExerciseCustomDummy.Visibility = Visibility.Visible;
        }
    }
}
