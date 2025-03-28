﻿using ChessPosition;
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

            UiCbStudy.IsChecked = true;
            UiCbGames.IsChecked = false;
            UiCbExercises.IsChecked = false;

            UiCbStudy.Visibility = Visibility.Visible;
            if (AppState.MainWin.ActiveVariationTree == null)
            {
                UiRbCurrentChapter.IsChecked = true;
            }
            else
            {
                UiRbCurrentItem.IsChecked = true;
            }

            Scope = OperationScope.NONE;
        }

        /// <summary>
        /// Ensures that there is only one check box check,
        /// the one that corresponds to the passed itemType.
        /// </summary>
        /// <param name="itemType"></param>
        private void ShowItemType(GameData.ContentType itemType, bool enabled)
        {
            UiCbGames.IsChecked = false;
            UiCbExercises.IsChecked = false;

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
        /// sets singular or plural lable for games/exercises.
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
            UiCbStudy.IsChecked = isChecked;
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

            if (UiCbStudy.IsChecked == true)
            {
                ApplyToStudies = true;
            }
            if (UiCbGames.IsChecked == true)
            {
                ApplyToGames = true;
            }
            if (UiCbExercises.IsChecked == true)
            {
                ApplyToExercises = true;
            }

            if (UiCbComments.IsChecked == true)
            {
                MoveAttrsFlags |= (int)MoveAttribute.COMMENT_AND_NAGS;
            }
            if (UiCbAnnotator.IsChecked == true)
            {
                ArticleAttrsFlags |= (int)ArticleAttribute.ANNOTATOR;
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

            DialogResult = true;
        }

        /// <summary>
        /// Links to the relevant Wiki page.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnHelp_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/czbar/ChessForge/wiki/Cleaning-Sidelines-And-Comments");
        }
    }
}
