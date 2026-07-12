using ChessPosition;
using GameTree;
using System.Windows;
using System.Windows.Controls;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for DeleteNotesDialog.xaml
    /// </summary>
    public partial class CleanSidelinesCommentsDialog : Window
    {
        /// <summary>
        /// Set of move attribute flags to be set on exit.
        /// </summary>
        public int MoveAttrsFlags { get; set; }

        /// <summary>
        /// Set of article attribute flags to be set on exit.
        /// </summary>
        public int ArticleAttrsFlags { get; set; }

        /// <summary>
        /// The scope selected by the user.
        /// </summary>
        public OperationScope Scope { get; set; }

        /// <summary>
        /// Whether to apply the operation to Study/Studies.
        /// </summary>
        public bool ApplyToStudies { get; set; }

        /// <summary>
        /// Whether to apply the operation to Model Games.
        /// </summary>
        public bool ApplyToGames { get; set; }

        /// <summary>
        /// Whether to apply the operation to Exercises.
        /// </summary>
        public bool ApplyToExercises { get; set; }

        // flags change in selection logic after the first radio button check
        private bool firstTimeCheck = true;

        /// <summary>
        /// Constructor. Sets the title of the dialog
        /// and checks the controls as per the current state
        /// of the application.
        /// </summary>
        public CleanSidelinesCommentsDialog()
        {
            InitializeComponent();
            UiCbAnnotator.Content = Properties.Resources.Annotator + " / " + Properties.Resources.Author;
            UiCbStudy.Visibility = Visibility.Visible;

            // Set the "Apply to" check boxes according to the current configuration
            UiCbStudy.IsChecked = Configuration.CleanupApplyToStudies;
            UiCbGames.IsChecked = Configuration.CleanupApplyToGames;
            UiCbExercises.IsChecked = Configuration.CleanupApplyToExercises;


            // Set the radio buttons according to the current configuration
            Scope = Configuration.CleanupScope;

            // if there is no active variation tree, then the "Current Item" option is not available.
            if (AppState.MainWin.ActiveVariationTree == null)
            {
                UiRbCurrentItem.IsChecked = false;

                // check the "Current Chapter" radio button if the scope is not "Workbook"
                UiRbWorkbook.IsChecked = (Scope == OperationScope.WORKBOOK);
                if (Scope != OperationScope.WORKBOOK)
                {
                    UiRbCurrentChapter.IsChecked = true;
                }
            }
            else
            {
                // check the "Current Item" radio button if the scope is not "Chapter" or "Workbook"
                UiRbWorkbook.IsChecked = (Scope == OperationScope.WORKBOOK);
                if (Scope != OperationScope.WORKBOOK)
                {
                    UiRbCurrentChapter.IsChecked = (Scope == OperationScope.CHAPTER);
                }
                if (Scope != OperationScope.WORKBOOK && Scope != OperationScope.CHAPTER)
                {
                    UiRbCurrentItem.IsChecked = true;
                }
            }

            // Set the "Attributes" check boxes according to the current configuration
            int moveAttrsFlags = Configuration.CleanupMoveAttrs;
            int articleAttrsFlags = Configuration.CleanupArticleAttrs;

            UiCbComments.IsChecked = (moveAttrsFlags & (int)MoveAttribute.COMMENT_AND_NAGS) != 0;
            UiCbEngineEvals.IsChecked = (moveAttrsFlags & (int)MoveAttribute.ENGINE_EVALUATION) != 0;
            UiCbBadMoveDetection.IsChecked = (moveAttrsFlags & (int)MoveAttribute.BAD_MOVE_ASSESSMENT) != 0;
            UiCbSideLines.IsChecked = (moveAttrsFlags & (int)MoveAttribute.SIDELINE) != 0;
            UiCbReferences.IsChecked = (moveAttrsFlags & (int)MoveAttribute.REFERENCE) != 0;
            UiCbDiagrams.IsChecked = (moveAttrsFlags & (int)MoveAttribute.DIAGRAM) != 0;

            UiCbAnnotator.IsChecked = (articleAttrsFlags & (int)ArticleAttribute.ANNOTATOR) != 0;
        }

        /// <summary>
        /// Ensures that there is only one check box check,
        /// the one that corresponds to the passed itemType.
        /// </summary>
        /// <param name="itemType"></param>
        private void ShowItemType(GameData.ContentType itemType, bool enabled)
        {
            CheckBox cb = null;
            switch (itemType)
            {
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
                cb.Visibility = Visibility.Visible;
                cb.IsEnabled = enabled ? true : false;
            }
        }

        /// <summary>
        /// Enables all itmes, shows or hides them,
        /// sets singular or plural labels for games/exercises.
        /// </summary>
        /// <param name="showHide"></param>
        /// <param name="plural"></param>
        private void ShowEnableAllItemTypes(bool showHide, bool plural)
        {
            UiCbStudy.IsEnabled = true;
            UiCbGames.IsEnabled = true;
            UiCbExercises.IsEnabled = true;

            UiCbStudy.Visibility = showHide ? Visibility.Visible : Visibility.Hidden;
            UiCbGames.Visibility = showHide ? Visibility.Visible : Visibility.Hidden;
            UiCbExercises.Visibility = showHide ? Visibility.Visible : Visibility.Hidden;

            UiCbGames.Content = plural ? Properties.Resources.Games : Properties.Resources.Game;
            UiCbExercises.Content = plural ? Properties.Resources.Exercises : Properties.Resources.Exercise;
        }

        /// <summary>
        /// Checks or unchecks all check boxes.
        /// </summary>
        /// <param name="isChecked"></param>
        private void CheckAll(bool isChecked)
        {
            UiCbStudy.IsChecked = isChecked && Configuration.CleanupApplyToStudies;
            UiCbGames.IsChecked = isChecked && Configuration.CleanupApplyToGames;
            UiCbExercises.IsChecked = isChecked && Configuration.CleanupApplyToExercises;
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
            if (AppState.MainWin.ActiveVariationTree == null)
            {
                ShowItemType(GameData.ContentType.NONE, false);
                ShowEnableAllItemTypes(false, true);
            }
            else
            {
                ShowEnableAllItemTypes(false, false);
                ShowItemType(AppState.MainWin.ActiveVariationTree.ContentType, false);
            }

            UiCbStudy.Content = Properties.Resources.Study;
        }

        /// <summary>
        /// "Current Chapter" radio button selected.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiRbCurrentChapter_Checked(object sender, RoutedEventArgs e)
        {
            ShowEnableAllItemTypes(true, true);
            UiCbStudy.Content = Properties.Resources.Study;

            if (firstTimeCheck)
            {
                CheckAll(true);
                firstTimeCheck = false;
            }
        }

        /// <summary>
        /// "Entire Workbook" radio button selected.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiRbWorkbook_Checked(object sender, RoutedEventArgs e)
        {
            ShowEnableAllItemTypes(true, true);
            UiCbStudy.Content = (AppState.Workbook != null && AppState.Workbook.GetChapterCount() > 1) ? Properties.Resources.Studies : Properties.Resources.Study;
            if (firstTimeCheck)
            {
                CheckAll(true);
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
                Scope = OperationScope.ACTIVE_ITEM;
            }
            else if (UiRbCurrentChapter.IsChecked == true)
            {
                Scope = OperationScope.CHAPTER;
            }
            else if (UiRbWorkbook.IsChecked == true)
            {
                Scope = OperationScope.WORKBOOK;
            }
            Configuration.CleanupScope = Scope;

            ApplyToStudies = UiCbStudy.IsChecked == true;
            Configuration.CleanupApplyToStudies = UiCbStudy.IsChecked == true;

            ApplyToGames = UiCbGames.IsChecked == true;
            Configuration.CleanupApplyToGames = UiCbGames.IsChecked == true;
            
            ApplyToExercises = UiCbExercises.IsChecked == true;
            Configuration.CleanupApplyToExercises = UiCbExercises.IsChecked == true;

            if (UiCbComments.IsChecked == true)
            {
                MoveAttrsFlags |= (int)MoveAttribute.COMMENT_AND_NAGS;
            }
            if (UiCbEngineEvals.IsChecked == true)
            {
                MoveAttrsFlags |= (int)MoveAttribute.ENGINE_EVALUATION;
            }
            if (UiCbBadMoveDetection.IsChecked == true)
            {
                MoveAttrsFlags |= (int)MoveAttribute.BAD_MOVE_ASSESSMENT;
            }
            if (UiCbSideLines.IsChecked == true)
            {
                MoveAttrsFlags |= (int)MoveAttribute.SIDELINE;
            }
            if (UiCbReferences.IsChecked == true)
            {
                MoveAttrsFlags |= (int)MoveAttribute.REFERENCE;
            }
            if (UiCbDiagrams.IsChecked == true)
            {
                MoveAttrsFlags |= (int)MoveAttribute.DIAGRAM;
            }

            if (UiCbAnnotator.IsChecked == true)
            {
                ArticleAttrsFlags |= (int)ArticleAttribute.ANNOTATOR;
            }

            Configuration.CleanupMoveAttrs = MoveAttrsFlags;
            Configuration.CleanupArticleAttrs = ArticleAttrsFlags;

            DialogResult = true;
        }

        /// <summary>
        /// Links to the relevant Wiki page.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnHelp_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(WebAccess.UrlTarget.HelpFolder + "Annotation-Editor#deleting-annotations");
        }
    }
}
