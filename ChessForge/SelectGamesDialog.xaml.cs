using GameTree;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for SelectGamesDialog.xaml
    /// </summary>
    public partial class SelectGamesDialog : Window
    {
        /// <summary>
        /// Modes in which this dialog can be invoked.
        /// </summary>
        public enum Mode
        {
            IMPORT_INTO_NEW_CHAPTER,
            IMPORT_GAMES,
            IMPORT_EXERCISES,
            CREATE_WORKBOOK,
            DOWNLOAD_WEB_GAMES,
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
        /// This dialog will be invoked in the following contexts:
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
            CheckIfAllSelected();

            if (mode == Mode.DOWNLOAD_WEB_GAMES)
            {
                PrepareGuiInDownloadMode();
            }
            else
            {
                (UiLvGames.View as GridView).Columns[1].Header = BuildItemsHeaderText();
            }

            UiCbSelectAll.Checked += UiCbSelectAll_Checked;
            UiCbSelectAll.Unchecked += UiCbSelectAll_Unchecked;

            UiLvGames.ItemsSource = gameList;

            SetInstructionText();
        }

        /// <summary>
        /// Builds text for the header of the column with item titles.
        /// </summary>
        /// <returns></returns>
        private string BuildItemsHeaderText()
        {
            StringBuilder sb = new StringBuilder();
            if (_gameCount > 0)
            {
                sb.Append(Properties.Resources.GameCount + " = " + _gameCount.ToString());
                if (_exerciseCount > 0)
                {
                    sb.Append(" / ");
                }
            }
            if (_exerciseCount > 0)
            {
                sb.Append(Properties.Resources.ExerciseCount + " = " + _exerciseCount.ToString());
            }

            return sb.ToString();
        }

        /// <summary>
        /// if all items are selected, check the SelectAll Checkbox
        /// </summary>
        private void CheckIfAllSelected()
        {
            bool allSelected = true;
            foreach (GameData gameData in _gameList)
            {
                if (!gameData.IsSelected)
                {
                    allSelected = false;
                    break;
                }
            }
            if (allSelected && _gameList.Count > 0)
            {
                UiCbSelectAll.IsChecked = true;
            }
        }

        /// <summary>
        /// In the Download mode, the list looks different and we need different text
        /// in some places.
        /// </summary>
        private void PrepareGuiInDownloadMode()
        {
            GridView gridView = UiLvGames.View as GridView;

            // remove all columns except the first one, which is a special one with the selection check box
            while (gridView.Columns.Count > 1)
            {
                gridView.Columns.RemoveAt(1);
            }

            // create columns 
            GridViewColumn no = ListViewHelper.CreateColumn(Properties.Resources.OrderNo, 40, "OrderNo");
            gridView.Columns.Add(no);

            GridViewColumn eco = ListViewHelper.CreateColumn(Properties.Resources.ECO, 40, "ECO");
            gridView.Columns.Add(eco);

            GridViewColumn game = ListViewHelper.CreateColumn(Properties.Resources.Game, 570, "GameTitle");
            gridView.Columns.Add(game);
            game.Header = BuildItemsHeaderText();

            GridViewColumn date = ListViewHelper.CreateColumn(Properties.Resources.Date, 90, "Date");
            gridView.Columns.Add(date);

            // change the title and the "instruction" label
            this.Title = Properties.Resources.DownloadedGames;
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
                    UiLblInstruct.Content = Properties.Resources.SelectItemsforWorkbook;
                    break;
                case Mode.IMPORT_GAMES:
                    UiLblInstruct.Content = Properties.Resources.SelectGamesToImport;
                    break;
                case Mode.IMPORT_EXERCISES:
                    UiLblInstruct.Content = Properties.Resources.SelectExercisesToImport;
                    break;
                case Mode.IMPORT_INTO_NEW_CHAPTER:
                    UiLblInstruct.Content = Properties.Resources.SelectItemsForChapter;
                    break;
                case Mode.DOWNLOAD_WEB_GAMES:
                    UiLblInstruct.Content = Properties.Resources.SelectGames;
                    break;
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
                GameData.ContentType typ = gameData.GetContentType(false);
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
