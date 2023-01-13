using ChessPosition.GameTree;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using GameTree;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for SelectGamesDialog.xaml
    /// </summary>
    public partial class SelectGamesDialog : Window
    {
        /// <summary>
        /// Indicates whether a Study should be created
        /// from selected games.
        /// </summary>
        public bool CreateStudy = true;

        /// <summary>
        /// Indicates whether Games should be copied
        /// into the Games section of the chapter.
        /// </summary>
        public bool CopyGames = true;


        /// <summary>
        /// Indicates whether each Game should have
        /// its own chapter when creating a Workbook.
        /// </summary>
        public bool MultiChapter = false;

        /// <summary>
        /// Modes in which this dialog can be invoked.
        /// </summary>
        public enum Mode
        {
            IMPORT_INTO_NEW_CHAPTER,
            IMPORT_GAMES,
            IMPORT_EXERCISES,
            CREATE_WORKBOOK,
        }

        /// <summary>
        /// The list of games to process.
        /// </summary>
        private ObservableCollection<GameData> _gameList;

        // mode in which this dialog was invoked
        private Mode _mode;

        // number of Games in the passed list
        private int _gameCount;

        // number of Exercises in the passed list
        private int _exerciseCount;

        /// <summary>
        /// Creates the dialog object. Sets ItemsSource for the ListView
        /// to GamesHeaders list.
        /// This dialog will be invoked in the follwoing contexts:
        /// - Selecting Games and Exercise to create a new Chapter
        /// - Importing Games into a chapter
        /// - Importing Exercises into a chapter
        /// - Creating a new Workbook
        /// </summary>
        public SelectGamesDialog(ref ObservableCollection<GameData> gameList, Mode mode)
        {
            _mode = mode;
            _gameList = gameList;
            SetGameAndExerciseCount();
            InitializeComponent();
            UiLvGames.ItemsSource = gameList;

            SetInstructionText();
            InitializeCopyOptionsRadioButtons();

            ShowMultiChapterCheckBox();
        }

        /// <summary>
        /// Controls the visibility of the checkbox
        /// and radio buttons.
        /// </summary>
        private void ShowMultiChapterCheckBox()
        {
            if (_mode == Mode.CREATE_WORKBOOK)
            {
                UiCbMultiChapter.Visibility = Visibility.Visible;
            }
            else
            {
                UiCbMultiChapter.Visibility = Visibility.Collapsed;

                MoveRadioButtonUp(UiRbStudyAndGames);
                MoveRadioButtonUp(UiRbStudyOnly);
                MoveRadioButtonUp(UiRbGamesOnly);
            }
        }

        /// <summary>
        /// Moves the Radio Button for esthetics reasons if
        /// the Multi Chapter button is not showing.
        /// </summary>
        /// <param name="rb"></param>
        private void MoveRadioButtonUp(RadioButton rb)
        {
            Thickness th = rb.Margin;
            th.Top -= 15;
            th.Bottom -= 15;
            rb.Margin = th;
        }

        /// <summary>
        /// Sets the text above the selection list depending on the dialog mode
        /// and content of the game list.
        /// </summary>
        private void SetInstructionText()
        {
            switch (_mode)
            {
                case Mode.CREATE_WORKBOOK:
                    UiLblInstruct.Content = "Select Items to create a new Workbook from:";
                    break;
                case Mode.IMPORT_GAMES:
                    UiLblInstruct.Content = "Select Games to Import:";
                    break;
                case Mode.IMPORT_EXERCISES:
                    UiLblInstruct.Content = "Select Exercises to Import:";
                    break;
                case Mode.IMPORT_INTO_NEW_CHAPTER:
                    UiLblInstruct.Content = "Select Items to create a new Chapter from:";
                    break;
            }
        }

        /// <summary>
        /// Selects the first radio button for copy/merge options
        /// if there is at least one game.
        /// Disables all radio buttons if there are no games.
        /// </summary>
        private void InitializeCopyOptionsRadioButtons()
        {
            if (_mode == Mode.IMPORT_GAMES || _mode == Mode.IMPORT_EXERCISES)
            {

                UiRbStudyAndGames.Visibility = Visibility.Collapsed;
                UiRbStudyOnly.Visibility = Visibility.Collapsed;
                UiRbGamesOnly.Visibility = Visibility.Collapsed;
            }
            else
            {
                if (_gameCount > 0)
                {
                    UiRbStudyAndGames.IsChecked = true;
                }
                else
                {
                    UiRbStudyAndGames.IsEnabled = false;
                    UiRbStudyOnly.IsEnabled = false;
                    UiRbGamesOnly.IsEnabled = false;
                }
            }
        }

        /// <summary>
        /// Count games and exercises.
        /// </summary>
        private void SetGameAndExerciseCount()
        {
            List<GameData> lstGamesToRemove = new List<GameData>();

            foreach (GameData gameData in _gameList)
            {
                GameData.ContentType typ = gameData.GetContentType();
                if (typ == GameData.ContentType.GENERIC || typ == GameData.ContentType.MODEL_GAME)
                {
                    _gameCount++;
                }
                else if (typ == GameData.ContentType.EXERCISE)
                {
                    _exerciseCount++;
                }
                else
                {
                    lstGamesToRemove.Add(gameData);
                }
            }

            foreach (GameData gd in lstGamesToRemove)
            {
                _gameList.Remove(gd);
            }
        }

        /// <summary>
        /// SelectAll box was checked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiCbSelectAll_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var item in _gameList)
            {
                item.IsSelected = true;
            }
        }

        /// <summary>
        /// SelectAll box was unchecked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiCbSelectAll_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (var item in _gameList)
            {
                item.IsSelected = false;
            }
        }

        /// <summary>
        /// OK button was clicked. Exits with the result = true
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnOk_Click(object sender, RoutedEventArgs e)
        {
            CopyGames = UiRbGamesOnly.IsChecked == true || UiRbStudyAndGames.IsChecked == true;
            CreateStudy = UiRbStudyOnly.IsChecked == true || UiRbStudyAndGames.IsChecked == true;
            MultiChapter = UiCbMultiChapter.IsChecked == true;
            
            DialogResult = true;
        }

        /// <summary>
        /// Cancel button was clicked. Exits with the result = false
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
