using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using ChessPosition;
using GameTree;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for WorkbookOptionsDialog.xaml
    /// </summary>
    public partial class WorkbookOptionsDialog : Window
    {
        private readonly string _strWhite = "WHITE";
        private readonly string _strBlack = "BLACK";
        private readonly string _strTrainingSide = "TRAINING SIDE";

        /// <summary>
        /// The Training Side selected in the dialog.
        /// </summary>
        public PieceColor TrainingSide;

        /// <summary>
        /// The Workbook Title entered by the user
        /// </summary>
        public string WorkbookTitle;

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

        /// <summary>
        /// Creates the dialog, initializes controls
        /// </summary>
        public WorkbookOptionsDialog(Workbook _workbook)
        {
            InitializeComponent();
            WorkbookTitle = _workbook.Title;
            TrainingSide = _workbook.TrainingSide;

            _tbTitle.Text = _workbook.Title;
            UiLblSideToMove.Content = _workbook.TrainingSide == PieceColor.Black ? _strBlack : _strWhite;

            StudyBoardOrientation = _workbook.StudyBoardOrientation;
            GameBoardOrientation = _workbook.GameBoardOrientation;
            ExerciseBoardOrientation = _workbook.ExerciseBoardOrientation;

            EnableBoardOrientationControls();
        }

        /// <summary>
        /// Enables the Board Orientation controls based on the
        /// current setting of Training Side.
        /// </summary>
        private void EnableBoardOrientationControls()
        {
            bool enable = (string)(UiLblSideToMove.Content) == _strBlack;

            UiLblBoardStudyOrient.IsEnabled = enable;
            UiLblBoardGamesOrient.IsEnabled = enable;
            UiLblBoardExercisesOrient.IsEnabled = enable;

            UiImgSwapStudyOrient.IsEnabled = enable;
            UiImgSwapGameOrient.IsEnabled = enable;
            UiImgSwapExerciseOrient.IsEnabled = enable;

            if (enable)
            {
                UiLblBoardStudyOrient.IsEnabled = true;
                UiLblBoardGamesOrient.IsEnabled = true;
                UiLblBoardExercisesOrient.IsEnabled = true;
            }
            else
            {
                UiLblBoardStudyOrient.Content = _strTrainingSide;
                UiLblBoardGamesOrient.Content = _strTrainingSide;
                UiLblBoardExercisesOrient.Content = _strTrainingSide;
            }
        }

        /// <summary>
        /// The user pressed the OK button.
        /// Saves the workbook's title and exits.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnOK_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            WorkbookTitle = _tbTitle.Text;

            if ((string)UiLblSideToMove.Content == _strBlack)
            {
                TrainingSide = PieceColor.Black;
            }
            else
            {
                TrainingSide = PieceColor.White;
            }

            StudyBoardOrientation = ((string)UiLblBoardStudyOrient.Content == _strWhite) ? PieceColor.White : TrainingSide;
            GameBoardOrientation = ((string)UiLblBoardGamesOrient.Content == _strWhite) ? PieceColor.White : TrainingSide;
            ExerciseBoardOrientation = ((string)UiLblBoardExercisesOrient.Content == _strWhite) ? PieceColor.White : TrainingSide;

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
        }

        /// <summary>
        /// Toggles the board orientation for a view
        /// represented by the passed label.
        /// </summary>
        /// <param name="label"></param>
        private void SwapBoardOrientation(Label label)
        {
            if ((string)label.Content == _strWhite)
            {
                label.Content = _strTrainingSide;
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
            EnableBoardOrientationControls();        }

        /// <summary>
        /// Handles the Swap Board Orientation for the Study event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiLblBoardStudyOrient_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            SwapBoardOrientation(UiLblBoardStudyOrient);
        }

        /// <summary>
        /// Handles the Swap Board Orientation for the Games event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiLblBoardGamesOrient_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            SwapBoardOrientation(UiLblBoardGamesOrient);
        }

        /// <summary>
        /// Handles the Swap Board Orientation for the Exercise event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiLblBoardExercisesOrient_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            SwapBoardOrientation(UiLblBoardExercisesOrient);
        }

    }
}
