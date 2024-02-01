﻿using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.Text;
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
            ASSIGN_ECO
        }

        /// <summary>
        /// Type of the action being scoped in this session.
        /// </summary>
        public ScopedAction Action;

        /// <summary>
        /// The scope selected by the user.
        /// </summary>
        public OperationScope ApplyScope { get; set; }

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
        /// Whether Study should be shown at all
        /// </summary>
        private bool _allowStudies
        {
            get => Action != ScopedAction.ASSIGN_ECO;
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

            if (_allowStudies)
            {
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
            }
            else
            {
                UiCbStudy.Content = "   -   ";
                UiCbStudy.IsChecked = false;
                UiCbGames.IsChecked = true;
                UiCbExercises.IsChecked = false;

                UiCbStudy.IsEnabled = false;

                if (AppState.Workbook.ActiveArticle != null &&
                    (AppState.Workbook.ActiveArticle.ContentType == GameData.ContentType.MODEL_GAME
                    || AppState.Workbook.ActiveArticle.ContentType == GameData.ContentType.EXERCISE))
                {
                    UiRbCurrentItem.IsChecked = true;
                }
                else
                {
                    UiRbCurrentItem.IsEnabled = false;
                    UiRbCurrentChapter.IsChecked = true;
                }
            }

            ApplyScope = OperationScope.NONE;
        }

        /// <summary>
        /// Ensures that there is only one check box check,
        /// the one that corresponds to the passed itemType.
        /// </summary>
        /// <param name="itemType"></param>
        private void ShowItemType(GameData.ContentType itemType, bool enabled)
        {
            if (!_allowStudies && itemType == GameData.ContentType.STUDY_TREE)
            {
                return;
            }

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
            UiCbStudy.IsEnabled = _allowStudies;
            UiCbGames.IsEnabled = true;
            UiCbExercises.IsEnabled = true;

            UiCbStudy.Visibility = (showHide && _allowStudies) ? Visibility.Visible : Visibility.Hidden;
            UiCbGames.Visibility = showHide ? Visibility.Visible : Visibility.Hidden;
            UiCbExercises.Visibility = showHide ? Visibility.Visible : Visibility.Hidden;

            //UiCbStudy.IsEnabled = (showHide && _allowStudies) ? true : false;
            //UiCbGames.IsEnabled = showHide ? true : false;
            //UiCbExercises.IsEnabled = showHide ? true : false;

            UiCbGames.Content = plural ? Properties.Resources.Games : Properties.Resources.Game;
            UiCbExercises.Content = plural ? Properties.Resources.Exercises : Properties.Resources.Exercise;
        }

        /// <summary>
        /// Checks or unchecks all check boxes.
        /// </summary>
        /// <param name="isChecked"></param>
        private void CheckAll(bool isChecked)
        {
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
                ShowItemType(GameData.ContentType.NONE, false);
                ShowEnableAllItemTypes(false, true);
            }
            else
            {
                ShowEnableAllItemTypes(false, false);
                ShowItemType(AppState.MainWin.ActiveVariationTree.ContentType, false);
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
            ShowEnableAllItemTypes(true, true);
            if (_allowStudies)
            {
                UiCbStudy.Content = Properties.Resources.Study;
            }
            if (firstTimeCheck)
            {
                CheckAll(true);
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
            ShowEnableAllItemTypes(true, true);
            if (_allowStudies)
            {
                UiCbStudy.Content = (AppState.Workbook != null && AppState.Workbook.GetChapterCount() > 1) ? Properties.Resources.Studies : Properties.Resources.Study;
            }
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

            DialogResult = true;
        }
    }
}
