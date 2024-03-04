using ChessPosition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for ChessboardColorsDialog.xaml
    /// </summary>
    public partial class ChessboardColorsDialog : Window
    {
        // dictionary mapping icon image controls in the dialog tp BoardSets
        private Dictionary<Image, BoardSet> _dictIconImageToBoardSet = new Dictionary<Image, BoardSet>();

        // dictionary tracking current selections
        private Dictionary<TabViewType, ChessBoards.ColorSet>  _dictViewToColorSet = new Dictionary<TabViewType, ChessBoards.ColorSet> ();

        // tracks the currently selected view
        private TabViewType _selectedView;

        /// <summary>
        /// Initializes the GUI and the structures
        /// </summary>
        public ChessboardColorsDialog()
        {
            InitializeComponent();

            _dictViewToColorSet.Add(TabViewType.STUDY, Configuration.StudyBoardSet.Colors);
            _dictViewToColorSet.Add(TabViewType.MODEL_GAME, Configuration.GameBoardSet.Colors);
            _dictViewToColorSet.Add(TabViewType.EXERCISE, Configuration.ExerciseBoardSet.Colors);
            _dictViewToColorSet.Add(TabViewType.TRAINING, Configuration.TrainingBoardSet.Colors);  

            IconsToBoardSets();
            SelectViewType(AppState.ActiveTab);
            SetIconImages();
        }

        /// <summary>
        /// Updates the current selection tracker per the current configuration.
        /// This will be called after the defaults are requested.
        /// </summary>
        private void UpdateViewColorSelections()
        {
            _dictViewToColorSet[TabViewType.STUDY] = Configuration.StudyBoardSet.Colors;
            _dictViewToColorSet[TabViewType.MODEL_GAME] = Configuration.GameBoardSet.Colors;
            _dictViewToColorSet[TabViewType.EXERCISE] = Configuration.ExerciseBoardSet.Colors;
            _dictViewToColorSet[TabViewType.TRAINING] = Configuration.TrainingBoardSet.Colors;
        }

        /// <summary>
        /// Initializes the mapping of icon image controls to chessboard color sets
        /// </summary>
        private void IconsToBoardSets()
        {
            _dictIconImageToBoardSet.Add(UiImg1, ChessBoards.BoardSets[ChessBoards.ColorSet.BLUE]);
            _dictIconImageToBoardSet.Add(UiImg2, ChessBoards.BoardSets[ChessBoards.ColorSet.GREEN]);
            _dictIconImageToBoardSet.Add(UiImg3, ChessBoards.BoardSets[ChessBoards.ColorSet.LIGHT_BLUE]);
            _dictIconImageToBoardSet.Add(UiImg4, ChessBoards.BoardSets[ChessBoards.ColorSet.LIGHT_GREEN]);
            _dictIconImageToBoardSet.Add(UiImg5, ChessBoards.BoardSets[ChessBoards.ColorSet.PALE_BLUE]);
            _dictIconImageToBoardSet.Add(UiImg6, ChessBoards.BoardSets[ChessBoards.ColorSet.GREY]);
            _dictIconImageToBoardSet.Add(UiImg7, ChessBoards.BoardSets[ChessBoards.ColorSet.BROWN]);
            _dictIconImageToBoardSet.Add(UiImg8, ChessBoards.BoardSets[ChessBoards.ColorSet.ORANGE_SHADES]);
        }

        /// <summary>
        /// Sets image sources for icon image controls
        /// </summary>
        private void SetIconImages()
        {
            UiImg1.Source = _dictIconImageToBoardSet[UiImg1].Icon;
            UiImg2.Source = _dictIconImageToBoardSet[UiImg2].Icon;
            UiImg3.Source = _dictIconImageToBoardSet[UiImg3].Icon;
            UiImg4.Source = _dictIconImageToBoardSet[UiImg4].Icon;
            UiImg5.Source = _dictIconImageToBoardSet[UiImg5].Icon;
            UiImg6.Source = _dictIconImageToBoardSet[UiImg6].Icon;
            UiImg7.Source = _dictIconImageToBoardSet[UiImg7].Icon;
            UiImg8.Source = _dictIconImageToBoardSet[UiImg8].Icon;
        }

        /// <summary>
        /// Set the GUI controls per the selected View Type
        /// </summary>
        /// <param name="vt"></param>
        private void SelectViewType(TabViewType vt)
        {
            DimViewTypeButton(UiBtnStudyBoard);
            DimViewTypeButton(UiBtnGamesBoard);
            DimViewTypeButton(UiBtnExercisesBoard);
            DimViewTypeButton(UiBtnTrainingBoard);

            switch (vt)
            {
                case TabViewType.MODEL_GAME:
                    _selectedView = TabViewType.MODEL_GAME;
                    HighlightViewTypeButton(UiBtnGamesBoard);
                    UiImgSelected.Source = ChessBoards.BoardSets[_dictViewToColorSet[TabViewType.MODEL_GAME]].SmallBoard;
                    break;
                case TabViewType.EXERCISE:
                    _selectedView = TabViewType.EXERCISE;
                    HighlightViewTypeButton(UiBtnExercisesBoard);
                    UiImgSelected.Source = ChessBoards.BoardSets[_dictViewToColorSet[TabViewType.EXERCISE]].SmallBoard;
                    break;
                case TabViewType.TRAINING:
                    _selectedView = TabViewType.TRAINING;
                    HighlightViewTypeButton(UiBtnTrainingBoard);
                    UiImgSelected.Source = ChessBoards.BoardSets[_dictViewToColorSet[TabViewType.TRAINING]].SmallBoard;
                    break;
                case TabViewType.STUDY:
                default:
                    _selectedView = TabViewType.STUDY;
                    HighlightViewTypeButton(UiBtnStudyBoard);
                    UiImgSelected.Source = ChessBoards.BoardSets[_dictViewToColorSet[TabViewType.STUDY]].SmallBoard;
                    break;
            }
        }

        /// <summary>
        /// Highlight a View Type button.
        /// </summary>
        /// <param name="btn"></param>
        private void HighlightViewTypeButton(Button btn)
        {
            btn.FontWeight = FontWeights.Bold;
            btn.Foreground = Brushes.DarkBlue;
            btn.Background = Brushes.LightGray;
        }

        /// <summary>
        /// Un-highlight a View Type button.
        /// </summary>
        /// <param name="btn"></param>
        private void DimViewTypeButton(Button btn)
        {
            btn.FontWeight = FontWeights.Normal;
            btn.Foreground = Brushes.DarkGray;
        }

        /// <summary>
        /// Study View board was selected.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnStudyBoard_Click(object sender, RoutedEventArgs e)
        {
            SelectViewType(TabViewType.STUDY);
        }

        /// <summary>
        /// Games View board was selected.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnGamesBoard_Click(object sender, RoutedEventArgs e)
        {
            SelectViewType(TabViewType.MODEL_GAME);
        }

        /// <summary>
        /// Exercises View board was selected.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnExercisesBoard_Click(object sender, RoutedEventArgs e)
        {
            SelectViewType(TabViewType.EXERCISE);
        }

        /// <summary>
        /// Training View board was selected.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnTrainingBoard_Click(object sender, RoutedEventArgs e)
        {
            SelectViewType(TabViewType.TRAINING);
        }

        /// <summary>
        /// Icon image control was clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiImg1_MouseDown(object sender, MouseButtonEventArgs e)
        {
            UiImgSelected.Source = _dictIconImageToBoardSet[UiImg1].SmallBoard;
            _dictViewToColorSet[_selectedView] = _dictIconImageToBoardSet[UiImg1].Colors;
        }

        /// <summary>
        /// Icon image control was clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiImg2_MouseDown(object sender, MouseButtonEventArgs e)
        {
            UiImgSelected.Source = _dictIconImageToBoardSet[UiImg2].SmallBoard;
            _dictViewToColorSet[_selectedView] = _dictIconImageToBoardSet[UiImg2].Colors;
        }

        /// <summary>
        /// Icon image control was clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiImg3_MouseDown(object sender, MouseButtonEventArgs e)
        {
            UiImgSelected.Source = _dictIconImageToBoardSet[UiImg3].SmallBoard;
            _dictViewToColorSet[_selectedView] = _dictIconImageToBoardSet[UiImg3].Colors;
        }

        /// <summary>
        /// Icon image control was clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiImg4_MouseDown(object sender, MouseButtonEventArgs e)
        {
            UiImgSelected.Source = _dictIconImageToBoardSet[UiImg4].SmallBoard;
            _dictViewToColorSet[_selectedView] = _dictIconImageToBoardSet[UiImg4].Colors;
        }

        /// <summary>
        /// Icon image control was clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiImg5_MouseDown(object sender, MouseButtonEventArgs e)
        {
            UiImgSelected.Source = _dictIconImageToBoardSet[UiImg5].SmallBoard;
            _dictViewToColorSet[_selectedView] = _dictIconImageToBoardSet[UiImg5].Colors;
        }

        /// <summary>
        /// Icon image control was clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiImg6_MouseDown(object sender, MouseButtonEventArgs e)
        {
            UiImgSelected.Source = _dictIconImageToBoardSet[UiImg6].SmallBoard;
            _dictViewToColorSet[_selectedView] = _dictIconImageToBoardSet[UiImg6].Colors;
        }

        /// <summary>
        /// Icon image control was clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiImg7_MouseDown(object sender, MouseButtonEventArgs e)
        {
            UiImgSelected.Source = _dictIconImageToBoardSet[UiImg7].SmallBoard;
            _dictViewToColorSet[_selectedView] = _dictIconImageToBoardSet[UiImg7].Colors;
        }

        /// <summary>
        /// Icon image control was clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiImg8_MouseDown(object sender, MouseButtonEventArgs e)
        {
            UiImgSelected.Source = _dictIconImageToBoardSet[UiImg8].SmallBoard;
            _dictViewToColorSet[_selectedView] = _dictIconImageToBoardSet[UiImg8].Colors;
        }

        /// <summary>
        /// The Save button was clicked.
        /// Persist the configuration.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnSave_Click(object sender, RoutedEventArgs e)
        {
            Configuration.StudyBoardSet = ChessBoards.BoardSets[_dictViewToColorSet[TabViewType.STUDY]];
            Configuration.GameBoardSet = ChessBoards.BoardSets[_dictViewToColorSet[TabViewType.MODEL_GAME]];
            Configuration.ExerciseBoardSet = ChessBoards.BoardSets[_dictViewToColorSet[TabViewType.EXERCISE]];
            Configuration.TrainingBoardSet = ChessBoards.BoardSets[_dictViewToColorSet[TabViewType.TRAINING]];
            DialogResult = true;
        }

        /// <summary>
        /// The user requested to restore defaults.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnDefaults_Click(object sender, RoutedEventArgs e)
        {
            Configuration.SetChessboardDefaults();
            UpdateViewColorSelections();
            SelectViewType(_selectedView);
        }
    }
}
