using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for OperationScopeDialog.xaml
    /// </summary>
    public partial class OperationScopeDialog : Window
    {
        public OperationScopeDialog(string title)
        {
            InitializeComponent();

            this.Title = title;

            UiCbStudy.IsChecked = true;
            UiCbGames.IsChecked = false;
            UiCbExercises.IsChecked = false;

            if (AppState.MainWin.ActiveVariationTree == null)
            {
                UiRbCurrentChapter.IsChecked = true;
                UiRbCurrentItem.IsEnabled = false;
            }
            else
            {
                UiRbCurrentItem.IsChecked = true;
            }
        }

        /// <summary>
        /// Ensures that there is only one check box check,
        /// the one that corresponds to the passed itemType.
        /// </summary>
        /// <param name="itemType"></param>
        private void ShowItemType(GameData.ContentType itemType)
        {
            UiCbStudy.IsChecked = false;
            UiCbGames.IsChecked = false;
            UiCbExercises.IsChecked = false;

            switch (itemType)
            {
                case GameData.ContentType.STUDY_TREE:
                    UiCbStudy.IsChecked = true;
                    break;
                case GameData.ContentType.MODEL_GAME:
                    UiCbGames.IsChecked = true;
                    break;
                case GameData.ContentType.EXERCISE:
                    UiCbExercises.IsChecked = true;
                    break;
            }
        }

        /// <summary>
        /// "Current View" radio button selected.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiRbCurrentItem_Checked(object sender, RoutedEventArgs e)
        {
            if (AppState.MainWin.ActiveVariationTree == null)
            {
                ShowItemType(GameData.ContentType.NONE);
            }
            else
            {
                ShowItemType(AppState.MainWin.ActiveVariationTree.ContentType);
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
            UiCbStudy.Content = Properties.Resources.Study;
        }

        /// <summary>
        /// "Entire Workbook" radio button selected.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiRbWorkbook_Checked(object sender, RoutedEventArgs e)
        {
            UiCbStudy.Content = (AppState.Workbook != null && AppState.Workbook.GetChapterCount() > 1) ? Properties.Resources.Studies : Properties.Resources.Study;
        }
    }
}
