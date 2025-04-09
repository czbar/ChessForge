using ChessPosition;
using GameTree;
using System.Windows;
using System.Windows.Controls;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for OperationScopeDialog.xaml
    /// </summary>
    public partial class OperationScopeDialog : Window
    {
        /// <summary>
        /// Type of action that is being scoped so the correct GUI behavior 
        /// can be implemented.
        /// </summary>
        public enum ScopedAction
        {
            DEFAULT,
            ASSIGN_ECO,
            SAVE_DIAGRAM
        }

        /// <summary>
        /// Type of the action being scoped in this session.
        /// </summary>
        public ScopedAction Action;

        /// <summary>
        /// The types of selected in this dialog.
        /// </summary>
        public ViewTypeScope ApplicableViews = ViewTypeScope.NONE;

        /// <summary>
        /// The scope selected by the user.
        /// </summary>
        public OperationScope ApplyScope { get; set; }

        // flags change in selection logic after the first radio button check
        private bool firstTimeCheck = true;

        /// <summary>
        /// Whether Study should be shown at all
        /// </summary>
        private bool _allowStudies
        {
            get => Action != ScopedAction.ASSIGN_ECO;
        }

        /// <summary>
        /// Whether Intros should be shown at all
        /// </summary>
        private bool _allowIntros
        {
            get => Action == ScopedAction.SAVE_DIAGRAM;
        }

        /// <summary>
        /// Constructor. Sets the title of the dialog
        /// and checks the controls as per the current state
        /// of the application.
        /// </summary>
        /// <param name="title"></param>
        public OperationScopeDialog(string title, ScopedAction action)
        {
            Action = action;
            InitializeComponent();

            this.Title = title;

            if (AppState.ActiveTab == TabViewType.INTRO  || 
                (AppState.IsTreeViewTabActive() && AppState.ActiveVariationTree != null))
            {
                UiRbCurrentItem.IsChecked = true;
            }
            else
            {
                UiRbCurrentItem.IsEnabled = false;
                UiRbCurrentChapter.IsChecked = true;
            }

            ApplyScope = OperationScope.NONE;
        }

        /// <summary>
        /// Ensures that there is only one check box check,
        /// the one that corresponds to the passed itemType.
        /// </summary>
        /// <param name="itemType"></param>
        private void CheckSingleItemType(GameData.ContentType itemType, bool enabled)
        {
            if (!_allowStudies && itemType == GameData.ContentType.STUDY_TREE
                || !_allowIntros && itemType == GameData.ContentType.INTRO)
            {
                return;
            }

            CheckAllItemTypes(false);

            CheckBox cb = null;
            switch (itemType)
            {
                case GameData.ContentType.INTRO:
                    cb = UiCbIntro;
                    break;
                case GameData.ContentType.STUDY_TREE:
                    cb = UiCbStudy;
                    break;
                case GameData.ContentType.MODEL_GAME:
                    cb = UiCbGames;
                    break;
                case GameData.ContentType.EXERCISE:
                    cb = UiCbExercises;
                    break;
            }

            if (cb != null)
            {
                cb.IsChecked = true;
                cb.IsEnabled = enabled;
            }
        }

        /// <summary>
        /// Enables all itmes, shows or hides them,
        /// sets singular or plural labels for games/exercises.
        /// </summary>
        /// <param name="enable"></param>
        /// <param name="plural"></param>
        private void EnableAllItemTypes(bool enable, bool plural)
        {
            UiCbIntro.IsEnabled = enable && _allowIntros;
            UiCbStudy.IsEnabled = enable && _allowStudies;
            UiCbGames.IsEnabled = enable ;
            UiCbExercises.IsEnabled = enable;

            UiCbGames.Content = plural ? Properties.Resources.Games : Properties.Resources.Game;
            UiCbExercises.Content = plural ? Properties.Resources.Exercises : Properties.Resources.Exercise;
        }

        /// <summary>
        /// Checks or unchecks all check boxes.
        /// </summary>
        /// <param name="isChecked"></param>
        private void CheckAllItemTypes(bool isChecked)
        {
            UiCbIntro.IsChecked = isChecked && _allowIntros;
            UiCbStudy.IsChecked = isChecked && _allowStudies;
            UiCbGames.IsChecked = isChecked;
            UiCbExercises.IsChecked = isChecked;
        }


        //####################################################
        //
        // Radio button checked
        //
        //####################################################

        /// <summary>
        /// "Current View" radio button selected.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiRbCurrentItem_Checked(object sender, RoutedEventArgs e)
        {
            UiLblScopeInfo.Content = Properties.Resources.OperationScope;

            if (AppState.MainWin.ActiveVariationTree == null)
            {
                CheckSingleItemType(GameData.ContentType.NONE, false);
                EnableAllItemTypes(false, true);
            }
            else
            {
                EnableAllItemTypes(false, false);
                CheckSingleItemType(AppState.MainWin.ActiveVariationTree.ContentType, false);
            }

            if (_allowIntros)
            {
                UiCbIntro.Content = Properties.Resources.Intro;
            }
            if (_allowStudies)
            {
                UiCbStudy.Content = Properties.Resources.Study;
            }
        }

        /// <summary>
        /// "Current Chapter" radio button selected.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiRbCurrentChapter_Checked(object sender, RoutedEventArgs e)
        {
            EnableAllItemTypes(true, true);

            if (_allowIntros)
            {
                UiCbIntro.Content = Properties.Resources.Intro;
            }
            if (_allowStudies)
            {
                UiCbStudy.Content = Properties.Resources.Study;
            }

            if (firstTimeCheck)
            {
                CheckAllItemTypes(true);
                firstTimeCheck = false;
            }

            Chapter chapter = AppState.ActiveChapter;
            if (chapter != null)
            {
                UiLblScopeInfo.Content = Properties.Resources.Chapter + ": " + chapter.GetTitle();
            }
        }

        /// <summary>
        /// "Entire Workbook" radio button selected.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiRbWorkbook_Checked(object sender, RoutedEventArgs e)
        {
            UiLblScopeInfo.Content = Properties.Resources.OperationScope;
            EnableAllItemTypes(true, true);
            if (_allowStudies)
            {
                UiCbStudy.Content = (AppState.Workbook != null && AppState.Workbook.GetChapterCount() > 1) ? Properties.Resources.Studies : Properties.Resources.Study;
            }
            if (_allowIntros)
            {
                UiCbIntro.Content = (AppState.Workbook != null && AppState.Workbook.GetChapterCount() > 1) ? Properties.Resources.Intros : Properties.Resources.Intro;
            }

            if (firstTimeCheck)
            {
                CheckAllItemTypes(true);
                firstTimeCheck = false;
            }
        }

        //####################################################
        //
        // Exit
        //
        //####################################################

        /// <summary>
        /// Collects the controls states and converts them to scope. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnOk_Click(object sender, RoutedEventArgs e)
        {
            if (UiRbCurrentItem.IsChecked == true)
            {
                ApplyScope = OperationScope.ACTIVE_ITEM;
            }
            else if (UiRbCurrentChapter.IsChecked == true)
            {
                ApplyScope = OperationScope.CHAPTER;
            }
            else if (UiRbWorkbook.IsChecked == true)
            {
                ApplyScope = OperationScope.WORKBOOK;
            }

            if (UiCbIntro.IsChecked == true)
            {
                ApplicableViews |= ViewTypeScope.INTRO;
            }
            if (UiCbStudy.IsChecked == true)
            {
                ApplicableViews |= ViewTypeScope.STUDY;
            }
            if (UiCbGames.IsChecked == true)
            {
                ApplicableViews |= ViewTypeScope.MODEL_GAMES;
            }
            if (UiCbExercises.IsChecked == true)
            {
                ApplicableViews |= ViewTypeScope.EXERCISES;
            }

            DialogResult = true;
        }
    }
}
