using ChessPosition;
using EngineService;
using GameTree;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Security.Policy;
using System.Diagnostics;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // prefix use for manu items showing recent files
        public readonly string MENUITEM_RECENT_FILES_PREFIX = "RecentFiles";

        public readonly string APP_NAME = "Chess Forge";

        /// <summary>
        /// The RichTextBox based full Workbook view
        /// </summary>
        private WorkbookView _workbookView;

        /// <summary>
        /// The RichTextBox based view of the lines
        /// starting from the Bookmark position being
        /// trained from.
        /// </summary>
        private WorkbookView _trainingBrowseRichTextBuilder;

        /// <summary>
        /// The RichTextBox based training view
        /// </summary>
        public TrainingView UiTrainingView;

        // width and and height of a square in the main chessboard
        private const int squareSize = 80;

        public EngineLinesBox EngineLinesGUI;
        AnimationState MoveAnimation = new AnimationState();
        public EvaluationManager Evaluation;

        // The main chessboard of the application
        public ChessBoard MainChessBoard;

        /// <summary>
        /// Chessboard shown over moves in different views
        /// </summary>
        public ChessBoard FloatingChessBoard;

        /// <summary>
        /// The RichTextBox based comment box
        /// underneath the main chessbaord.
        /// </summary>
        public CommentBox BoardCommentBox;

        public GameReplay ActiveLineReplay;

        /// <summary>
        /// manages data for the ActiveLine DataGrid
        /// </summary>
        public ActiveLineManager ActiveLine;

        /// <summary>
        /// The complete tree of the currently
        /// loaded workbook (from the PGN or CHF file)
        /// </summary>
        public WorkbookTree Workbook;

        /// <summary>
        /// Determines if the program is running in Debug mode.
        /// </summary>
        private bool _isDebugMode = false;

        /// <summary>
        /// Collection of timers for this application.
        /// </summary>
        public AppTimers Timers;

        /// <summary>
        /// The main application window.
        /// Initializes the GUI controls.
        /// Note that some of the controls must be initialized
        /// in a particular order as one control may use a reference 
        /// to another one.
        /// </summary>
        public MainWindow()
        {
            AppStateManager.MainWin = this;

            // Sets a public reference for access from other objects.
            EngineGame.SetMainWin(this);
            Evaluation = new EvaluationManager();

            InitializeComponent();
            SoundPlayer.Initialize();

            BoardCommentBox = new CommentBox(UiRtbBoardComment.Document, this);
            ActiveLine = new ActiveLineManager(UiDgActiveLine, this);

            EngineLinesGUI = new EngineLinesBox(this, UiTbEngineLines, UiPbEngineThinking, Evaluation);
            Timers = new AppTimers(EngineLinesGUI, this);

            Configuration.Initialize(this);
            Configuration.StartDirectory = App.AppPath;
            Configuration.ReadConfigurationFile();
            MoveAnimation.MoveDuration = Configuration.MoveSpeed;
            if (Configuration.IsMainWinPosValid())
            {
                this.Left = Configuration.MainWinPos.Left;
                this.Top = Configuration.MainWinPos.Top;
                this.Width = Configuration.MainWinPos.Right - Configuration.MainWinPos.Left;
                this.Height = Configuration.MainWinPos.Bottom - Configuration.MainWinPos.Top;
            }

            // main chess board
            MainChessBoard = new ChessBoard(MainCanvas, UiImgMainChessboard, null, true);
            FloatingChessBoard = new ChessBoard(_cnvFloat, _imgFloatingBoard, null, true);

            BookmarkManager.InitBookmarksGui(this);

            ActiveLineReplay = new GameReplay(this, MainChessBoard, BoardCommentBox);

            _isDebugMode = Configuration.DebugMode != 0;
        }

        /// <summary>
        /// Actions taken after the main window
        /// has been loaded.
        /// In particular, if the last used file can be identified
        /// it will be read in and the session initrialized with it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UiDgActiveLine.ContextMenu = UiMnMainBoard;
            AddDebugMenu();

            LearningMode.ChangeCurrentMode(LearningMode.Mode.IDLE);
            AppStateManager.SetupGuiForCurrentStates();

            Timers.Start(AppTimers.TimerId.APP_START);
        }

        [Conditional("DEBUG")]
        private void AddDebugMenu()
        {
            MenuItem mnDebug = new MenuItem
            {
                Name = "DebugMenu"
            };

            mnDebug.Header = "Debug";
            UiMainMenu.Items.Add(mnDebug);

            MenuItem mnDebugDump = new MenuItem
            {
                Name = "DebugDumpMenu"
            };

            mnDebugDump.Header = "Dump All";
            mnDebug.Items.Add(mnDebugDump);
            mnDebugDump.Click += UiMnDebugDump_Click;

            MenuItem mnDebugDumpStates = new MenuItem
            {
                Name = "DebugDumpStates"
            };

            mnDebugDumpStates.Header = "Dump States and Timers";
            mnDebug.Items.Add(mnDebugDumpStates);
            mnDebugDumpStates.Click += UiMnDebugDumpStates_Click;
        }

        private void UiMnDebugDump_Click(object sender, RoutedEventArgs e)
        {
            DumpDebugLogs(true);
        }

        private void UiMnDebugDumpStates_Click(object sender, RoutedEventArgs e)
        {
            DumpDebugStates();
        }

        // tracks the application start stage
        private int _appStartStage = 0;

        // lock object to use during the startup process
        private object _appStartLock = new object();

        /// <summary>
        /// This method controls the two important stages of the startup process.
        /// When the Appstart timer invokes it for the first time, the engine
        /// will be loaded while the timer is stopped.
        /// The second time it is invoked, it will read the most recent file
        /// if such file exists.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        public void AppStartTimeUp(object source, ElapsedEventArgs e)
        {
            lock (_appStartLock)
            {

                if (_appStartStage == 0)
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        BoardCommentBox.StartingEngine();
                    });
                    _appStartStage = 1;
                    Timers.Stop(AppTimers.TimerId.APP_START);
                    EngineMessageProcessor.CreateEngineService(this, _isDebugMode);
                    Timers.Start(AppTimers.TimerId.APP_START);
                }
                else if (_appStartStage == 1)
                {
                    _appStartStage = 2;
                    this.Dispatcher.Invoke(() =>
                    {

                        CreateRecentFilesMenuItems();
                        Timers.Stop(AppTimers.TimerId.APP_START);
                        bool engineStarted = EngineMessageProcessor.StartEngineService();
                        Timers.Start(AppTimers.TimerId.APP_START);
                        if (!engineStarted)
                        {
                            MessageBox.Show("Failed to load the engine. Move evaluation will not be available.", "Chess Engine Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        // if we have LastWorkbookFile or a name on the commend line
                        // we will try to open
                        string cmdLineFile = App.CmdLineFileName;
                        bool success = false;
                        if (!string.IsNullOrEmpty(cmdLineFile))
                        {
                            try
                            {
                                ReadWorkbookFile(cmdLineFile, true);
                                success = true;
                            }
                            catch
                            {
                                success = false;
                            }
                        }

                        if (!success)
                        {
                            string lastWorkbookFile = Configuration.LastWorkbookFile;

                            if (!string.IsNullOrEmpty(lastWorkbookFile))
                            {
                                try
                                {
                                    ReadWorkbookFile(lastWorkbookFile, true);
                                }
                                catch
                                {
                                }
                            }
                            else
                            {
                                BoardCommentBox.OpenFile();
                            }
                        }
                    });
                }
            }

            if (_appStartStage == 2)
            {
                Timers.Stop(AppTimers.TimerId.APP_START);
            }
        }

        /// <summary>
        /// Creates menu items for the Recent Files and 
        /// adds them to the File menu.
        /// </summary>
        private void CreateRecentFilesMenuItems()
        {
            List<string> recentFiles = Configuration.RecentFiles;
            for (int i = 0; i < recentFiles.Count; i++)
            {
                MenuItem mi = new MenuItem
                {
                    Name = MENUITEM_RECENT_FILES_PREFIX + i.ToString()
                };
                try
                {
                    string fileName = Path.GetFileName(recentFiles.ElementAt(i));
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        mi.Header = fileName;
                        MenuFile.Items.Add(mi);
                        mi.Click += OpenWorkbookFile;
                    }
                }
                catch { };
            }
        }

        /// <summary>
        /// Get position of a chessboard's square
        /// </summary>
        /// <param name="sq">XY coordinates of the square</param>
        /// <returns></returns>
        private Point GetSquareTopLeftPoint(SquareCoords sq)
        {
            double left = squareSize * sq.Xcoord + UiImgMainChessboard.Margin.Left;
            double top = squareSize * (7 - sq.Ycoord) + UiImgMainChessboard.Margin.Top;

            return new Point(left, top);
        }

        /// <summary>
        /// Get the center point of a chessboard's square
        /// </summary>
        /// <param name="sq">XY coordinates of the square</param>
        /// <returns></returns>
        private Point GetSquareCenterPoint(SquareCoords sq)
        {
            Point pt = GetSquareTopLeftPoint(sq);
            return new Point(pt.X + squareSize / 2, pt.Y + squareSize / 2);
        }

        /// <summary>
        /// Get Image control at a given point.
        /// Invoked when the user clicks on the chessboard
        /// preparing to make a move.
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        private Image GetImageFromPoint(Point p)
        {
            SquareCoords sq = ClickedSquare(p);
            if (sq == null)
            {
                return null;
            }
            else
            {
                return MainChessBoard.GetPieceImage(sq.Xcoord, sq.Ycoord, true);
            }
        }

        /// <summary>
        /// Get XY coordinates of clicked square.
        /// </summary>
        /// <param name="p">Location of the clicked point.</param>
        /// <returns></returns>
        private SquareCoords ClickedSquare(Point p)
        {
            double squareSide = UiImgMainChessboard.Width / 8.0;
            double xPos = p.X / squareSide;
            double yPos = p.Y / squareSide;

            if (xPos > 0 && xPos < 8 && yPos > 0 && yPos < 8)
            {
                return new SquareCoords((int)xPos, 7 - (int)yPos);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// The user pressed the mouse button over the board.
        /// If it is a left button it indicates the commencement of
        /// an intended move.
        /// TODO: fix so this is only called when indded the click occured on the board.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {

            Point clickedPoint = e.GetPosition(UiImgMainChessboard);
            SquareCoords sq = ClickedSquare(clickedPoint);

            if (sq == null)
            {
                return;
            }

            if (Evaluation.IsRunning)
            {
                BoardCommentBox.ShowFlashAnnouncement("Engine evaluation in progress!");
                return;
            }

            if (e.ChangedButton == MouseButton.Left)
            {
                if (sq != null)
                {
                    SquareCoords sqNorm = new SquareCoords(sq);
                    if (MainChessBoard.IsFlipped)
                    {
                        sqNorm.Flip();
                    }

                    if (CanMovePiece(sqNorm))
                    {
                        DraggedPiece.isDragInProgress = true;
                        DraggedPiece.Square = sq;

                        DraggedPiece.ImageControl = GetImageFromPoint(clickedPoint);
                        Point ptLeftTop = GetSquareTopLeftPoint(sq);
                        DraggedPiece.ptDraggedPieceOrigin = ptLeftTop;

                        // for the remainder, we need absolute point
                        clickedPoint.X += UiImgMainChessboard.Margin.Left;
                        clickedPoint.Y += UiImgMainChessboard.Margin.Top;
                        DraggedPiece.ptStartDragLocation = clickedPoint;


                        Point ptCenter = GetSquareCenterPoint(sq);

                        Canvas.SetLeft(DraggedPiece.ImageControl, ptLeftTop.X + (clickedPoint.X - ptCenter.X));
                        Canvas.SetTop(DraggedPiece.ImageControl, ptLeftTop.Y + (clickedPoint.Y - ptCenter.Y));
                    }
                }
            }
        }

        private bool CanMovePiece(SquareCoords sqNorm)
        {
            PieceColor pieceColor = MainChessBoard.GetPieceColor(sqNorm);

            // in the Manual Review, the color of the piece on the main board must match the side on the move in the selected position
            if (LearningMode.CurrentMode == LearningMode.Mode.MANUAL_REVIEW)
            {
                TreeNode nd;
                if (ActiveLine.GetPlyCount() > 1)
                {
                    nd = ActiveLine.GetSelectedTreeNode();
                }
                else
                {
                    nd = Workbook.Nodes[0];
                }
                if (pieceColor != PieceColor.None && pieceColor == nd.ColorToMove)
                    return true;
                else
                    return false;
            }
            else if (LearningMode.CurrentMode == LearningMode.Mode.ENGINE_GAME && EngineGame.CurrentState == EngineGame.GameState.USER_THINKING
                || LearningMode.CurrentMode == LearningMode.Mode.TRAINING && TrainingState.CurrentMode == TrainingState.Mode.AWAITING_USER_TRAINING_MOVE && !TrainingState.IsBrowseActive)
            {
                if (EngineGame.GetPieceColor(sqNorm) == EngineGame.ColorToMove)
                    return true;
                else
                    return false;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Depending on the Application and/or Training mode,
        /// this may have been the user completing a move.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (DraggedPiece.isDragInProgress)
            {
                DraggedPiece.isDragInProgress = false;
                Point clickedPoint = e.GetPosition(UiImgMainChessboard);
                SquareCoords targetSquare = ClickedSquare(clickedPoint);
                if (targetSquare == null)
                {
                    // just put the piece back
                    Canvas.SetLeft(DraggedPiece.ImageControl, DraggedPiece.ptDraggedPieceOrigin.X);
                    Canvas.SetTop(DraggedPiece.ImageControl, DraggedPiece.ptDraggedPieceOrigin.Y);
                }
                else
                {
                    // double check that we are legitimately making a move
                    if (LearningMode.CurrentMode == LearningMode.Mode.ENGINE_GAME && EngineGame.CurrentState == EngineGame.GameState.USER_THINKING
                        || LearningMode.CurrentMode == LearningMode.Mode.TRAINING && TrainingState.CurrentMode == TrainingState.Mode.AWAITING_USER_TRAINING_MOVE
                        || LearningMode.CurrentMode == LearningMode.Mode.MANUAL_REVIEW)
                    {
                        UserMoveProcessor.FinalizeUserMove(targetSquare);
                    }
                    else
                    {
                        ReturnDraggedPiece(false);
                    }
                }
                Canvas.SetZIndex(DraggedPiece.ImageControl, 0);
            }
        }

        /// <summary>
        /// Shows a GUI element allowing the user 
        /// to select the piece to promote to.
        /// </summary>
        /// <param name="normTarget">Normalized propmotion square coordinates
        /// i.e. 0 is for Black and 7 is for White promotion.</param>
        /// <returns></returns>
        public PieceType GetUserPromoSelection(SquareCoords normTarget)
        {
            bool whitePromotion = normTarget.Ycoord == 7;
            PromotionDialog dlg = new PromotionDialog(whitePromotion);

            Point pos = CalculatePromoDialogLocation(normTarget, whitePromotion);
            dlg.Left = pos.X;
            dlg.Top = pos.Y;
            dlg.ShowDialog();

            return dlg.SelectedPiece;
        }

        /// <summary>
        /// Given the promotion square in the normalized
        /// form (i.e. ignoring a possible chessboard flip),
        /// works out the Left and Top position of the Promotion
        /// dialog.
        /// The dialog should fit entirely within the board and its boarders and should
        /// nicely overlap with the promotion square.
        /// </summary>
        /// <param name="normTarget"></param>
        /// <returns></returns>
        private Point CalculatePromoDialogLocation(SquareCoords normTarget, bool whitePromotion)
        {
            //TODO: this is far from ideal.
            // We need to find a better way of calulating the position against
            // the chessboard
            Point leftTop = new Point();
            if (!MainChessBoard.IsFlipped)
            {
                leftTop.X = ChessForgeMain.Left + ChessForgeMain.UiImgMainChessboard.Margin.Left + 20 + normTarget.Xcoord * 80;
                if (whitePromotion)
                {
                    leftTop.Y = ChessForgeMain.Top + ChessForgeMain.UiImgMainChessboard.Margin.Top + 40 + (7 - normTarget.Ycoord) * 80;
                }
                else
                {
                    leftTop.Y = ChessForgeMain.Top + ChessForgeMain.UiImgMainChessboard.Margin.Top + 40 + (3 - normTarget.Ycoord) * 80;
                }
            }
            else
            {
                leftTop.X = ChessForgeMain.Left + ChessForgeMain.UiImgMainChessboard.Margin.Left + 20 + (7 - normTarget.Xcoord) * 80;
                if (whitePromotion)
                {
                    leftTop.X = ChessForgeMain.Top + ChessForgeMain.UiImgMainChessboard.Margin.Top + 40 + (normTarget.Ycoord - 4) * 80;
                }
                else
                {
                    leftTop.X = ChessForgeMain.Top + ChessForgeMain.UiImgMainChessboard.Margin.Top + 40 + (normTarget.Ycoord) * 80;
                }
            }

            return leftTop;
        }

        /// <summary>
        /// Completes a castling move. King would have already been moved.
        /// </summary>
        /// <param name="move"></param>
        public void MoveCastlingRook(string move)
        {
            SquareCoords orig = null;
            SquareCoords dest = null;
            switch (move)
            {
                case "e1g1":
                    orig = !MainChessBoard.IsFlipped ? new SquareCoords(7, 0) : new SquareCoords(0, 7);
                    dest = !MainChessBoard.IsFlipped ? new SquareCoords(5, 0) : new SquareCoords(2, 7);
                    break;
                case "e8g8":
                    orig = !MainChessBoard.IsFlipped ? new SquareCoords(7, 7) : new SquareCoords(0, 0);
                    dest = !MainChessBoard.IsFlipped ? new SquareCoords(5, 7) : new SquareCoords(2, 0);
                    break;
                case "e1c1":
                    orig = !MainChessBoard.IsFlipped ? new SquareCoords(0, 0) : new SquareCoords(7, 7);
                    dest = !MainChessBoard.IsFlipped ? new SquareCoords(3, 0) : new SquareCoords(4, 7);
                    break;
                case "e8c8":
                    orig = !MainChessBoard.IsFlipped ? new SquareCoords(0, 7) : new SquareCoords(7, 0);
                    dest = !MainChessBoard.IsFlipped ? new SquareCoords(3, 7) : new SquareCoords(4, 0);
                    break;
            }

            MovePiece(orig, dest);
        }

        /// <summary>
        /// Moving a piece from square to square.
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="dest"></param>
        private void MovePiece(SquareCoords orig, SquareCoords dest)
        {
            if (orig == null || dest == null)
                return;

            MainChessBoard.GetPieceImage(dest.Xcoord, dest.Ycoord, true).Source = MainChessBoard.GetPieceImage(orig.Xcoord, orig.Ycoord, true).Source;
            MainChessBoard.GetPieceImage(orig.Xcoord, orig.Ycoord, true).Source = null;
        }

        /// <summary>
        /// Returns the dragged piece's Image control to
        /// the square it started from.
        /// If clearImage == true, the image in the control
        /// will be cleared (e.g. because the move was successfully
        /// executed and the image has been transferred to the control
        /// on the target square.
        /// </summary>
        /// <param name="clearImage"></param>
        public void ReturnDraggedPiece(bool clearImage)
        {
            if (clearImage)
            {
                DraggedPiece.ImageControl.Source = null;
            }
            Canvas.SetLeft(DraggedPiece.ImageControl, DraggedPiece.ptDraggedPieceOrigin.X);
            Canvas.SetTop(DraggedPiece.ImageControl, DraggedPiece.ptDraggedPieceOrigin.Y);
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            Point clickedPoint = e.GetPosition(UiImgMainChessboard);

            if (DraggedPiece.isDragInProgress)
            {
                Canvas.SetZIndex(DraggedPiece.ImageControl, 10);
                clickedPoint.X += UiImgMainChessboard.Margin.Left;
                clickedPoint.Y += UiImgMainChessboard.Margin.Top;

                Canvas.SetLeft(DraggedPiece.ImageControl, clickedPoint.X - squareSize / 2);
                Canvas.SetTop(DraggedPiece.ImageControl, clickedPoint.Y - squareSize / 2);
            }

        }

        /// <summary>
        /// Move animation requested as part of auto-replay.
        /// As such we need to flip the coordinates if
        /// the board is flipped.
        /// </summary>
        /// <param name="move"></param>
        public void RequestMoveAnimation(MoveUI move)
        {
            SquareCoords origin = MainChessBoard.FlipCoords(move.Origin);
            SquareCoords destination = MainChessBoard.FlipCoords(move.Destination);
            AnimateMove(origin, destination);
        }

        /// <summary>
        /// Caller must handle a possible flipped stated of the board.
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="destination"></param>
        private void AnimateMove(SquareCoords origin, SquareCoords destination)
        {
            // caller already accounted for a possible flipped board so call with ignoreFlip = true
            Image img = MainChessBoard.GetPieceImage(origin.Xcoord, origin.Ycoord, true);
            MoveAnimation.Piece = img;
            MoveAnimation.Origin = origin;
            MoveAnimation.Destination = destination;

            Canvas.SetZIndex(img, 1);

            Point orig = GetSquareTopLeftPoint(origin);
            Point dest = GetSquareTopLeftPoint(destination);

            TranslateTransform trans = new TranslateTransform();
            if (img.RenderTransform != null)
                img.RenderTransform = trans;

            DoubleAnimation animX = new DoubleAnimation(0, dest.X - orig.X, TimeSpan.FromMilliseconds(MoveAnimation.MoveDuration));
            DoubleAnimation animY = new DoubleAnimation(0, dest.Y - orig.Y, TimeSpan.FromMilliseconds(MoveAnimation.MoveDuration));

            LearningMode.CurrentTranslateTransform = trans;
            LearningMode.CurrentAnimationX = animX;
            LearningMode.CurrentAnimationY = animY;

            animX.Completed += new EventHandler(MoveAnimationCompleted);
            trans.BeginAnimation(TranslateTransform.XProperty, animX);
            trans.BeginAnimation(TranslateTransform.YProperty, animY);

        }

        /// <summary>
        /// Stops move animation if there is one in progress.
        /// </summary>
        public void StopMoveAnimation()
        {
            // TODO Apparently, there are 2 methods to stop animation.
            // Method 1 below keeps the animated image at the spot it was when the stop request came.
            // Method 2 returns it to the initial position.
            // Neither works fully to our satisfaction. They seem to not be exiting immediately and are leaving some garbage
            // behind which prevents us from immediatey changing the speed of animation on user's request 
            if (LearningMode.CurrentAnimationX != null && LearningMode.CurrentAnimationY != null && LearningMode.CurrentTranslateTransform != null)
            {
                // *** Method 1.
                //AppState.CurrentAnimationX.BeginTime = null;
                //AppState.CurrentAnimationY.BeginTime = null;
                //AppState.CurrentTranslateTransform.BeginAnimation(TranslateTransform.XProperty, AppState.CurrentAnimationX);
                //AppState.CurrentTranslateTransform.BeginAnimation(TranslateTransform.YProperty, AppState.CurrentAnimationY);

                // *** Method 2.
                LearningMode.CurrentTranslateTransform.BeginAnimation(TranslateTransform.XProperty, null);
                LearningMode.CurrentTranslateTransform.BeginAnimation(TranslateTransform.YProperty, null);
            }
        }

        /// <summary>
        /// Called when animation completes.
        /// The coords saved in the MoveAnimation object
        /// are absolute as a possible flipped state of the board was
        /// taken into account at the start fo the animation.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MoveAnimationCompleted(object sender, EventArgs e)
        {
            LearningMode.CurrentTranslateTransform = null;
            LearningMode.CurrentAnimationX = null;
            LearningMode.CurrentAnimationY = null;

            MainChessBoard.GetPieceImage(MoveAnimation.Destination.Xcoord, MoveAnimation.Destination.Ycoord, true).Source = MoveAnimation.Piece.Source;

            Point orig = GetSquareTopLeftPoint(MoveAnimation.Origin);
            //_pieces[AnimationOrigin.Xcoord, AnimationOrigin.Ycoord].Source = AnimationPiece.Source;

            Canvas.SetLeft(MainChessBoard.GetPieceImage(MoveAnimation.Origin.Xcoord, MoveAnimation.Origin.Ycoord, true), orig.X);
            Canvas.SetTop(MainChessBoard.GetPieceImage(MoveAnimation.Origin.Xcoord, MoveAnimation.Origin.Ycoord, true), orig.Y);

            //TODO: there should be a better way than having to recreate the image control.
            //   but it seems the image would no longer show (tested when not removing
            //   the image from the origin square, the image won't show seemingly due to
            // RenderTransfrom being set.)
            //
            // This seems to work but re-shows the last moved piece on its origin square???
            // _pieces[AnimationOrigin.Xcoord, AnimationOrigin.Ycoord].RenderTransform = null;
            //

            Image old = MainChessBoard.GetPieceImage(MoveAnimation.Origin.Xcoord, MoveAnimation.Origin.Ycoord, true);
            MainCanvas.Children.Remove(old);
            MainChessBoard.SetPieceImage(new Image(), MoveAnimation.Origin.Xcoord, MoveAnimation.Origin.Ycoord, true);
            MainCanvas.Children.Add(MainChessBoard.GetPieceImage(MoveAnimation.Origin.Xcoord, MoveAnimation.Origin.Ycoord, true));
            Canvas.SetLeft(MainChessBoard.GetPieceImage(MoveAnimation.Origin.Xcoord, MoveAnimation.Origin.Ycoord, true), squareSize * MoveAnimation.Origin.Xcoord + UiImgMainChessboard.Margin.Left);
            Canvas.SetTop(MainChessBoard.GetPieceImage(MoveAnimation.Origin.Xcoord, MoveAnimation.Origin.Ycoord, true), squareSize * (7 - MoveAnimation.Origin.Ycoord) + UiImgMainChessboard.Margin.Top);

            ActiveLineReplay.PrepareNextMoveForAnimation(ActiveLineReplay.LastAnimatedMoveIndex, false);
        }

        private void OpenWorkbookFile(object sender, RoutedEventArgs e)
        {
            if (ChangeAppModeWarning(LearningMode.Mode.MANUAL_REVIEW))
            {
                string menuItemName = ((MenuItem)e.Source).Name;
                string path = Configuration.GetRecentFile(menuItemName);
                ReadWorkbookFile(path, false);
            }
        }

        /// <summary>
        /// Loads a new Workbook file.
        /// If the application is NOT in the IDLE mode, it will ask the user:
        /// - to close/cancel/save/put_aside the current tree (TODO: TO BE IMPLEMENTED)
        /// - stop a game against the engine, if in progress
        /// - stop any engine evaluations if in progress (TODO: it should be allowed to continue background analysis in a separate low-pri thread).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Menu_LoadWorkbook(object sender, RoutedEventArgs e)
        {
            if (ChangeAppModeWarning(LearningMode.Mode.MANUAL_REVIEW))
            {
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Multiselect = false,
                    Filter = "Workbooks (*.chf); PGN (*.pgn)|*.chf;*.pgn|All files (*.*)|*.*"
                };

                string initDir;
                if (!string.IsNullOrEmpty(Configuration.LastOpenDirectory))
                {
                    initDir = Configuration.LastOpenDirectory;
                }
                else
                {
                    initDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                }

                openFileDialog.InitialDirectory = initDir;

                bool? result;

                try
                {
                    result = openFileDialog.ShowDialog();
                }
                catch
                {
                    openFileDialog.InitialDirectory = "";
                    result = openFileDialog.ShowDialog();
                };

                if (result == true)
                {
                    Configuration.LastOpenDirectory = Path.GetDirectoryName(openFileDialog.FileName);
                    ReadWorkbookFile(openFileDialog.FileName, false);
                }
            }
        }

        /// <summary>
        /// Invoked from the menu item File->Close Workbook
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnCloseWorkbook_Click(object sender, RoutedEventArgs e)
        {
            WorkbookManager.AskToSaveWorkbook();
        }

        /// <summary>
        /// Returns true if user accept the change. of mode.
        /// </summary>
        /// <param name="newMode"></param>
        /// <returns></returns>
        private bool ChangeAppModeWarning(LearningMode.Mode newMode)
        {
            if (LearningMode.CurrentMode == LearningMode.Mode.IDLE)
            {
                // it is a fresh state, no need for any warnings
                return true;
            }

            bool result = false;
            // we may not be changing the mode, but changing
            // the variation tree we are working with.
            if (LearningMode.CurrentMode == LearningMode.Mode.MANUAL_REVIEW && newMode == LearningMode.Mode.MANUAL_REVIEW)
            {
                // TODO: ask what to do with the current tree
                // abandon, save, put aside
                result = true;
            }
            else if (LearningMode.CurrentMode != LearningMode.Mode.MANUAL_REVIEW && newMode == LearningMode.Mode.MANUAL_REVIEW)
            {
                switch (LearningMode.CurrentMode)
                {
                    case LearningMode.Mode.ENGINE_GAME:
                        if (MessageBox.Show("Cancel Game?", "Game with the Computer is in Progress", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                            result = true;
                        break;
                    default:
                        result = true;
                        break;
                }
            }
            else
            {
                return true;
            }

            return result;
        }

        /// <summary>
        /// Recreates the "Recent Files" menu items by
        /// removing the exisiting ones and inserting
        /// ones corresponding to what's in the configuration file.
        /// </summary>
        public void RecreateRecentFilesMenuItems()
        {
            List<object> itemsToRemove = new List<object>();

            for (int i = 0; i < MenuFile.Items.Count; i++)
            {
                if (MenuFile.Items[i] is MenuItem item)
                {
                    if (item.Name.StartsWith(MENUITEM_RECENT_FILES_PREFIX))
                    {
                        itemsToRemove.Add(item);
                    }
                }
            }

            foreach (MenuItem item in itemsToRemove.Cast<MenuItem>())
            {
                MenuFile.Items.Remove(item);
            }

            CreateRecentFilesMenuItems();
        }

        /// <summary>
        /// Reads in the file and builds internal Variation Tree
        /// structures for the entire content.
        /// The Chess Forge file will have a .chf extension.
        /// However, we also read .pgn files (with the intent
        /// to save them as .chf files)
        /// </summary>
        /// <param name="fileName"></param>
        private async void ReadWorkbookFile(string fileName, bool isLastOpen)
        {
            try
            {
                if (!WorkbookManager.CheckFileExists(fileName, isLastOpen))
                {
                    return;
                }

                AppStateManager.RestartInIdleMode(false);

                // WorkbookFilePath is reset to "" in the above call!
                AppStateManager.WorkbookFilePath = fileName;

                bool isOrigPgn = false;
                if (AppStateManager.WorkbookFileType == AppStateManager.FileType.PGN)
                {
                    isOrigPgn = true;
                }

                await Task.Run(() =>
                {
                    BoardCommentBox.ReadingFile();
                });

                AppStateManager.WorkbookFilePath = fileName;
                AppStateManager.UpdateAppTitleBar();

                Workbook = new WorkbookTree();
                BookmarkManager.ClearBookmarksGui();
                UiRtbWorkbookView.Document.Blocks.Clear();

                if (AppStateManager.WorkbookFileType == AppStateManager.FileType.CHF)
                {
                    string workbookText = File.ReadAllText(fileName);
                    PgnGameParser pgnGame = new PgnGameParser(workbookText, Workbook, out bool isMulti, true);
                }
                else
                {
                    int gameCount = WorkbookManager.ReadPgnFile(fileName);
                    if (gameCount == 0)
                    {
                        MessageBox.Show("No games to process.", "Input File", MessageBoxButton.OK, MessageBoxImage.Error);
                        AppStateManager.WorkbookFilePath = "";
                        AppStateManager.UpdateAppTitleBar();
                        return;
                    }
                }

                BoardCommentBox.ShowWorkbookTitle();

                if (Workbook.TrainingSide == PieceColor.None)
                {
                    ShowWorkbookOptionsDialog();
                }

                if (Workbook.TrainingSide == PieceColor.White && MainChessBoard.IsFlipped || Workbook.TrainingSide == PieceColor.Black && !MainChessBoard.IsFlipped)
                {
                    MainChessBoard.FlipBoard();
                }

                // If this is not a CHF file, ask the user to save the converted file.
                bool recentFilesProcessed = false;
                if (AppStateManager.WorkbookFileType != AppStateManager.FileType.CHF)
                {
                    if (WorkbookManager.SaveWorkbookToNewFile(fileName, true))
                    {
                        recentFilesProcessed = true;
                    }
                }

                if (!recentFilesProcessed)
                {
                    WorkbookManager.UpdateRecentFilesList(fileName);
                }

                BoardCommentBox.ShowWorkbookTitle();

                _workbookView = new WorkbookView(UiRtbWorkbookView.Document, this);
                _trainingBrowseRichTextBuilder = new WorkbookView(UiRtbTrainingBrowse.Document, this);
                if (Workbook.Nodes.Count == 0)
                {
                    Workbook.CreateNew();
                }
                else
                {
                    Workbook.BuildLines();
                }
                UiTabWorkbook.Focus();

                _workbookView.BuildFlowDocumentForWorkbook();
                if (Workbook.Bookmarks.Count == 0 && isOrigPgn)
                {
                    var res = AskToGenerateBookmarks();
                    if (res == MessageBoxResult.Yes)
                    {
                        Workbook.GenerateBookmarks();
                        UiTabBookmarks.Focus();
                        AppStateManager.IsDirty = true;
                    }
                }

                TreeNode firstNode = Workbook.GetFirstNodeInMainLine();
                int startingNode = firstNode == null ? 0 : firstNode.NodeId;
                string startLineId = Workbook.GetDefaultLineIdForNode(startingNode);
                SetActiveLine(startLineId, startingNode);
                UiRtbWorkbookView.Focus();

                SetupDataInTreeView();

                BookmarkManager.ShowBookmarks();

                SelectLineAndMoveInWorkbookViews(startLineId, ActiveLine.GetSelectedPlyNodeIndex());

                LearningMode.ChangeCurrentMode(LearningMode.Mode.MANUAL_REVIEW);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error processing input file", MessageBoxButton.OK, MessageBoxImage.Error);
                AppStateManager.RestartInIdleMode();
            }
        }

        /// <summary>
        /// Rebuilds the entire Workbook View
        /// </summary>
        public void RebuildWorkbookView()
        {
            _workbookView.BuildFlowDocumentForWorkbook();
        }

        /// <summary>
        /// Obtains the current ActiveLine's LineId and move,
        /// and asks other view to select / re-select.
        /// This is needed e.g. when the WorkbookTree is rebuilt after
        /// adding nodes.
        /// </summary>
        public void RefreshSelectedActiveLineAndNode()
        {
            TreeNode nd = ActiveLine.GetSelectedTreeNode();
            string lineId = ActiveLine.GetLineId();
            SelectLineAndMoveInWorkbookViews(lineId, ActiveLine.GetSelectedPlyNodeIndex());
        }

        /// <summary>
        /// Adds a new Node to the Workbook View,
        /// avoiding the full rebuild (performance).
        /// This can only be done "safely" if we are adding a move to a leaf.
        /// </summary>
        /// <param name="nd"></param>
        public void AddNewNodeToWorkbookView(TreeNode nd)
        {
            _workbookView.AddNewNode(nd);
        }

        public void SelectLineAndMoveInWorkbookViews(string lineId, int index)
        {
            TreeNode nd = ActiveLine.GetNodeAtIndex(index);
            _workbookView.SelectLineAndMove(lineId, nd.NodeId);
            _lvWorkbookTable_SelectLineAndMove(lineId, nd.NodeId);
            if (Evaluation.CurrentMode == EvaluationManager.Mode.CONTINUOUS)
            {
                EvaluateActiveLineSelectedPositionEx();
            }
        }

        private MessageBoxResult AskToGenerateBookmarks()
        {
            return MessageBox.Show("Would you like to auto-select positions for training?",
                "No Bookmarks in this Workbook", MessageBoxButton.YesNo, MessageBoxImage.Question);
        }

        public void SetActiveLine(string lineId, int selectedNodeId, bool displayPosition = true)
        {
            ObservableCollection<TreeNode> line = Workbook.SelectLine(lineId);
            SetActiveLine(line, selectedNodeId, displayPosition);
        }

        public void DisplayPosition(BoardPosition position)
        {
            MainChessBoard.DisplayPosition(position);
        }

        public void RemoveMoveSquareColors()
        {
            MainChessBoard.RemoveMoveSquareColors();
        }

        /// <summary>
        /// Sets data and selection for the Active Line
        /// </summary>
        /// <param name="line"></param>
        /// <param name="selectedNodeId"></param>
        /// <param name="displayPosition"></param>
        public void SetActiveLine(ObservableCollection<TreeNode> line, int selectedNodeId, bool displayPosition = true)
        {
            ActiveLine.SetNodeList(line);

            if (selectedNodeId >= 0)
            {
                TreeNode nd = ActiveLine.GetNodeFromId(selectedNodeId);
                if (selectedNodeId > 0)
                {
                    ActiveLine.SelectPly((int)nd.Parent.MoveNumber, nd.Parent.ColorToMove);
                }
                if (displayPosition)
                {
                    MainChessBoard.DisplayPosition(nd.Position);
                }
                if (Evaluation.CurrentMode == EvaluationManager.Mode.CONTINUOUS)
                {
                    EvaluateActiveLineSelectedPositionEx();
                }
            }
        }

        /// <summary>
        /// Appends a new node to the Active Line.
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="displayPosition"></param>
        public void AppendNodeToActiveLine(TreeNode nd, bool displayPosition = true)
        {
            if (nd.NodeId > 0)
            {
                ActiveLine.Line.AddPlyAndMove(nd);
                ActiveLine.SelectPly((int)nd.Parent.MoveNumber, nd.Parent.ColorToMove);
                if (displayPosition)
                {
                    MainChessBoard.DisplayPosition(nd.Position);
                }
            }
        }

        /// <summary>
        /// Tidy up upon application closing.
        /// Stop all timers, write out any logs,
        /// save any unsaved bits.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChessForgeMain_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            AppLog.Message("Application Closing");

            StopEvaluation();
            if (AppStateManager.WorkbookFileType == AppStateManager.FileType.PGN)
            {
                WorkbookManager.PromptUserToConvertPGNToCHF();
            }
            else
            {
                if (AppStateManager.CurrentLearningMode != LearningMode.Mode.IDLE
                    && AppStateManager.IsDirty || (Workbook != null && Workbook.HasTrainingMoves()))
                {
                    WorkbookManager.PromptAndSaveWorkbook(false, true);
                }
            }
            Timers.StopAll();

            DumpDebugLogs(false);
            Configuration.WriteOutConfiguration();
        }

        /// <summary>
        /// Writes out all logs.
        /// If userRequested == true, this was requested via the menu
        /// and we dump everything with distinct file names.
        /// Otherwise we only dump app and engine logs, ovewriting previous
        /// logs.
        /// </summary>
        /// <param name="userRequested"></param>
        public void DumpDebugLogs(bool userRequested)
        {
            string distinct = null;

            if (userRequested)
            {
                distinct = "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                AppLog.DumpWorkbookTree(DebugUtils.BuildLogFileName(App.AppPath, "wktree", distinct), Workbook);
                AppLog.DumpStatesAndTimers(DebugUtils.BuildLogFileName(App.AppPath, "timest", distinct));
            }

            try
            {
                AppLog.Dump(DebugUtils.BuildLogFileName(App.AppPath, "applog", distinct));
                EngineLog.Dump(DebugUtils.BuildLogFileName(App.AppPath, "engine", distinct));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Dump logs exception: " + ex.Message, "DEBUG", MessageBoxButton.OK, MessageBoxImage.Stop);
            }
        }


        public void DumpDebugStates()
        {
            string distinct = "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
            AppLog.DumpStatesAndTimers(DebugUtils.BuildLogFileName(App.AppPath, "timest", distinct));
        }

        /// <summary>
        /// The user requested evaluation of the currently selected move.
        /// Check if there is an item currently selected. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_EvaluatePosition(object sender, RoutedEventArgs e)
        {
            AppStateManager.SetCurrentEvaluationMode(EvaluationManager.Mode.CONTINUOUS);
            EvaluateActiveLineSelectedPositionEx();
        }

        private void EvaluateActiveLineSelectedPositionEx()
        {
            // stop the timer to prevent showing garbage after position is set but engine has not received our commands yet
            EngineMessageProcessor.RequestPositionEvaluation(ActiveLine.GetSelectedPlyNodeIndex(), Configuration.EngineMpv, 0);
        }

        private void MenuItem_EvaluateLine(object sender, RoutedEventArgs e)
        {
            // a defensive check
            if (ActiveLine.GetPlyCount() == 0)
            {
                return;
            }

            if (Evaluation.CurrentMode != EvaluationManager.Mode.IDLE)
            {
                StopEvaluation();
            }

            int idx = ActiveLine.GetSelectedPlyNodeIndex();
            Evaluation.PositionIndex = idx > 0 ? idx : 1;

            // we will start with the first move of the active line
            if (EngineMessageProcessor.IsEngineAvailable)
            {
                AppStateManager.SetCurrentEvaluationMode(EvaluationManager.Mode.LINE);
                UiDgActiveLine.SelectedCells.Clear();
                EngineMessageProcessor.RequestMoveEvaluation(Evaluation.PositionIndex);
            }
            else
            {
                MessageBox.Show("Chess Engine is not available.", "Move Evaluation Failure", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }


        public void UpdateLastMoveTextBox(TreeNode nd)
        {
            string moveTxt = MoveUtils.BuildSingleMoveText(nd, true);

            UpdateLastMoveTextBox(moveTxt);
        }

        public void UpdateLastMoveTextBox(int posIndex)
        {
            string moveTxt = Evaluation.Position.MoveNumber.ToString()
                    + (Evaluation.Position.ColorToMove == PieceColor.Black ? "." : "...")
                    + ActiveLine.GetNodeAtIndex(posIndex).LastMoveAlgebraicNotation;

            UpdateLastMoveTextBox(moveTxt);
        }

        public void UpdateLastMoveTextBox(string moveTxt)
        {
            UiLblMoveUnderEval.Dispatcher.Invoke(() =>
            {
                UiLblMoveUnderEval.Content = moveTxt;
            });
        }

        public void ResetEvaluationProgressBar()
        {
            EngineLinesGUI.ResetEvaluationProgressBar();
        }

        /// <summary>
        /// If in training mode, we want to keep the evaluation lines
        /// visible in the comment box, and display the response moves
        /// with their line evaluations in the Training tab.
        /// </summary>
        public void MoveEvaluationFinishedInTraining()
        {
            AppStateManager.ShowMoveEvaluationControls(false, true);
            UiTrainingView.ShowEvaluationResult();
        }

        /// <summary>
        /// The user requests a game against the computer starting from the current position
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Menu_PlayEngine(object sender, RoutedEventArgs e)
        {
            // check that there is a move selected in the _dgMainLineView so
            // that we have somewhere to start
            TreeNode nd = ActiveLine.GetSelectedTreeNode();
            if (nd != null)
            {
                StartEngineGame(nd, false);
                if (nd.ColorToMove == PieceColor.White && !MainChessBoard.IsFlipped || nd.ColorToMove == PieceColor.Black && MainChessBoard.IsFlipped)
                {
                    MainChessBoard.FlipBoard();
                }
            }
            else
            {
                MessageBox.Show("Select the move from which to start.", "Computer Game", MessageBoxButton.OK);
            }
        }

        private void UiMnciExitEngineGame_Click(object sender, RoutedEventArgs e)
        {
            StopEngineGame();
        }


        private void UiMnciBookmarkPosition_Click(object sender, RoutedEventArgs e)
        {
            int moveIndex = ActiveLine.GetSelectedPlyNodeIndex();
            if (moveIndex < 0)
            {
                return;
            }
            else
            {
                int posIndex = moveIndex;
                TreeNode nd = ActiveLine.GetNodeAtIndex(posIndex);
                BookmarkManager.AddBookmark(nd);
                UiTabBookmarks.Focus();
            }
        }

        /// <summary>
        /// This method will start a game vs the engine.
        /// It will be called in one of two possible contexts:
        /// either the game was requested from MANUAL_REVIEW
        /// or during TRAINING.
        /// If the latter, then the EngineGame has already been
        /// constructed and we start from the last move/ply.
        /// </summary>
        /// <param name="startNode"></param>
        public void StartEngineGame(TreeNode startNode, bool IsTraining)
        {
            UiImgMainChessboard.Source = ChessBoards.ChessBoardGreen;

            LearningMode.ChangeCurrentMode(LearningMode.Mode.ENGINE_GAME);

            // TODO: should make a call to SetupGUI for game, instead
            AppStateManager.ShowMoveEvaluationControls(false, false);

            EngineGame.InitializeGameObject(startNode, true, IsTraining);
            UiDgEngineGame.ItemsSource = EngineGame.Line.MoveList;

            if (startNode.ColorToMove == PieceColor.White)
            {
                if (!MainChessBoard.IsFlipped)
                {
                    MainChessBoard.FlipBoard();
                }
            }

            EngineMessageProcessor.RequestEngineMove(startNode.Position);
        }

        /// <summary>
        /// This method will be invoked periodically by the 
        /// timer checking for the completion of user moves.
        /// The user can make moves in 2 contexts:
        /// 1. a game against the engine (in this case EngineGame.State 
        /// should already be set to ENGINE_THINKING)
        /// 2. a user entered the move as part of training and we will
        /// provide them a feedback based on the content of the workbook.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        public void CheckForUserMoveTimerEvent(object source, ElapsedEventArgs e)
        {
            if (TrainingState.IsTrainingInProgress && LearningMode.CurrentMode != LearningMode.Mode.ENGINE_GAME)
            {
                if ((TrainingState.CurrentMode == TrainingState.Mode.USER_MOVE_COMPLETED))
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        Timers.Stop(AppTimers.TimerId.CHECK_FOR_USER_MOVE);
                        UiTrainingView.ReportLastMoveVsWorkbook();
                    });
                }
            }
            else // this is a game user vs engine then
            {
                // check if the user move was completed and if so request engine move
                if (EngineGame.CurrentState == EngineGame.GameState.ENGINE_THINKING)
                {
                    Timers.Stop(AppTimers.TimerId.CHECK_FOR_USER_MOVE);
                    EngineMessageProcessor.RequestEngineMove(EngineGame.GetLastPosition());
                }
            }
        }

        /// <summary>
        /// Reset controls and restore selection in the ActiveLine
        /// control.
        /// We are going back to the MANUAL REVIEW mode
        /// so Active Line view will be shown.
        /// </summary>
        public void StopEngineGame()
        {
            Timers.Stop(AppTimers.TimerId.EVALUATION_LINE_DISPLAY);

            ResetEngineThinkingGUI();

            MainChessBoard.RemoveMoveSquareColors();

            Evaluation.Reset();
            EngineMessageProcessor.StopEngineEvaluation();
            LearningMode.ChangeCurrentMode(LearningMode.Mode.MANUAL_REVIEW);
            EngineGame.CurrentState = EngineGame.GameState.IDLE;
            Timers.Stop(AppTimers.TimerId.CHECK_FOR_USER_MOVE);

            AppStateManager.MainWin.Workbook.BuildLines();
            RebuildWorkbookView();

            AppStateManager.SetupGuiForCurrentStates();

            ActiveLine.DisplayPositionForSelectedCell();
            AppStateManager.SwapCommentBoxForEngineLines(false);
            BoardCommentBox.RestoreTitleMessage();
        }

        public void ResetEngineThinkingGUI()
        {
            UiPbEngineThinking.Dispatcher.Invoke(() =>
            {
                UiPbEngineThinking.Visibility = Visibility.Hidden;
                UiPbEngineThinking.Minimum = 0;

                int moveTime = AppStateManager.CurrentLearningMode == LearningMode.Mode.ENGINE_GAME ?
                    Configuration.EngineMoveTime : Configuration.EngineEvaluationTime;
                UiPbEngineThinking.Maximum = moveTime;
                UiPbEngineThinking.Value = 0;
            });

        }

        /// <summary>
        /// Ensure that Workbook Tree's ListView allows
        /// mouse wheel scrolling.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiScvWorkbookTable_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scv = (ScrollViewer)sender;
            scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta);
            e.Handled = true;
        }

        /// <summary>
        /// A key pressed event has been received.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiRtbWorkbookFull_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Hand it off to the ActiveLine view.
            // In the future we may want to handle some key strokes here
            // but for now we will respond to whatever the ActiveLine view will request.
            ActiveLine.PreviewKeyDown(sender, e);
        }

        /// <summary>
        /// Re-directs the user to the bookmark page where they can
        /// select a bookmarked position.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_StartTraining(object sender, RoutedEventArgs e)
        {
            if (AppStateManager.CurrentLearningMode == LearningMode.Mode.ENGINE_GAME)
            {
                StopEngineGame();
            }
            else if (Evaluation.IsRunning)
            {
                EngineMessageProcessor.StopEngineEvaluation();
            }

            LearningMode.ChangeCurrentMode(LearningMode.Mode.MANUAL_REVIEW);
            AppStateManager.SetCurrentEvaluationMode(EvaluationManager.Mode.IDLE);

            AppStateManager.SwapCommentBoxForEngineLines(false);

            UiTabBookmarks.Focus();
        }

        /// <summary>
        /// A request from the menu to start training at the currently selected position.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnciStartTrainingHere_Click(object sender, RoutedEventArgs e)
        {
            // do some housekeeping just in case
            if (AppStateManager.CurrentLearningMode == LearningMode.Mode.ENGINE_GAME)
            {
                StopEngineGame();
            }
            else if (Evaluation.IsRunning)
            {
                EngineMessageProcessor.StopEngineEvaluation();
            }

            TreeNode nd = ActiveLine.GetSelectedTreeNode();
            if (nd != null)
            {
                if (!BookmarkManager.IsBookmarked(nd.NodeId))
                {
                    if (MessageBox.Show("Do you want to bookmark this move?", "Training", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        BookmarkManager.AddBookmark(nd);
                    }
                }
                SetAppInTrainingMode(nd);
            }
            else
            {
                MessageBox.Show("No move selected to start training from.", "Training", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// Starts a training session from the specified bookmark position.
        /// </summary>
        /// <param name="bookmarkIndex"></param>
        public void SetAppInTrainingMode(int bookmarkIndex)
        {
            if (bookmarkIndex >= Workbook.Bookmarks.Count)
            {
                return;
            }

            TreeNode startNode = Workbook.Bookmarks[bookmarkIndex].Node;
            SetAppInTrainingMode(startNode);

        }

        /// <summary>
        /// Starts a training session from the specified Node.
        /// </summary>
        /// <param name="startNode"></param>
        public void SetAppInTrainingMode(TreeNode startNode)
        {
            // Set up the training mode
            StopEvaluation();
            LearningMode.ChangeCurrentMode(LearningMode.Mode.TRAINING);
            TrainingState.IsTrainingInProgress = true;
            TrainingState.CurrentMode = TrainingState.Mode.AWAITING_USER_TRAINING_MOVE;
            Evaluation.ChangeCurrentMode(EvaluationManager.Mode.IDLE);

            LearningMode.TrainingSide = startNode.ColorToMove;
            MainChessBoard.DisplayPosition(startNode.Position);

            _trainingBrowseRichTextBuilder.BuildFlowDocumentForWorkbook(startNode.NodeId);

            UiTrainingView = new TrainingView(UiRtbTrainingProgress.Document, this);
            UiTrainingView.Initialize(startNode);

            if (LearningMode.TrainingSide == PieceColor.Black && !MainChessBoard.IsFlipped
                || LearningMode.TrainingSide == PieceColor.White && MainChessBoard.IsFlipped)
            {
                MainChessBoard.FlipBoard();
            }

            AppStateManager.ShowMoveEvaluationControls(false, false);
            BoardCommentBox.TrainingSessionStart();

            // The Line display is the same as when playing a game against the computer 
            EngineGame.InitializeGameObject(startNode, false, false);
            UiDgEngineGame.ItemsSource = EngineGame.Line.MoveList;
            Timers.Start(AppTimers.TimerId.CHECK_FOR_USER_MOVE);
        }

        /// <summary>
        /// Exits the Training session, if confirmed by the user.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_StopTraining(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Exit the training session?", "Chess Forge Training", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                if (WorkbookManager.PromptAndSaveWorkbook(false))
                {
                    EngineMessageProcessor.StopEngineEvaluation();
                    Evaluation.Reset();

                    TrainingState.IsTrainingInProgress = false;
                    MainChessBoard.RemoveMoveSquareColors();
                    LearningMode.ChangeCurrentMode(LearningMode.Mode.MANUAL_REVIEW);
                    AppStateManager.SetupGuiForCurrentStates();

                    ActiveLine.DisplayPositionForSelectedCell();
                    AppStateManager.SwapCommentBoxForEngineLines(false);
                    BoardCommentBox.RestoreTitleMessage();
                }
            }
        }

        /// <summary>
        /// Flips the main chess board upside down.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_FlipBoard(object sender, RoutedEventArgs e)
        {
            MainChessBoard.FlipBoard();
        }

        public void InvokeRequestWorkbookResponse(object source, ElapsedEventArgs e)
        {
            UiTrainingView.RequestWorkbookResponse();
        }

        public void ShowTrainingProgressPopupMenu(object source, ElapsedEventArgs e)
        {
            UiTrainingView.ShowPopupMenu();
        }

        public void FlashAnnouncementTimeUp(object source, ElapsedEventArgs e)
        {
            BoardCommentBox.HideFlashAnnouncement();
        }

        /// <summary>
        /// Hides the floating board if shown
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiRtbTrainingProgress_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            ShowFloatingChessboard(false);
        }

        public void ShowFloatingChessboard(bool visible)
        {
            this.Dispatcher.Invoke(() =>
            {
                UiVbFloatingChessboard.Visibility = visible ? Visibility.Visible : Visibility.Hidden;
            });
        }

        /// <summary>
        /// Handles a context menu event to start training
        /// from the recently clicked bookmark.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnTrainFromBookmark_Click(object sender, RoutedEventArgs e)
        {
            if (BookmarkManager.ClickedIndex >= 0)
            {
                SetAppInTrainingMode(BookmarkManager.ClickedIndex);
            }
        }

        /// <summary>
        /// Handles left click on any of the Bookmark chessboards.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiCnvBookmark_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Canvas canvas)
            {
                BookmarkManager.ChessboardClickedEvent(canvas.Name, _cmBookmarks, e);
            }
        }

        /// <summary>
        /// A bookmark view was clicked somewhere.
        /// We disable the bookmark menu in case the click was not on a bookmark.
        /// The event is then handled by a bookmark handler, if the click was on
        /// a bookmark and the menus will be enabled accordingly.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BookmarkGrid_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            BookmarkManager.ClickedIndex = -1;
            BookmarkManager.EnableBookmarkMenus(_cmBookmarks, false);
        }

        /// <summary>
        /// Allows the user to add a bookmark by re-directing them to the Workbook view 
        /// and advising on the procedure. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnAddBookmark_Click(object sender, RoutedEventArgs e)
        {
            UiTabWorkbook.Focus();
            MessageBox.Show("Right-click a move and select \"Add to Bookmarks\" from the popup-menu", "Chess Forge Training", MessageBoxButton.OK);
        }

        /// <summary>
        /// Handles a mouse click in the Workbook's grid. At this point
        /// we disable node specific menu items in case no node was clicked.
        /// If a node was clicked, it will be corrected when the event is handled
        /// in the Run's OnClick handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WorkbookGrid_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            _workbookView.LastClickedNodeId = -1;
            _workbookView.EnableWorkbookMenus(UiCmnWorkbookRightClick, false);
        }

        /// <summary>
        /// Adds the lst clicked node to bookmarks.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnWorkbookSelectAsBookmark_Click(object sender, RoutedEventArgs e)
        {
            int ret = BookmarkManager.AddBookmark(_workbookView.LastClickedNodeId);
            if (ret == 1)
            {
                MessageBox.Show("This bookmark already exists.", "Training Bookmarks", MessageBoxButton.OK);
            }
            else if (ret == -1)
            {
                MessageBox.Show("Failed to add the bookmark.", "Training Bookmarks", MessageBoxButton.OK);
            }
            else
            {
                AppStateManager.IsDirty = true;
                // AppStateManager.SaveWorkbookFile();
                UiTabBookmarks.Focus();
            }
        }

        /// <summary>
        /// Adds the last click node, and all its siblings to bookmarks
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnWorkbookBookmarkAlternatives_Click(object sender, RoutedEventArgs e)
        {
            int ret = BookmarkManager.AddAllSiblingsToBookmarks(_workbookView.LastClickedNodeId);
            if (ret == 1)
            {
                MessageBox.Show("Bookmarks already exist.", "Training Bookmarks", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            else if (ret == -1)
            {
                MessageBox.Show("Failed to add the bookmarks.", "Training Bookmarks", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            else
            {
                AppStateManager.IsDirty = true;
                UiTabBookmarks.Focus();
            }
        }

        private void UiMnGenerateBookmark_Click(object sender, RoutedEventArgs e) { BookmarkManager.GenerateBookmarks(); }

        private void UiMnDeleteBookmark_Click(object sender, RoutedEventArgs e) { BookmarkManager.DeleteBookmark(); }

        private void UiMnDeleteAllBookmarks_Click(object sender, RoutedEventArgs e) { BookmarkManager.DeleteAllBookmarks(); }

        private void UiMnTrainRestartGame_Click(object sender, RoutedEventArgs e)
        {
            UiTrainingView.RestartGameAfter(sender, e);
        }

        private void UiMnTrainSwitchToWorkbook_Click(object sender, RoutedEventArgs e)
        {
            UiTrainingView.RollbackToWorkbookMove();
        }

        private void UiMnTrainEvalMove_Click(object sender, RoutedEventArgs e)
        {
            UiTrainingView.RequestMoveEvaluation();
        }


        private void UiMnTrainEvalLine_Click(object sender, RoutedEventArgs e)
        {
            UiTrainingView.RequestLineEvaluation();
        }

        /// <summary>
        /// Restarts training from the same position/bookmark
        /// that we started the current session with.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnTrainRestartTraining_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Restart the training session?", "Chess Forge Training", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                SetAppInTrainingMode(TrainingState.StartPosition);
            }
        }

        private void ViewActiveLine_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            ActiveLine.PreviewKeyDown(sender, e);
        }

        private void ViewActiveLine_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            ActiveLine.PreviewMouseDown(sender, e);
        }

        private void ViewActiveLine_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ActiveLine.MouseDoubleClick(sender, e);
        }

        /// <summary>
        /// Auto-replays the current Active Line on a menu request.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_ReplayLine(object sender, RoutedEventArgs e)
        {
            ActiveLine.ReplayLine(0);
        }

        /// <summary>
        /// Advise the Training View that the engine made a move
        /// while playing a training game against the user.
        /// </summary>
        public void EngineTrainingGameMoveMade()
        {
            this.Dispatcher.Invoke(() =>
            {
                UiTrainingView.EngineMoveMade();
            });
        }

        /// <summary>
        /// Shade the "from" and "to" squares of the passed move.
        /// </summary>
        /// <param name="engCode"></param>
        public void ColorMoveSquares(string engCode)
        {
            this.Dispatcher.Invoke(() =>
            {
                MainChessBoard.RemoveMoveSquareColors();

                MoveUtils.EngineNotationToCoords(engCode, out SquareCoords sqOrig, out SquareCoords sqDest);
                MainChessBoard.ColorMoveSquare(sqOrig.Xcoord, sqOrig.Ycoord, true);
                MainChessBoard.ColorMoveSquare(sqDest.Xcoord, sqDest.Ycoord, false);
            });
        }

        private void UiTabItemTrainingBrowse_GotFocus(object sender, RoutedEventArgs e) { AppStateManager.SetupGuiForTrainingBrowseMode(); }

        private void UiRtbTrainingProgress_GotFocus(object sender, RoutedEventArgs e) { AppStateManager.SetupGuiForTrainingProgressMode(); }

        private void UiImgLeftArrow_PreviewMouseDown(object sender, MouseButtonEventArgs e) { BookmarkManager.PageDown(); }

        private void UiImgRightArrow_PreviewMouseDown(object sender, MouseButtonEventArgs e) { BookmarkManager.PageUp(); }

        private void UiDgEngineGame_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void UiDgEngineGame_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void UiBtnExitTraining_Click(object sender, RoutedEventArgs e)
        {
            MenuItem_StopTraining(sender, e);
        }

        private void UiBtnExitGame_Click(object sender, RoutedEventArgs e)
        {
            StopEngineGame();
        }

        private void UiMnPromoteLine_Click(object sender, RoutedEventArgs e)
        {
            _workbookView.PromoteCurrentLine();
        }

        private void UiMnDeleteMovesFromHere_Click(object sender, RoutedEventArgs e)
        {
            _workbookView.DeleteRemainingMoves();
        }

        private void UiMnWorkbookSave_Click(object sender, RoutedEventArgs e)
        {
            WorkbookManager.PromptAndSaveWorkbook(true);
        }

        private void UiMnWorkbookSaveAs_Click(object sender, RoutedEventArgs e)
        {
            WorkbookManager.SaveWorkbookToNewFile(AppStateManager.WorkbookFilePath, false);
        }

        /// <summary>
        /// View->Select Engine... menu item clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnSelectEngine_Click(object sender, RoutedEventArgs e)
        {
            string searchPath = Path.GetDirectoryName(Configuration.EngineExePath);
            if (!string.IsNullOrEmpty(Configuration.SelectEngineExecutable(searchPath)))
            {
                ReloadEngine();
            }
        }

        /// <summary>
        /// Stops and restarts the engine.
        /// </summary>
        /// <returns></returns>
        public bool ReloadEngine()
        {
            EngineMessageProcessor.StopEngineService();
            EngineMessageProcessor.CreateEngineService(this, _isDebugMode);

            bool engineStarted = EngineMessageProcessor.StartEngineService();
            if (!engineStarted)
            {
                MessageBox.Show("Failed to load the engine. Move evaluation will not be available.", "Chess Engine Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// User clicked Help->About
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnHelpAbout_Click(object sender, RoutedEventArgs e)
        {
            AboutBoxDialog dlg = new AboutBoxDialog();
            dlg.ShowDialog();
        }

        /// <summary>
        /// The user requested to edit Workbook options.
        /// The dialog will be shown.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnWorkbookOptions_Click(object sender, RoutedEventArgs e)
        {
            if (AppStateManager.CurrentLearningMode != LearningMode.Mode.IDLE)
            {
                ShowWorkbookOptionsDialog();
            }
        }

        /// <summary>
        /// Shows the Workbook options dialog.
        /// </summary>
        /// <returns></returns>
        private bool ShowWorkbookOptionsDialog()
        {
            WorkbookOptionsDialog dlg = new WorkbookOptionsDialog(Workbook)
            {
                Left = ChessForgeMain.Left + 100,
                Top = ChessForgeMain.Top + 100,
                Topmost = true
            };
            dlg.ShowDialog();

            if (dlg.ExitOK)
            {
                Workbook.TrainingSide = dlg.TrainingSide;
                Workbook.Title = dlg.WorkbookTitle;
                AppStateManager.SaveWorkbookFile();
                MainChessBoard.FlipBoard(Workbook.TrainingSide);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// The user requested to edit Application options.
        /// The dialog will be shown.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnApplicationOptions_Click(object sender, RoutedEventArgs e)
        {
            if (AppStateManager.CurrentLearningMode != LearningMode.Mode.IDLE)
            {
                ShowApplicationOptionsDialog();
            }
        }

        /// <summary>
        /// Shows the Application Options dialog.
        /// </summary>
        private void ShowApplicationOptionsDialog()
        {
            AppOptionsDialog dlg = new AppOptionsDialog
            {
                Left = ChessForgeMain.Left + 100,
                Top = ChessForgeMain.Top + 100,
                Topmost = true
            };
            dlg.ShowDialog();

            if (dlg.ExitOK)
            {
                if (dlg.ChangedEnginePath)
                    Configuration.WriteOutConfiguration();
                if (dlg.ChangedEnginePath)
                {
                    ReloadEngine();
                }
            }
        }

        /// <summary>
        /// Creates a new Workbook.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnNewWorkbook_Click(object sender, RoutedEventArgs e)
        {
            if (!WorkbookManager.AskToCloseWorkbook())
            {
                return;
            }

            // prepare document
            AppStateManager.RestartInIdleMode(false);
            Workbook = new WorkbookTree();
            _workbookView = new WorkbookView(UiRtbWorkbookView.Document, this);
            _trainingBrowseRichTextBuilder = new WorkbookView(UiRtbTrainingBrowse.Document, this);

            // ask for the options
            if (!ShowWorkbookOptionsDialog())
            {
                // user abandoned
                return;
            }

            if (!WorkbookManager.SaveWorkbookToNewFile(null, false))
            {
                AppStateManager.RestartInIdleMode(false);
                return;
            }

            BoardCommentBox.ShowWorkbookTitle();

            LearningMode.ChangeCurrentMode(LearningMode.Mode.MANUAL_REVIEW);

            AppStateManager.SetupGuiForCurrentStates();
            Workbook.CreateNew();
            UiTabWorkbook.Focus();
            _workbookView.BuildFlowDocumentForWorkbook();
            int startingNode = 0;
            string startLineId = Workbook.GetDefaultLineIdForNode(startingNode);
            SetActiveLine(startLineId, startingNode);
        }

        /// <summary>
        /// The user requested export of the Workbook to PGN.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnExportPgn_Click(object sender, RoutedEventArgs e)
        {
            WorkbookManager.SaveWorkbookToPgn();
        }

        /// <summary>
        /// Stops any evaluation that is currently happening.
        /// Resets evaluation state and adjusts the GUI accordingly. 
        /// </summary>
        public void StopEvaluation()
        {
            EngineMessageProcessor.StopEngineEvaluation();

            Evaluation.Reset();
            AppStateManager.ResetEvaluationControls();
            AppStateManager.ShowMoveEvaluationControls(false, true);
            AppStateManager.SetupGuiForCurrentStates();
            Timers.StopAll();
        }

        /// <summary>
        /// Handles the Evaluation toggle being clicked while in the ON mode.
        /// Any evaluation in progress will be stopped.
        /// to CONTINUOUS.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiImgEngineOn_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            EngineMessageProcessor.StopEngineEvaluation();

            UiImgEngineOff.Visibility = Visibility.Visible;
            UiImgEngineOn.Visibility = Visibility.Collapsed;

            StopEvaluation();

            e.Handled = true;
        }

        /// <summary>
        /// Handles the Evaluation toggle being clicked while in the OFF mode.
        /// If in MANUAL REVIEW mode, sets the current evaluation mode
        /// to CONTINUOUS.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiImgEngineOff_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (AppStateManager.CurrentLearningMode == LearningMode.Mode.MANUAL_REVIEW)
            {
                AppStateManager.SetCurrentEvaluationMode(EvaluationManager.Mode.CONTINUOUS);
                UiImgEngineOff.Visibility = Visibility.Collapsed;
                UiImgEngineOn.Visibility = Visibility.Visible;
                Timers.Start(AppTimers.TimerId.EVALUATION_LINE_DISPLAY);
                EvaluateActiveLineSelectedPositionEx();
            }

            e.Handled = true;
        }
    }
}