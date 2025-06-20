﻿using ChessPosition;
using System.Windows;
using System.Windows.Media;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for RtfExportDialog.xaml
    /// </summary>
    public partial class RtfExportDialog : Window
    {
        /// <summary>
        /// Export format selected by the user.
        /// </summary>
        public enum ExportFormat
        {
            RTF,
            TEXT
        }

        /// <summary>
        /// 
        /// </summary>
        ExportFormat _exportFormat;

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
        private bool _lastCbBookmarks = false;

        /// <summary>
        /// Initializes the data.
        /// </summary>
        public RtfExportDialog(ExportFormat exportFormat)
        {
            _exportFormat = exportFormat;

            InitializeComponent();

            if (_exportFormat == ExportFormat.RTF)
            {
                Title = Properties.Resources.ExportRtf;
            }
            else
            {
                Title = Properties.Resources.ExportText;

                UiGbColumnFormats.IsEnabled = false;
                UiCbIntro2Col.IsChecked = false;
                UiCbStudy2Col.IsChecked = false;
                UiCbGames2Col.IsChecked = false;
                UiCbExercises2Col.IsChecked = false;
                UiCbBookmarks2Col.IsChecked = false;

                UiGbColumnFormats.Foreground = Brushes.LightGray;
                UiCbIntro2Col.Foreground = Brushes.LightGray;
                UiCbStudy2Col.Foreground = Brushes.LightGray;
                UiCbGames2Col.Foreground = Brushes.LightGray;
                UiCbExercises2Col.Foreground = Brushes.LightGray;
                UiCbBookmarks2Col.Foreground = Brushes.LightGray;

                UiCbFens.IsChecked = true;
                UiCbFens.IsEnabled = false;
                UiCbFens.Foreground = Brushes.LightGray;
            }

            Scope = ConfigurationRtfExport.GetScope();

            // check if current view is printable, if not, disable the radio button and change scope if CurrentViewSelected
            TabViewType vt = AppState.ActiveTab;
            if (vt != TabViewType.CHAPTERS && vt != TabViewType.INTRO && vt != TabViewType.STUDY 
                && vt != TabViewType.MODEL_GAME && vt != TabViewType.EXERCISE && vt != TabViewType.BOOKMARKS)
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
            bVal = ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.INCLUDE_BOOKMARKS);
            _lastCbBookmarks = bVal;
            EnableChapterItems(true, true);

            if (_exportFormat == ExportFormat.RTF)
            {
                bVal = ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.TWO_COLUMN_INTRO);
                UiCbIntro2Col.IsChecked = bVal;
                bVal = ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.TWO_COLUMN_STUDY);
                UiCbStudy2Col.IsChecked = bVal;
                bVal = ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.TWO_COLUMN_GAMES);
                UiCbGames2Col.IsChecked = bVal;
                bVal = ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.TWO_COLUMN_EXERCISES);
                UiCbExercises2Col.IsChecked = bVal;
                bVal = ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.TWO_COLUMN_BOOKMARKS);
                UiCbBookmarks2Col.IsChecked = bVal;

                bVal = ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.FEN_UNDER_DIAGRAMS);
                UiCbFens.IsChecked = bVal;
            }

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
            sVal = ConfigurationRtfExport.GetStringValue(ConfigurationRtfExport.CUSTOM_TERM_BOOKMARKS);
            UiTbBookmarksCustom.Text = sVal;

            UiCbStudyCustom.IsChecked = null;
            UiCbGamesCustom.IsChecked = null;
            UiCbGameCustom.IsChecked = null;
            UiCbExercisesCustom.IsChecked = null;
            UiCbExerciseCustom.IsChecked = null;
            UiCbBookmarksCustom.IsChecked = null;

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

            bVal = ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.USE_CUSTOM_BOOKMARKS);
            UiCbBookmarksCustom.IsChecked = bVal;

            // the Scope button must only be set now so that the controls that will be disabled have their values preserved.
            SetScopeButton(Scope);
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
                _lastCbBookmarks = UiCbBookmarks.IsChecked == true;

                UiCbIntro.IsChecked = false;
                UiCbStudy.IsChecked = false;
                UiCbGames.IsChecked = false;
                UiCbExercises.IsChecked = false;
                UiCbBookmarks.IsChecked = false;
            }
            else
            {
                UiCbIntro.IsChecked = _lastCbIntro;
                UiCbStudy.IsChecked = _lastCbStudy;
                UiCbGames.IsChecked = _lastCbGames;
                UiCbExercises.IsChecked = _lastCbExercises;
                UiCbBookmarks.IsChecked = _lastCbBookmarks;
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
                _lastCbBookmarks = UiCbBookmarks.IsChecked == true;
            }
            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.INCLUDE_INTRO, _lastCbIntro);
            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.INCLUDE_STUDY, _lastCbStudy);
            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.INCLUDE_GAMES, _lastCbGames);
            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.INCLUDE_EXERCISES, _lastCbExercises);
            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.INCLUDE_BOOKMARKS, _lastCbBookmarks);

            if (_exportFormat == ExportFormat.RTF)
            {
                ConfigurationRtfExport.SetValue(ConfigurationRtfExport.TWO_COLUMN_INTRO, UiCbIntro2Col.IsChecked == true);
                ConfigurationRtfExport.SetValue(ConfigurationRtfExport.TWO_COLUMN_STUDY, UiCbStudy2Col.IsChecked == true);
                ConfigurationRtfExport.SetValue(ConfigurationRtfExport.TWO_COLUMN_GAMES, UiCbGames2Col.IsChecked == true);
                ConfigurationRtfExport.SetValue(ConfigurationRtfExport.TWO_COLUMN_EXERCISES, UiCbExercises2Col.IsChecked == true);
                ConfigurationRtfExport.SetValue(ConfigurationRtfExport.TWO_COLUMN_BOOKMARKS, UiCbBookmarks2Col.IsChecked == true);

                ConfigurationRtfExport.SetValue(ConfigurationRtfExport.FEN_UNDER_DIAGRAMS, UiCbFens.IsChecked == true);
            }

            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.USE_CUSTOM_STUDY, UiCbStudyCustom.IsChecked == true);
            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.USE_CUSTOM_GAMES, UiCbGamesCustom.IsChecked == true);
            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.USE_CUSTOM_GAME, UiCbGameCustom.IsChecked == true);
            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.USE_CUSTOM_EXERCISES, UiCbExercisesCustom.IsChecked == true);
            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.USE_CUSTOM_EXERCISE, UiCbExerciseCustom.IsChecked == true);
            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.USE_CUSTOM_BOOKMARKS, UiCbBookmarksCustom.IsChecked == true);

            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.CUSTOM_TERM_STUDY, UiTbStudyCustom.Text);
            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.CUSTOM_TERM_GAMES, UiTbGamesCustom.Text);
            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.CUSTOM_TERM_GAME, UiTbGameCustom.Text);
            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.CUSTOM_TERM_EXERCISES, UiTbExercisesCustom.Text);
            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.CUSTOM_TERM_EXERCISE, UiTbExerciseCustom.Text);
            ConfigurationRtfExport.SetValue(ConfigurationRtfExport.CUSTOM_TERM_BOOKMARKS, UiTbBookmarksCustom.Text);

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
            EnableWorkbookItems(false);
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

        /// <summary>
        /// Make the Main TextBox visible and the Dummy hidden.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiCbBookmarksCustom_Checked(object sender, RoutedEventArgs e)
        {
            UiTbBookmarksCustom.Visibility = Visibility.Visible;
            UiTbBookmarksCustomDummy.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// Make the Main TextBox hidden and the Dummy visible.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiCbBookmarksCustom_Unchecked(object sender, RoutedEventArgs e)
        {
            UiTbBookmarksCustom.Visibility = Visibility.Hidden;
            UiTbBookmarksCustomDummy.Visibility = Visibility.Visible;
        }
    }
}
