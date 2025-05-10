using ChessPosition;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for WorkbookOptionsDialog.xaml
    /// </summary>
    public partial class WorkbookOptionsDialog : Window
    {
        /// <summary>
        /// The Training Side selected in the dialog.
        /// </summary>
        public PieceColor TrainingSide;

        /// <summary>
        /// The Workbook Title entered by the user
        /// </summary>
        public string WorkbookTitle;

        /// <summary>
        /// Author's name entered by the user
        /// </summary>
        public string Author;

        /// <summary>
        /// Determines the initial board orientation in the Study view.
        /// </summary>
        public PieceColor StudyBoardOrientation;

        /// <summary>
        /// Determines the initial board orientation in the Games view.
        /// </summary>
        public PieceColor GameBoardOrientation;

        /// <summary>
        /// Determines the initial board orientation in the Exercises view.
        /// </summary>
        public PieceColor ExerciseBoardOrientation;

        /// <summary>
        /// True if dialog exited by clicking Save
        /// </summary>
        public bool ExitOK = false;

        // string for White to display in the UI 
        private static string _strWhite = Properties.Resources.White.ToUpper();

        // string for Black to display in the UI 
        private static string _strBlack = Properties.Resources.Black.ToUpper();

        // string for side_to_move 
        private readonly static string _strSideToMove = Properties.Resources.SideToMove.ToUpper();

        /// <summary>
        /// Creates the dialog, initializes controls
        /// </summary>
        public WorkbookOptionsDialog(Workbook _workbook)
        {
            InitializeComponent();
            WorkbookTitle = _workbook.Title;
            Author = _workbook.Author;
            TrainingSide = _workbook.TrainingSideConfig;

            UiTbTitle.Text = _workbook.Title;
            UiTbAuthor.Text = _workbook.Author;
            UiLblSideToMove.Content = _workbook.TrainingSideConfig == PieceColor.Black ? _strBlack : _strWhite;

            UiLblSideToMove.ToolTip = Properties.Resources.TooltipSideToMove;

            StudyBoardOrientation = GetBoardOrientation(_workbook.StudyBoardOrientationConfig);
            UiLblBoardStudyOrient.Content = _workbook.StudyBoardOrientationConfig == PieceColor.Black ? _strBlack : _strWhite;

            GameBoardOrientation = GetBoardOrientation(_workbook.GameBoardOrientationConfig);
            UiLblBoardGamesOrient.Content = _workbook.GameBoardOrientationConfig == PieceColor.Black ? _strBlack : _strWhite;

            // in Exercise, PieceColor.None is valid as it indicates "side to move"
            ExerciseBoardOrientation = _workbook.ExerciseBoardOrientationConfig;
            if (ExerciseBoardOrientation == PieceColor.None)
            {
                UiLblBoardExercisesOrient.Content = _strSideToMove;
            }
            else
            {
                UiLblBoardExercisesOrient.Content = _workbook.ExerciseBoardOrientationConfig == PieceColor.Black ? _strBlack : _strWhite;
            }

            UiLblVersion.Content = Properties.Resources.Version +  ": " + _workbook.Version;
            UiLblVersion.ToolTip = Properties.Resources.TooltipWorkbookVersion;
            
        }

        /// <summary>
        /// Returns the board orientation color to show in the GUI.
        /// Returns White or Black, never None.
        /// </summary>
        /// <param name="proposed"></param>
        /// <returns></returns>
        private PieceColor GetBoardOrientation(PieceColor proposed)
        {
            PieceColor color;

            if (proposed == PieceColor.None)
            {
                color = TrainingSide != PieceColor.None ? TrainingSide : PieceColor.White;
            }
            else
            {
                color = proposed;
            }

            return color;
        }

        /// <summary>
        /// The user pressed the OK button.
        /// Saves the workbook's title and exits.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnOK_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            WorkbookTitle = UiTbTitle.Text;
            Author = UiTbAuthor.Text;

            if ((string)UiLblSideToMove.Content == _strBlack)
            {
                TrainingSide = PieceColor.Black;
            }
            else
            {
                TrainingSide = PieceColor.White;
            }

            StudyBoardOrientation = ((string)UiLblBoardStudyOrient.Content == _strBlack) ? PieceColor.Black : PieceColor.White;
            GameBoardOrientation = ((string)UiLblBoardGamesOrient.Content == _strBlack) ? PieceColor.Black : PieceColor.White;
            
            if ((string)UiLblBoardExercisesOrient.Content == _strSideToMove)
            {
                ExerciseBoardOrientation = PieceColor.None;
            }
            else
            {
                ExerciseBoardOrientation = ((string)UiLblBoardExercisesOrient.Content == _strBlack) ? PieceColor.Black : PieceColor.White;
            }

            ExitOK = true;
            this.Close();
        }

        /// <summary>
        /// Exits the dialog without setting ExitOK to true.
        /// The caller should consider such exit as cancellation.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnCancel_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Invokes the appropriate Wiki page
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnHelp_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/czbar/ChessForge/wiki/Workbook-Options");
        }

        /// <summary>
        /// Toggles the Training Side
        /// </summary>
        private void SwapTrainingSide()
        {
            if ((string)UiLblSideToMove.Content == _strWhite)
            {
                UiLblSideToMove.Content = _strBlack;
                TrainingSide = PieceColor.Black;
            }
            else
            {
                UiLblSideToMove.Content = _strWhite;
                TrainingSide = PieceColor.White;
            }
            SetAllBoardOrientations(TrainingSide);
        }

        /// <summary>
        /// Sets all boards' orientation to the passed color.
        /// </summary>
        /// <param name="color"></param>
        private void SetAllBoardOrientations(PieceColor color)
        {
            if (color != PieceColor.None)
            {
                UiLblBoardStudyOrient.Content = (color == PieceColor.White ? _strWhite : _strBlack);
                UiLblBoardGamesOrient.Content = (color == PieceColor.White ? _strWhite : _strBlack);

                if ((string)UiLblBoardExercisesOrient.Content != _strSideToMove)
                {
                    UiLblBoardExercisesOrient.Content = (color == PieceColor.White ? _strWhite : _strBlack);
                }
            }
        }

        /// <summary>
        /// Toggles the board orientation for a view
        /// represented by the passed label.
        /// </summary>
        /// <param name="label"></param>
        private void SwapBoardOrientation(Label label, bool includeNone)
        {
            if ((string)label.Content == _strWhite)
            {
                label.Content = _strBlack;
            }
            else if ((string)label.Content == _strBlack)
            {
                label.Content = includeNone ? _strSideToMove : _strWhite;
            }
            else
            {
                label.Content = _strWhite;
            }
        }

        /// <summary>
        /// In response to the user clicking on the Swap icon,
        /// swaps the training side.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiImgSwapColor_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            SwapTrainingSide();
        }

        /// <summary>
        /// Handles the Swap Board Orientation for the Study event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiLblBoardStudyOrient_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            SwapBoardOrientation(UiLblBoardStudyOrient, false);
        }

        /// <summary>
        /// Handles the Swap Board Orientation for the Games event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiLblBoardGamesOrient_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            SwapBoardOrientation(UiLblBoardGamesOrient, false);
        }

        /// <summary>
        /// Handles the Swap Board Orientation for the Exercise event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiLblBoardExercisesOrient_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            SwapBoardOrientation(UiLblBoardExercisesOrient, true);
        }

    }
}
