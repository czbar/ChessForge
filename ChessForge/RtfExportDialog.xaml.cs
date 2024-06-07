using System.Windows;
using System.Windows.Input;

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
        /// Chapter to print if scope is chapter.
        /// </summary>
        public Chapter Chapter;

        /// <summary>
        /// Article to print, if scope is Article.
        /// </summary>
        public Article Article;

        /// <summary>
        /// Initializes the data.
        /// </summary>
        public RtfExportDialog()
        {
            InitializeComponent();

            string scope = ConfigurationRtfExport.GetStringValue(ConfigurationRtfExport.CFG_SCOPE);
            Scope = GetScopeFromString(scope);

            switch (Scope)
            {
                case PrintScope.WORKBOOK:
                    ConfigurationRtfExport.SetValue(ConfigurationRtfExport.CFG_SCOPE, ConfigurationRtfExport.WorkbookScopeCoded);
                    break;
                case PrintScope.CHAPTER:
                    ConfigurationRtfExport.SetValue(ConfigurationRtfExport.CFG_SCOPE, ConfigurationRtfExport.ChapterScopeCoded);
                    break;
                case PrintScope.ARTICLE:
                    ConfigurationRtfExport.SetValue(ConfigurationRtfExport.CFG_SCOPE, ConfigurationRtfExport.ArticleScopeCoded);
                    break;
            }

            SetControlStates();
        }

        /// <summary>
        /// Sets control states based on the current configuration values.
        /// </summary>
        private void SetControlStates()
        {
            string sVal;
            bool bVal;

            sVal = ConfigurationRtfExport.GetStringValue(ConfigurationRtfExport.CFG_SCOPE);
            SetScopeButton(sVal);

            bVal = ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.INCLUDE_CONTENTS);
            UiCbContents.IsChecked = bVal;
            bVal = ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.INCLUDE_GAME_INDEX);
            UiCbGameIndex.IsChecked = bVal;
            bVal = ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.INCLUDE_EXERCISE_INDEX);
            UiCbExerciseIndex.IsChecked = bVal;

            bVal = ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.INCLUDE_INTRO);
            UiCbIntro.IsChecked = bVal;
            bVal = ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.INCLUDE_STUDY);
            UiCbStudy.IsChecked = bVal;
            bVal = ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.INCLUDE_GAMES);
            UiCbGames.IsChecked = bVal;
            bVal = ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.INCLUDE_EXERCISES);
            UiCbExercises.IsChecked = bVal;

            bVal = ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.TWO_COLUMN_INTRO);
            UiCbIntro2Col.IsChecked = bVal;
            bVal = ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.TWO_COLUMN_STUDY);
            UiCbStudy2Col.IsChecked = bVal;
            bVal = ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.TWO_COLUMN_GAMES);
            UiCbGames2Col.IsChecked = bVal;
            bVal = ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.TWO_COLUMN_EXERCISES);
            UiCbExercises2Col.IsChecked = bVal;

            bVal = ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.USE_CUSTOM_STUDY);
            UiCbStudyCustom.IsChecked = bVal;
            bVal = ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.USE_CUSTOM_GAMES);
            UiCbGamesCustom.IsChecked = bVal;
            bVal = ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.USE_CUSTOM_GAME);
            UiCbGameCustom.IsChecked = bVal;
            bVal = ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.USE_CUSTOM_EXERCISES);
            UiCbExercisesCustom.IsChecked = bVal;
            bVal = ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.USE_CUSTOM_EXERCISE);
            UiCbExerciseCustom.IsChecked = bVal;

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
        }

        /// <summary>
        /// Updates configuration per the UI controls' states.
        /// </summary>
        private void SaveConfiguration()
        {
            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.CFG_SCOPE, GetSelectedScopeString());
            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.INCLUDE_CONTENTS, UiCbContents.IsChecked == true);
            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.INCLUDE_GAME_INDEX, UiCbGameIndex.IsChecked == true);

            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.INCLUDE_EXERCISE_INDEX, UiCbExerciseIndex.IsChecked == true);
            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.INCLUDE_INTRO, UiCbIntro.IsChecked == true);
            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.INCLUDE_STUDY, UiCbStudy.IsChecked == true);
            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.INCLUDE_GAMES, UiCbGames.IsChecked == true);
            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.INCLUDE_EXERCISES, UiCbExercises.IsChecked == true);

            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.TWO_COLUMN_INTRO, UiCbIntro2Col.IsChecked == true);
            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.TWO_COLUMN_STUDY, UiCbStudy2Col.IsChecked == true);
            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.TWO_COLUMN_GAMES, UiCbGames2Col.IsChecked == true);
            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.TWO_COLUMN_EXERCISES, UiCbExercises2Col.IsChecked == true);

            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.USE_CUSTOM_STUDY, UiCbStudyCustom.IsChecked == true);
            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.USE_CUSTOM_GAMES, UiCbStudyCustom.IsChecked == true);
            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.USE_CUSTOM_GAME, UiCbGameCustom.IsChecked == true);
            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.USE_CUSTOM_EXERCISES, UiCbExercisesCustom.IsChecked == true);
            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.USE_CUSTOM_EXERCISE, UiCbExerciseCustom.IsChecked == true);

            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.CUSTOM_TERM_STUDY, UiTbStudyCustom.Text);
            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.CUSTOM_TERM_GAMES, UiTbGamesCustom.Text);
            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.CUSTOM_TERM_GAME, UiTbGameCustom.Text);
            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.CUSTOM_TERM_EXERCISES, UiTbExercisesCustom.Text);
            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.CUSTOM_TERM_EXERCISE, UiTbExerciseCustom.Text);
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
        /// Converts encoded scope into PrintScope enum.
        /// </summary>
        /// <param name="scopeString"></param>
        /// <returns></returns>
        private PrintScope GetScopeFromString(string scopeString)
        {
            PrintScope scope = PrintScope.WORKBOOK;
            
            if (scopeString == ConfigurationRtfExport.ChapterScopeCoded)
            {
                scope = PrintScope.CHAPTER;
            }
            else if (scopeString == ConfigurationRtfExport.ArticleScopeCoded)
            {
                scope = PrintScope.ARTICLE;
            }

            return scope;
        }

        /// <summary>
        /// Proceed with the export as configured.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnOk_Click(object sender, RoutedEventArgs e)
        {
            Chapter = AppState.ActiveChapter;
            Article = null;

            SaveConfiguration();
            DialogResult = true;
        }

    }
}
