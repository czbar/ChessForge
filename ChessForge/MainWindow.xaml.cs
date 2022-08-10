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

        public EngineEvaluationGUI EngineLinesGUI;
        AnimationState MoveAnimation = new AnimationState();
        public EvaluationState Evaluation;

        // The main chessboard of the application
        private ChessBoard MainChessBoard;

        public ChessBoard FloatingChessBoard;

        /// <summary>
        /// The RichTextBox based comment box
        /// underneath the main chessbaord.
        /// </summary>
        public CommentBox BoardCommentBox;

        public GameReplay ActiveLineReplay;

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
            Evaluation = new EvaluationState(this);

            InitializeComponent();
            SoundPlayer.Initialize();

            BoardCommentBox = new CommentBox(UiRtbBoardComment.Document, this);
            ActiveLine = new ActiveLineManager(UiDgActiveLine, this);

            EngineLinesGUI = new EngineEvaluationGUI(this, UiTbEngineLines, UiPbEngineThinking, Evaluation);
            Timers = new AppTimers(EngineLinesGUI, this);

            Configuration.Initialize(this);
            Configuration.StartDirectory = Directory.GetCurrentDirectory();
            Configuration.ReadConfigurationFile();
            MoveAnimation.MoveDuration = Configuration.MoveSpeed;
            if (Configuration.MainWinPos.IsValid)
            {
                this.Left = Configuration.MainWinPos.Left;
                this.Top = Configuration.MainWinPos.Top;
                this.Width = Configuration.MainWinPos.Width;
                this.Height = Configuration.MainWinPos.Height;
            }

            // main chess board
            MainChessBoard = new ChessBoard(MainCanvas, UiImgMainChessboard, null, true);
            FloatingChessBoard = new ChessBoard(_cnvFloat, _imgFloatingBoard, null, true);

            BookmarkManager.InitBookmarksGui(this);

            ActiveLineReplay = new GameReplay(this, MainChessBoard, BoardCommentBox);


            UiSldReplaySpeed.Value = Configuration.MoveSpeed;
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

            AppStateManager.CurrentLearningMode = LearningMode.Mode.IDLE;
            AppStateManager.SetupGuiForCurrentStates();

            Timers.Start(AppTimers.TimerId.APP_START);
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
                        bool engineStarted = EngineMessageProcessor.Start();
                        Timers.Start(AppTimers.TimerId.APP_START);
                        if (!engineStarted)
                        {
                            MessageBox.Show("Failed to load the engine. Move evaluation will not be available.", "Chess Engine Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }

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
                MenuItem mi = new MenuItem();
                mi.Name = MENUITEM_RECENT_FILES_PREFIX + i.ToString();
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
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Evaluation.IsRunning)
            {
                BoardCommentBox.ShowFlashAnnouncement("Engine evaluation in progress!");
                return;
            }

            if (e.ChangedButton == MouseButton.Left)
            {
                Point clickedPoint = e.GetPosition(UiImgMainChessboard);
                SquareCoords sq = ClickedSquare(clickedPoint);

                if (sq != null)
                {
                    SquareCoords sqNorm = new SquareCoords(sq);
                    if (MainChessBoard.IsFlipped)
                    {
                        sqNorm.Flip();
                    }

                    if (sq != null && EngineGame.GetPieceColor(sqNorm) == EngineGame.ColorToMove)
                    {
                        if (LearningMode.CurrentMode == LearningMode.Mode.ENGINE_GAME && EngineGame.CurrentState == EngineGame.GameState.USER_THINKING
                            || LearningMode.CurrentMode == LearningMode.Mode.TRAINING && TrainingState.CurrentMode == TrainingState.Mode.AWAITING_USER_TRAINING_MOVE && !TrainingState.IsBrowseActive)
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
                        || LearningMode.CurrentMode == LearningMode.Mode.TRAINING && TrainingState.CurrentMode == TrainingState.Mode.AWAITING_USER_TRAINING_MOVE)
                    {
                        FinalizeUserMove(targetSquare);
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
        /// Invoked after the user made a move on the chessboard
        /// and released the mouse.
        /// </summary>
        /// <param name="targetSquare"></param>
        private void FinalizeUserMove(SquareCoords targetSquare)
        {
            // if the move is valid swap image at destination 
            // and clear image at origin
            if (targetSquare.Xcoord != DraggedPiece.Square.Xcoord || targetSquare.Ycoord != DraggedPiece.Square.Ycoord)
            {
                StringBuilder moveEngCode = new StringBuilder();
                SquareCoords origSquareNorm = new SquareCoords(DraggedPiece.Square);
                SquareCoords targetSquareNorm = new SquareCoords(targetSquare);
                if (MainChessBoard.IsFlipped)
                {
                    origSquareNorm.Flip();
                    targetSquareNorm.Flip();
                }

                bool isPromotion = false;
                PieceType promoteTo = PieceType.None;

                if (EngineGame.GetPieceType(origSquareNorm) == PieceType.Pawn
                    && (EngineGame.ColorToMove == PieceColor.White && targetSquareNorm.Ycoord == 7)
                    || (EngineGame.ColorToMove == PieceColor.Black && targetSquareNorm.Ycoord == 0))
                {
                    isPromotion = true;
                    promoteTo = GetUserPromoSelection(targetSquareNorm);
                }

                // do not process if this was a canceled promotion
                if ((!isPromotion || promoteTo != PieceType.None) && EngineGame.GetPieceColor(targetSquareNorm) != EngineGame.ColorToMove)
                {
                    moveEngCode.Append((char)(origSquareNorm.Xcoord + (int)'a'));
                    moveEngCode.Append((char)(origSquareNorm.Ycoord + (int)'1'));
                    moveEngCode.Append((char)(targetSquareNorm.Xcoord + (int)'a'));
                    moveEngCode.Append((char)(targetSquareNorm.Ycoord + (int)'1'));

                    // add promotion char if this is a promotion
                    if (isPromotion)
                    {
                        moveEngCode.Append(FenParser.PieceToFenChar[promoteTo]);
                    }
                    bool isCastle;
                    TreeNode nd;
                    if (EngineGame.ProcessUserMove(moveEngCode.ToString(), out nd, out isCastle))
                    {
                        // NOTE now EngineGame has a new move added so the 
                        // other side is on move!
                        ImageSource imgSrc = DraggedPiece.ImageControl.Source;
                        if (isPromotion)
                        {
                            if (EngineGame.ColorToMove == PieceColor.Black)
                            {
                                imgSrc = ChessBoard.GetWhitePieceRegImg(promoteTo);
                            }
                            else
                            {
                                imgSrc = ChessBoard.GetBlackPieceRegImg(promoteTo);
                            }
                        }
                        MainChessBoard.GetPieceImage(targetSquare.Xcoord, targetSquare.Ycoord, true).Source = imgSrc;

                        ReturnDraggedPiece(true);
                        if (isCastle)
                        {
                            MoveCastlingRook(moveEngCode.ToString());
                        }

                        SoundPlayer.PlayMoveSound(nd.LastMoveAlgebraicNotation);
                        BoardCommentBox.GameMoveMade(nd, true);
                        ColorMoveSquares(nd.LastMoveEngineNotation);
                    }
                    else
                    {
                        ReturnDraggedPiece(false);
                    }
                }
                else
                {
                    ReturnDraggedPiece(false);
                }
            }
            else
            {
                ReturnDraggedPiece(false);
            }
        }


        /// <summary>
        /// Shows a GUI element allow the user 
        /// to select the piece to promote to.
        /// </summary>
        /// <param name="normTarget">Normalized propmotion square coordinates
        /// i.e. 0 is for Black and 7 is for White promotion.</param>
        /// <returns></returns>
        private PieceType GetUserPromoSelection(SquareCoords normTarget)
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
        private void MoveCastlingRook(string move)
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
        /// If claerImage == true, the image in the control
        /// will be cleared (e.g. because the move was successfully
        /// executed and the image has been transferred to the control
        /// on the target square.
        /// </summary>
        /// <param name="clearImage"></param>
        private void ReturnDraggedPiece(bool clearImage)
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
        public void MakeMove(MoveUI move)
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
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Multiselect = false;
                openFileDialog.Filter = "ChessForge Workbooks (*.chf)|*.chf|PGN Game files (*.pgn)|*.pgn|All files (*.*)|*.*";

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
            if (AppStateManager.WorkbookFileType == AppStateManager.FileType.PGN)
            {
                PromptUserToConvertPGNToCHF();
            }
            else
            {
                AppStateManager.SaveWorkbookFile(true);
            }
            AppStateManager.RestartInIdleMode();
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
                        if (MessageBox.Show("Cancel Game", "Game with the Computer is in Progress", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
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

        private void RecreateRecentFilesMenuItems()
        {
            List<object> itemsToRemove = new List<object>();

            for (int i = 0; i < MenuFile.Items.Count; i++)
            {
                if (MenuFile.Items[i] is MenuItem)
                {
                    MenuItem item = (MenuItem)MenuFile.Items[i];
                    if (item.Name.StartsWith(MENUITEM_RECENT_FILES_PREFIX))
                    {
                        itemsToRemove.Add(item);
                    }
                }
            }

            foreach (MenuItem item in itemsToRemove)
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
                if (!File.Exists(fileName))
                {
                    if (isLastOpen)
                    {
                        MessageBox.Show("Most recent file " + fileName + " could not be found.", "File Not Found", MessageBoxButton.OK);
                    }
                    else
                    {
                        MessageBox.Show("File " + fileName + " could not be found.", "File Not Found", MessageBoxButton.OK);
                    }
                    Configuration.RemoveFromRecentFiles(fileName);
                    RecreateRecentFilesMenuItems();
                    return;
                }

                await Task.Run(() =>
                {
                    BoardCommentBox.ReadingFile();
                });

                System.Threading.Thread.Sleep(1000);
                AppStateManager.WorkbookFilePath = fileName;
                this.Title = APP_NAME + " - " + Path.GetFileName(fileName);

                string workbookText = File.ReadAllText(fileName);

                Workbook = new WorkbookTree();
                BookmarkManager.ClearBookmarksGui();
                UiRtbWorkbookView.Document.Blocks.Clear();
                PgnGameParser pgnGame = new PgnGameParser(workbookText, Workbook, true);

                BoardCommentBox.ShowWorkbookTitle(Workbook.Title);

                if (Workbook.TrainingSide == PieceColor.None)
                {
                    TrainingSideDialog dlg = new TrainingSideDialog();
                    dlg.Left = ChessForgeMain.Left + 100;
                    dlg.Top = ChessForgeMain.Top + 100;
                    dlg.Topmost = true;
                    dlg.WorkbookTitle = Workbook.Title;
                    dlg.ShowDialog();
                    Workbook.TrainingSide = dlg.SelectedSide;
                    Workbook.Title = dlg.WorkbookTitle;
                }

                if (Workbook.TrainingSide == PieceColor.White && MainChessBoard.IsFlipped || Workbook.TrainingSide == PieceColor.Black && !MainChessBoard.IsFlipped)
                {
                    MainChessBoard.FlipBoard();
                }

                //
                // If this is not a CHF file, ask the user to save the converted file.
                //
                if (AppStateManager.WorkbookFileType != AppStateManager.FileType.CHF)
                {
                    SaveConvertedWorkbooFile(fileName);
                }

                Configuration.AddRecentFile(fileName);
                RecreateRecentFilesMenuItems();

                BoardCommentBox.ShowWorkbookTitle(Workbook.Title);

                _workbookView = new WorkbookView(UiRtbWorkbookView.Document, this);
                _trainingBrowseRichTextBuilder = new WorkbookView(UiRtbTrainingBrowse.Document, this);
                //UiTrainingView = new TrainingView(UiRtbTrainingProgress.Document, this);

                Workbook.BuildLines();
                UiTabWorkbook.Focus();

                _workbookView.BuildFlowDocumentForWorkbook();
                if (Workbook.Bookmarks.Count == 0)
                {
                    var res = AskToGenerateBookmarks();
                    if (res == MessageBoxResult.Yes)
                    {
                        Workbook.GenerateBookmarks();
                        UiTabBookmarks.Focus();
                        AppStateManager.SaveWorkbookFile();
                    }
                }

                int startingNode = 0;
                string startLineId = Workbook.GetDefaultLineIdForNode(startingNode);
                SetActiveLine(startLineId, startingNode);

                SetupDataInTreeView();

                BookmarkManager.ShowBookmarks();

                SelectLineAndMoveInWorkbookViews(startLineId, startingNode);

                Configuration.LastWorkbookFile = fileName;

                LearningMode.ChangeCurrentMode(LearningMode.Mode.MANUAL_REVIEW);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error processing input file", MessageBoxButton.OK, MessageBoxImage.Error);
                AppStateManager.RestartInIdleMode();
            }
        }

        /// <summary>
        /// Prompts the user to decide whether they want to convert/save 
        /// PGN file as a CHF Workbook.
        /// Invoked when the app or the Workbook is being closed.
        /// </summary>
        /// <returns></returns>
        private int PromptUserToConvertPGNToCHF()
        {
            bool hasBookmarks = Workbook.Bookmarks.Count > 0;

            string msg = "Your edits " + (hasBookmarks ? "and bookmarks " : "")
                + "will be lost unless you save this Workbook as a ChessForge (.chf) file.\n\n Convert and save?";
            if (MessageBox.Show(msg, "Chess Forge File Closing", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                SaveConvertedWorkbooFile(AppStateManager.WorkbookFilePath);
                return 0;
            }
            else
            {
                return -1;
            }
        }

        /// <summary>
        /// Allows the user to save the PGN files as CHF
        /// thus allowing editing etc.
        /// </summary>
        /// <param name="fileName"></param>
        private void SaveConvertedWorkbooFile(string fileName)
        {
            SaveFileDialog saveDlg = new SaveFileDialog();
            saveDlg.Filter = "chf Workbook files (*.chf)|*.chf";
            saveDlg.Title = " Save Workbook converted from " + Path.GetFileName(fileName);

            saveDlg.FileName = Path.GetFileNameWithoutExtension(fileName) + ".chf";
            saveDlg.OverwritePrompt = true;
            if (saveDlg.ShowDialog() == true)
            {
                fileName = saveDlg.FileName;
                AppStateManager.WorkbookFilePath = fileName;
                AppStateManager.SaveWorkbookFile();
                Configuration.LastWorkbookFile = fileName;
            }
        }

        public void SelectLineAndMoveInWorkbookViews(string lineId, int nodeId)
        {
            _workbookView.SelectLineAndMove(lineId, nodeId);
            _lvWorkbookTable_SelectLineAndMove(lineId, nodeId);
        }

        private MessageBoxResult AskToGenerateBookmarks()
        {
            return MessageBox.Show("Would you like to auto-select positions for training?",
                "No Bookmarks in this Workbook", MessageBoxButton.YesNo, MessageBoxImage.Question);
        }

        public void SetActiveLine(string lineId, int selectedNodeId)
        {
            ObservableCollection<TreeNode> line = Workbook.SelectLine(lineId);
            SetActiveLine(line, selectedNodeId);
        }

        public void DisplayPosition(BoardPosition position)
        {
            MainChessBoard.DisplayPosition(position);
        }

        public void SetActiveLine(ObservableCollection<TreeNode> line, int selectedNodeId)
        {
            ActiveLine.SetNodeList(line);

            if (selectedNodeId > 0)
            {
                TreeNode nd = ActiveLine.GetNodeFromId(selectedNodeId);
                ActiveLine.SelectPly((int)nd.Parent.MoveNumber, nd.Parent.ColorToMove);
                MainChessBoard.DisplayPosition(nd.Position);
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
            if (AppStateManager.WorkbookFileType == AppStateManager.FileType.PGN)
            {
                PromptUserToConvertPGNToCHF();
            }
            else
            {
                AppStateManager.SaveWorkbookFile(true);
            }
            Timers.StopAll();
            AppLog.Dump();
            EngineLog.Dump();
            Configuration.WriteOutConfiguration();
        }

        private void sliderReplaySpeed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Configuration.MoveSpeed = (int)e.NewValue;
            MoveAnimation.MoveDuration = Configuration.MoveSpeed;
        }

        /// <summary>
        /// The user requested evaluation of the currently selected move.
        /// Check if there is an item currently selected. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_EvaluatePosition(object sender, RoutedEventArgs e)
        {
            if (Evaluation.CurrentMode != EvaluationState.EvaluationMode.IDLE)
            {
                // there is an evaluation running right now so do not allow another one.
                // This menu item should be disabled if that's the case so we should never
                // end up here but just in case ...
                MessageBox.Show("Cannot start an evaluation while another one in progress.", "Move Evaluation", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            int moveIndex = ActiveLine.GetSelectedPlyNodeIndex();
            if (moveIndex < 0)
            {
                MessageBox.Show("Select a move to evaluate.", "Move Evaluation", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                // the position we want to show is the next element
                int posIndex = moveIndex + 1;
                // check that the engine is available
                if (EngineMessageProcessor.IsEngineAvailable())
                {
                    // make an extra defensive check
                    if (posIndex < ActiveLine.GetPlyCount())
                    {
                        AppStateManager.SetCurrentEvaluationMode(EvaluationState.EvaluationMode.SINGLE_MOVE);
                        EngineMessageProcessor.RequestMoveEvaluation(posIndex);
                    }
                }
                else
                {
                    MessageBox.Show("Chess Engine is not available.", "Move Evaluation Failure", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void MenuItem_EvaluateLine(object sender, RoutedEventArgs e)
        {
            // a defensive check
            if (ActiveLine.GetPlyCount() == 0)
            {
                return;
            }

            if (Evaluation.CurrentMode != EvaluationState.EvaluationMode.IDLE)
            {
                // there is an evaluation running right now so do not allow another one.
                MessageBox.Show("Cannot start an evaluation while another one in progress.", "Move Evaluation", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Evaluation.PositionIndex = 1;
            // we will start with the first move of the active line
            if (EngineMessageProcessor.IsEngineAvailable())
            {
                AppStateManager.SetCurrentEvaluationMode(EvaluationState.EvaluationMode.LINE);
                UiDgActiveLine.SelectedCells.Clear();
                EngineMessageProcessor.RequestMoveEvaluation(Evaluation.PositionIndex);
            }
            else
            {
                MessageBox.Show("Chess Engine is not avalable.", "Move Evaluation Failure", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                PlayComputer(nd, false);
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

        /// <summary>
        /// This method will start a game vs the engine.
        /// It will be called in one of two possible contexts:
        /// either the game was requested from MANUAL_REVIEW
        /// or during TRAINING.
        /// If the latter, then the EngineGame has already been
        /// constructed and we start from the last move/ply.
        /// </summary>
        /// <param name="startNode"></param>
        public void PlayComputer(TreeNode startNode, bool IsTraining)
        {
            UiImgMainChessboard.Source = ChessBoards.ChessBoardGreen;

            LearningMode.ChangeCurrentMode(LearningMode.Mode.ENGINE_GAME);

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
        internal void ProcessUserMoveEvent(object source, ElapsedEventArgs e)
        {
            if (TrainingState.IsTrainingInProgress && LearningMode.CurrentMode != LearningMode.Mode.ENGINE_GAME)
            {
                if ((TrainingState.CurrentMode & TrainingState.Mode.USER_MOVE_COMPLETED) != 0)
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
                    EngineMessageProcessor.RequestEngineMove(EngineGame.GetCurrentPosition());
                }
            }
        }

        /// <summary>
        /// Reset controls and restore selection in the ActiveLine
        /// control.
        /// We are going back to the MANUAL REVIEW mode
        /// so Active Line view will be shown.
        /// </summary>
        private void StopEngineGame()
        {
            Timers.Stop(AppTimers.TimerId.EVALUATION_LINE_DISPLAY);

            UiPbEngineThinking.Dispatcher.Invoke(() =>
            {
                UiPbEngineThinking.Visibility = Visibility.Hidden;
                UiPbEngineThinking.Minimum = 0;
                UiPbEngineThinking.Maximum = (int)(Configuration.EngineEvaluationTime);
                UiPbEngineThinking.Value = 0;
            });

            MainChessBoard.RemoveMoveSquareColors();

            Evaluation.Reset();
            EngineMessageProcessor.StopEngineEvaluation();
            LearningMode.CurrentMode = LearningMode.Mode.MANUAL_REVIEW;
            EngineGame.CurrentState = EngineGame.GameState.IDLE;
            Timers.Stop(AppTimers.TimerId.CHECK_FOR_USER_MOVE);

            AppStateManager.SetupGuiForCurrentStates();

            ActiveLine.DisplayPositionForSelectedCell();
            AppStateManager.SwapCommentBoxForEngineLines(false);
            BoardCommentBox.RestoreTitleMessage();
        }

        /// <summary>
        /// Ensure that Workbook Tree's ListView allows
        /// mouse wheel scrolling.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _scvWorkbookTable_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scv = (ScrollViewer)sender;
            scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta);
            e.Handled = true;
        }

        private void _rtbWorkbookFull_PreviewKeyDown(object sender, KeyEventArgs e)
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

            AppStateManager.CurrentLearningMode = LearningMode.Mode.MANUAL_REVIEW;
            AppStateManager.SetupGuiForCurrentStates();
            AppStateManager.SetCurrentEvaluationMode(EvaluationState.EvaluationMode.IDLE);

            AppStateManager.SwapCommentBoxForEngineLines(false);

            UiTabBookmarks.Focus();
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
            LearningMode.CurrentMode = LearningMode.Mode.TRAINING;
            TrainingState.IsTrainingInProgress = true;
            TrainingState.CurrentMode = TrainingState.Mode.AWAITING_USER_TRAINING_MOVE;
            AppStateManager.SetupGuiForCurrentStates();

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
            if (MessageBox.Show("Exit the training session?", "Chess Forge Training", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                // TODO: ask questions re saving etc.

                EngineMessageProcessor.StopEngineEvaluation();
                Evaluation.Reset();

                TrainingState.IsTrainingInProgress = false;
                MainChessBoard.RemoveMoveSquareColors();
                LearningMode.CurrentMode = LearningMode.Mode.MANUAL_REVIEW;
                AppStateManager.SetupGuiForCurrentStates();

                ActiveLine.DisplayPositionForSelectedCell();
                AppStateManager.SwapCommentBoxForEngineLines(false);
                BoardCommentBox.RestoreTitleMessage();
            }
        }

        /// <summary>
        /// Stops evaluation in response to the user clicking
        /// the stop button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiImgStop_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            EngineMessageProcessor.StopEngineEvaluation();
            lock (LearningMode.EvalLock)
            {
                Evaluation.Reset();
                AppStateManager.ResetEvaluationControls();
                AppStateManager.ShowMoveEvaluationControls(false, false);
                AppStateManager.SetupGuiForCurrentStates();
            }

            e.Handled = true;
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
        private void _rtbTrainingProgress_PreviewMouseMove(object sender, MouseEventArgs e)
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
        private void _mnTrainFromBookmark_Click(object sender, RoutedEventArgs e)
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
        private void _cnvBookmark_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Canvas)
            {
                BookmarkManager.ChessboardClickedEvent(((Canvas)sender).Name, _cmBookmarks, e);
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
        private void _mnAddBookmark_Click(object sender, RoutedEventArgs e)
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
        private void _mnWorkbookSelectAsBookmark_Click(object sender, RoutedEventArgs e)
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
                AppStateManager.SaveWorkbookFile();
                UiTabBookmarks.Focus();
            }
        }

        /// <summary>
        /// Adds the last click node, and all its siblings to bookmarks
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _mnWorkbookBookmarkAlternatives_Click(object sender, RoutedEventArgs e)
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
                AppStateManager.SaveWorkbookFile();
                UiTabBookmarks.Focus();
            }
        }

        private void _mnGenerateBookmark_Click(object sender, RoutedEventArgs e) { BookmarkManager.GenerateBookmarks(); }

        private void _mnDeleteBookmark_Click(object sender, RoutedEventArgs e) { BookmarkManager.DeleteBookmark(); }

        private void _mnDeleteAllBookmarks_Click(object sender, RoutedEventArgs e) { BookmarkManager.DeleteAllBookmarks(); }

        private void _mnTrainRestartGame_Click(object sender, RoutedEventArgs e)
        {
            UiTrainingView.RestartGameAfter(sender, e);
        }

        private void _mnTrainSwitchToWorkbook_Click(object sender, RoutedEventArgs e)
        {
            UiTrainingView.RollbackToWorkbookMove();
        }

        private void _mnTrainEvalMove_Click(object sender, RoutedEventArgs e)
        {
            UiTrainingView.RequestMoveEvaluation();
        }


        private void _mnTrainEvalLine_Click(object sender, RoutedEventArgs e)
        {
            UiTrainingView.RequestLineEvaluation();
        }

        /// <summary>
        /// Restarts training from the same position/bookmark
        /// that we started the current session with.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _mnTrainRestartTraining_Click(object sender, RoutedEventArgs e)
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

        public void ColorMoveSquares(string engCode)
        {
            this.Dispatcher.Invoke(() =>
            {
                MainChessBoard.RemoveMoveSquareColors();

                SquareCoords sqOrig, sqDest;
                MoveUtils.EngineNotationToCoords(engCode, out sqOrig, out sqDest);
                MainChessBoard.ColorMoveSquare(sqOrig.Xcoord, sqOrig.Ycoord, true);
                MainChessBoard.ColorMoveSquare(sqDest.Xcoord, sqDest.Ycoord, false);
            });
        }

        private void _tabItemTrainingBrowse_GotFocus(object sender, RoutedEventArgs e) { AppStateManager.SetupGuiForTrainingBrowseMode(); }

        private void _rtbTrainingProgress_GotFocus(object sender, RoutedEventArgs e) { AppStateManager.SetupGuiForTrainingProgressMode(); }

        private void _imgLeftArrow_PreviewMouseDown(object sender, MouseButtonEventArgs e) { BookmarkManager.PageDown(); }

        private void _imgRightArrow_PreviewMouseDown(object sender, MouseButtonEventArgs e) { BookmarkManager.PageUp(); }

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

    }
}