﻿using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for EvaluationChartControl.xaml
    /// </summary>
    public partial class EvaluationChartControl : UserControl
    {
        // width of the 2 canvases
        private double CANVAS_WIDTH = 670;

        // height of the 2 canvases
        private double CANVAS_HEIGHT = 74;

        // diameter of the marker
        private int MARKER_SIZE = 8;

        // max allowed scale
        private int MAX_EVAL_SCALE = 16;

        // min allowed scale
        private int MIN_EVAL_SCALE = 1;

        /// <summary>
        /// Set to true from the client if the evaluations have changed
        /// or as the chart becomes visible after being hidden.
        /// </summary>
        public bool IsDirty = true;

        // length of the X-axis in either canvas
        private double _maxX;

        // length of the Y-axis in either canvas
        private double _maxY;

        // evaluation corresponding to the max Y-axis
        private double _evalScale = 4;

        // total number of plies in the represented line.
        private int _plyCount;

        // number of pixels per ply in the chart view
        private double _pixelsPerPly;

        // half of the pixels per ply value (to save code as it is used in multiple places)
        private double _halfPixelsPerPly;

        // list of nodes passed by the caller
        private ObservableCollection<TreeNode> _nodeList;

        // part of the vertical line in the white (top) half 
        private Line _whiteMovingLine = new Line();

        // part of the vertical line in the black (bottom) half 
        private Line _blackMovingLine = new Line();

        // fixed label with the "Evaluation Chart" text
        private Label _lblTitle = new Label();

        // label displaying the move currently selected in the chart.
        private Label _lblMove = new Label();

        // fixed position evaluation label
        private Label _lblFixedPosEval = new Label();

        // evaluation label in the white (top) half 
        private Label _lblWhiteEval = new Label();

        // evaluation label in the black (bottom) half 
        private Label _lblBlackEval = new Label();

        // ply marker in the white (top) half  
        private Ellipse _whiteMarker = new Ellipse();

        // ply marker in the black (bottom) half  
        private Ellipse _blackMarker = new Ellipse();

        // zIndex for the GUI elements
        private int _zIndexMidLine = 1;
        private int _zIndexVertLine = 1;
        private int _zIndexMarker = 2;
        private int _zIndexEvalLabel = 3;
        private int _zIndexMoveLabel = 4;
        private int _zFixedPosEvalLabel = 4;

        /// <summary>
        /// Creates the dialog and calculates the limits.
        /// </summary>
        /// <param name="NodeList"></param>
        public EvaluationChartControl()
        {
            InitializeComponent();
            UiCnvWhite.Width = CANVAS_WIDTH;
            UiCnvWhite.Height = CANVAS_HEIGHT; ;
            UiCnvWhite.Background = ChessForgeColors.TABLE_HIGHLIGHT_GREEN;

            UiCnvBlack.Width = CANVAS_WIDTH;
            UiCnvBlack.Height = CANVAS_HEIGHT;
            UiCnvBlack.Background = ChessForgeColors.TABLE_HEADER_GREEN;

            Line UiMidLine = new Line();
            UiMidLine.Stroke = Brushes.DarkGreen;
            UiMidLine.StrokeThickness = 1;
            UiMidLine.X1 = 0;
            UiMidLine.X1 = CANVAS_WIDTH;
            UiMidLine.Y1 = CANVAS_HEIGHT;
            UiMidLine.Y2 = CANVAS_HEIGHT;

            UiCnvWhite.Children.Add(UiMidLine);
            Canvas.SetZIndex(UiMidLine, _zIndexMidLine);

            // get the maxX and maxY based on the size of the canvases (mins are 0)
            // TODO: how to get these from the Canvas?
            _maxX = CANVAS_WIDTH;
            _maxY = CANVAS_HEIGHT;

            ConfigureZoomButtons();
            ConfigureTitleLabel();
            ConfigureMoveLabel();
            ConfigureFixedPositionEvaluationLabel();

            ConfigureVerticalLine();
            ConfigureEvaluationLabels();
            ConfigureMarkers();
        }

        /// <summary>
        /// Overloaded method to render the chart with the current active line.
        /// </summary>
        public void Update()
        {
            Update(AppState.MainWin.ActiveLine.GetNodeList());
        }

        /// <summary>
        /// Checks if the list differs from the one that we are showing now.
        /// If the same but marked as dirty refresh too.
        /// Displays the updated chart.
        /// </summary>
        /// <param name="nodeList"></param>
        public void Update(ObservableCollection<TreeNode> nodeList)
        {
            AppState.MainWin.Dispatcher.Invoke(() =>
            {
                if (CanShowChart(false))
                {
                    try
                    {
                        if (!IsDirty && nodeList.Count == _nodeList.Count)
                        {
                            int count = nodeList.Count;
                            if (count == 0 || nodeList[count - 1] == _nodeList[count - 1])
                            {
                                return;
                            }
                        }

                        _nodeList = nodeList;

                        // we do not include node 0
                        _plyCount = _nodeList.Count - 1;

                        if (_plyCount > 1)
                        {
                            _pixelsPerPly = _maxX / ((double)_plyCount - 1);
                        }
                        else
                        {
                            _pixelsPerPly = _maxX;
                        }
                        _halfPixelsPerPly = _pixelsPerPly / 2;

                        GeneratePathSet();
                        SetScaleLabelText();
                        AppState.MainWin.UiEvalChart.Visibility = Visibility.Visible;

                        SelectMove(AppState.ActiveLine.GetSelectedTreeNode());

                        IsDirty = false;
                    }
                    catch (Exception ex)
                    {
                        AppLog.Message("EvaluationChart:Update()", ex);
                    }
                    AppState.MainWin.UiEvalChart.Visibility = Visibility.Visible;
                }
                else
                {
                    AppState.MainWin.UiEvalChart.Visibility = Visibility.Hidden;
                }
            });
        }

        /// <summary>
        /// Repositions floating elements to the requested move.
        /// </summary>
        /// <param name="nd"></param>
        public void SelectMove(TreeNode nd)
        {
            if (nd == null)
            {
                if (_nodeList.Count > 1)
                {
                    nd = _nodeList[_nodeList.Count - 1];
                }
            }

            Point? p = GetPointForMove(nd);
            if (p != null)
            {
                Point pt = p.Value;
                RepositionFloatingElements(pt, null, nd);
            }
        }

        /// <summary>
        /// Reports if the chart can be shown
        /// </summary>
        /// <returns></returns>
        public bool ReportIfCanShow()
        {
            return CanShowChart(true);
        }

        /// <summary>
        /// Checks it chart can be currently shown.
        /// If it can't and showReason == true, a flash announcement
        /// with the reason will be displayed.
        /// </summary>
        /// <returns></returns>
        public bool CanShowChart(bool showReason)
        {
            bool res = true;

            if (Configuration.ShowEvaluationChart)
            {
                if (AppState.ActiveTab != TabViewType.STUDY && AppState.ActiveTab != TabViewType.MODEL_GAME)
                {
                    // report wrong tab
                    if (showReason)
                    {
                        AppState.MainWin.BoardCommentBox.ShowFlashAnnouncement(Properties.Resources.ChartErrorWrongTab, Brushes.Red);
                    }
                    res = false;
                }
                else if (EvaluationManager.IsRunning)
                {
                    // report evaluation in progress
                    if (showReason)
                    {
                        AppState.MainWin.BoardCommentBox.ShowFlashAnnouncement(Properties.Resources.ChartErrorEvalInProgress, Brushes.Red);
                    }
                    res = false;
                }
                else if (!HasMovesWithEval(AppState.ActiveLine.GetNodeList(), 2))
                {
                    // insufficient moves with evaluations
                    if (showReason)
                    {
                        AppState.MainWin.BoardCommentBox.ShowFlashAnnouncement(Properties.Resources.ChartErrorInsufficientEvals, Brushes.Red);
                    }
                    res = false;
                }
            }
            else
            {
                res = false;
            }

            return res;
        }

        //*****************************************************
        //
        //  INITIALIZATION OF DYNAMIC CONTROLS
        //
        //*****************************************************

        /// <summary>
        /// Place the buttons in the center.
        /// </summary>
        private void ConfigureZoomButtons()
        {
            Canvas.SetLeft(UiBtnPlus, (CANVAS_WIDTH / 2) - 18);
            Canvas.SetLeft(UiBtnMinus, (CANVAS_WIDTH / 2) + 1);

            Canvas.SetLeft(UiBtnScale, (CANVAS_WIDTH / 2) - 80);
            SetScaleLabelText();
        }

        /// <summary>
        /// Creates the static label with "Evaluation Chart" title.
        /// </summary>
        private void ConfigureTitleLabel()
        {
            _lblTitle.Content = Properties.Resources.EvaluationChart;
            _lblTitle.Background = Brushes.Transparent;

            UiCnvWhite.Children.Add(_lblTitle);
            Canvas.SetLeft(_lblTitle, 5);
            Canvas.SetTop(_lblTitle, 0);
        }

        /// <summary>
        /// Creates the move label.
        /// </summary>
        private void ConfigureMoveLabel()
        {
            _lblMove.Background = Brushes.Transparent;
            _lblMove.FontWeight = FontWeights.Bold;

            UiCnvWhite.Children.Add(_lblMove);
            Canvas.SetRight(_lblMove, 5);
            Canvas.SetTop(_lblMove, 0);

            Canvas.SetZIndex(_lblMove, _zIndexMoveLabel);
        }

        /// <summary>
        /// Creates the move label.
        /// </summary>
        private void ConfigureFixedPositionEvaluationLabel()
        {
            _lblFixedPosEval.Background = Brushes.Transparent;
            _lblFixedPosEval.FontSize = _lblFixedPosEval.FontSize - 2;

            UiCnvWhite.Children.Add(_lblFixedPosEval);
            Canvas.SetRight(_lblFixedPosEval, 5);
            Canvas.SetTop(_lblFixedPosEval, 18);

            Canvas.SetZIndex(_lblFixedPosEval, _zFixedPosEvalLabel);
        }

        /// <summary>
        /// Creates the vertical line consisting of two "half lines".
        /// </summary>
        private void ConfigureVerticalLine()
        {
            ConfigureVerticalHalfLine(UiCnvWhite, _whiteMovingLine);
            ConfigureVerticalHalfLine(UiCnvBlack, _blackMovingLine);
        }

        /// <summary>
        /// Configure one half of the vertical line.
        /// </summary>
        /// <param name="cnv"></param>
        /// <param name="line"></param>
        private void ConfigureVerticalHalfLine(Canvas cnv, Line line)
        {
            line.Width = 3;
            line.X1 = -1;
            line.X2 = -1;
            line.Y1 = 0;
            line.Y2 = CANVAS_HEIGHT;
            line.Stroke = Brushes.Ivory;

            cnv.Children.Add(line);

            Canvas.SetZIndex(line, _zIndexVertLine);
            Canvas.SetLeft(line, -1);
            Canvas.SetTop(line, 0);
        }

        /// <summary>
        /// Creates evaluation labels.
        /// </summary>
        private void ConfigureEvaluationLabels()
        {
            ConfigureEvaluationLabel(UiCnvWhite, _lblWhiteEval);
            ConfigureEvaluationLabel(UiCnvBlack, _lblBlackEval);
        }

        /// <summary>
        /// Configures one evaluation label.
        /// </summary>
        /// <param name="cnv"></param>
        /// <param name="lbl"></param>
        private void ConfigureEvaluationLabel(Canvas cnv, Label lbl)
        {
            lbl.Foreground = Brushes.Black;
            lbl.Background = Brushes.Ivory;
            lbl.Visibility = Visibility.Collapsed;
            cnv.Children.Add(lbl);
            Canvas.SetZIndex(lbl, _zIndexEvalLabel);
        }

        /// <summary>
        /// Confgures move markers.
        /// </summary>
        private void ConfigureMarkers()
        {
            ConfigureMarker(UiCnvWhite, _whiteMarker);
            ConfigureMarker(UiCnvBlack, _blackMarker);
        }

        /// <summary>
        /// Configures one move marker.
        /// </summary>
        /// <param name="cnv"></param>
        /// <param name="marker"></param>
        private void ConfigureMarker(Canvas cnv, Ellipse marker)
        {
            marker.Width = MARKER_SIZE;
            marker.Height = MARKER_SIZE;
            marker.Fill = Brushes.Yellow;

            cnv.Children.Add(marker);
            marker.Visibility = Visibility.Collapsed;
            Canvas.SetZIndex(marker, _zIndexMarker);
        }

        //*****************************************************
        //
        //  CREATE PATHS
        //
        //*****************************************************

        /// <summary>
        /// Generates the evaluation polylines.
        /// </summary>
        private void GeneratePathSet()
        {
            // clear previous paths
            RemovePathsFromCanvas(UiCnvWhite);
            RemovePathsFromCanvas(UiCnvBlack);

            int index = 1;
            while (true)
            {
                index = GenerateWhiteBlackPaths(index) + 1;
                if (index > _nodeList.Count - 1)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Builds the lists of LineSegments for the "white" and "black" parts of the chart.
        /// Inserts them into the paths and adds paths to the canvases.
        /// </summary>
        private int GenerateWhiteBlackPaths(int startIndex)
        {
            List<PathSegment> whitePathData = new List<PathSegment>();
            List<PathSegment> blackPathData = new List<PathSegment>();

            Polyline whiteLine = new Polyline();
            whiteLine.Stroke = Brushes.Black;

            Polyline blackLine = new Polyline();
            blackLine.Stroke = Brushes.Black;


            int lastProcessed = 0;

            TreeNode firstNode = null;
            bool singleNode = false;

            for (int i = startIndex; i < _nodeList.Count; i++)
            {
                lastProcessed = i;
                TreeNode nd = _nodeList[i];

                if (i == startIndex)
                {
                    if (string.IsNullOrEmpty(nd.EngineEvaluation))
                    {
                        return lastProcessed;
                    }

                    firstNode = nd;

                    singleNode = IsSingleNode(i);
                    BuildOpeningSegments(nd, singleNode, whitePathData, blackPathData, whiteLine, blackLine);
                }
                BuildLineSegments(nd, whitePathData, blackPathData, whiteLine, blackLine);

                bool close = (i == _nodeList.Count - 1) || string.IsNullOrWhiteSpace(_nodeList[i + 1].EngineEvaluation);
                if (close)
                {
                    BuildClosingSegments(nd, singleNode, whitePathData, blackPathData, whiteLine, blackLine);
                    break;
                }
            }

            if (firstNode != null)
            {
                InsertPathsInCanvases(firstNode, singleNode, whitePathData, blackPathData);
                InsertLinesInCanvases(whiteLine, blackLine);
            }

            return lastProcessed;
        }

        /// <summary>
        /// Inserts the passed Paths into the canvases.
        /// </summary>
        /// <param name="firstNode"></param>
        /// <param name="singleNode"></param>
        /// <param name="whitePathData"></param>
        /// <param name="blackPathData"></param>
        private void InsertPathsInCanvases(TreeNode firstNode, bool singleNode, List<PathSegment> whitePathData, List<PathSegment> blackPathData)
        {
            Point? p = GetPointForMove(firstNode);

            Path whitePath = new Path();
            whitePath.Fill = Brushes.White;
            InsertPathInCanvas(UiCnvWhite, whitePath, whitePathData, singleNode, new Point(p.Value.X, _maxY));

            Path blackPath = new Path();
            blackPath.Fill = Brushes.DarkGray;
            InsertPathInCanvas(UiCnvBlack, blackPath, blackPathData, singleNode, new Point(p.Value.X, 0));
        }

        /// <summary>
        /// Inserts the passed Lines into the canvases.
        /// </summary>
        /// <param name="whiteLine"></param>
        /// <param name="blackLine"></param>
        private void InsertLinesInCanvases(Polyline whiteLine, Polyline blackLine)
        {
            UiCnvWhite.Children.Add(whiteLine);
            UiCnvBlack.Children.Add(blackLine);
        }

        /// <summary>
        /// Builds line segments the go vertically down if this is the last node or the next node has no evaluation.
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="segWhite"></param>
        /// <param name="segBlack"></param>
        private void BuildOpeningSegments(TreeNode nd, bool singleNode, List<PathSegment> whitePath, List<PathSegment> blackPath, Polyline whiteLine, Polyline blackLine)
        {
            Point? p = GetPointForMove(nd);
            if (p.HasValue)
            {
                if (singleNode)
                {
                    // vertical up/down line
                    Point whitePoint = new Point(p.Value.X - _halfPixelsPerPly, CANVAS_HEIGHT - p.Value.Y);
                    whitePath.Add(new LineSegment(whitePoint, false));
                    whiteLine.Points.Add(whitePoint);

                    Point blackPoint = new Point(p.Value.X - _halfPixelsPerPly, -p.Value.Y);
                    blackPath.Add(new LineSegment(blackPoint, false));
                    blackLine.Points.Add(blackPoint);

                    whitePoint = new Point(p.Value.X, CANVAS_HEIGHT - p.Value.Y);
                    whitePath.Add(new LineSegment(whitePoint, false));
                    whiteLine.Points.Add(whitePoint);

                    blackPoint = new Point(p.Value.X, -p.Value.Y);
                    blackPath.Add(new LineSegment(blackPoint, false));
                    blackLine.Points.Add(blackPoint);
                }
                else
                {
                    whitePath.Add(new LineSegment(new Point(p.Value.X, CANVAS_HEIGHT - p.Value.Y), false));
                    blackPath.Add(new LineSegment(new Point(p.Value.X, -p.Value.Y), false));
                }
            }
        }

        /// <summary>
        /// Build one LineSegment for the "white" canvas
        /// and one for the "black".
        /// The y-coordinate is handled differently for one than for the other.
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="segWhite"></param>
        /// <param name="segBlack"></param>
        private void BuildLineSegments(TreeNode nd, List<PathSegment> whitePath, List<PathSegment> blackPath, Polyline whiteLine, Polyline blackLine)
        {
            Point? p = GetPointForMove(nd);
            if (p.HasValue)
            {
                Point whitePoint = new Point(p.Value.X, CANVAS_HEIGHT - p.Value.Y);
                whitePath.Add(new LineSegment(whitePoint, true));

                Point blackPoint = new Point(p.Value.X, -p.Value.Y);
                blackPath.Add(new LineSegment(blackPoint, true));

                whiteLine.Points.Add(whitePoint);
                blackLine.Points.Add(blackPoint);
            }
        }

        /// <summary>
        /// Builds line segments the go vertically down if this is the last node or the next node has no evaluation.
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="segWhite"></param>
        /// <param name="segBlack"></param>
        private void BuildClosingSegments(TreeNode nd, bool singleNode, List<PathSegment> whitePath, List<PathSegment> blackPath, Polyline whiteLine, Polyline blackLine)
        {
            Point? p = GetPointForMove(nd);

            if (p.HasValue)
            {
                if (singleNode)
                {
                    // extension for a single node
                    Point whitePoint = new Point(p.Value.X + _halfPixelsPerPly, CANVAS_HEIGHT - p.Value.Y);
                    whitePath.Add(new LineSegment(whitePoint, false));
                    whiteLine.Points.Add(whitePoint);

                    Point blackPoint = new Point(p.Value.X + _halfPixelsPerPly, -p.Value.Y);
                    blackPath.Add(new LineSegment(blackPoint, false));
                    blackLine.Points.Add(blackPoint);

                    // vertical up/down line
                    whitePoint = new Point(p.Value.X + _halfPixelsPerPly, CANVAS_HEIGHT);
                    blackPoint = new Point(p.Value.X + _halfPixelsPerPly, 0);
                    whitePath.Add(new LineSegment(whitePoint, false));
                    blackPath.Add(new LineSegment(blackPoint, false));
                }
                else
                {
                    whitePath.Add(new LineSegment(new Point(p.Value.X, CANVAS_HEIGHT), false));
                    blackPath.Add(new LineSegment(new Point(p.Value.X, 0), false));
                }
            }
        }

        /// <summary>
        /// Fill out the PathFigure objects and add to PathGeometry Figures.
        /// </summary>
        /// <param name="geometry"></param>
        /// <param name="figure"></param>
        /// <param name="startPoint"></param>
        /// <param name="segments"></param>
        /// <returns></returns>
        private PathGeometry GeneratePathGeometry(PathGeometry geometry, PathFigure figure, Point startPoint, List<PathSegment> segments)
        {
            figure.StartPoint = startPoint;
            figure.IsClosed = false;

            foreach (PathSegment segment in segments)
            {
                figure.Segments.Add(segment);
            }

            geometry.Figures.Add(figure);
            return geometry;
        }

        /// <summary>
        /// Inserts a path into a canvas.
        /// </summary>
        /// <param name="cnv"></param>
        /// <param name="path"></param>
        /// <param name="pathData"></param>
        /// <param name="startPoint"></param>
        private void InsertPathInCanvas(Canvas cnv, Path path, List<PathSegment> pathData, bool singleNode, Point startPoint)
        {
            PathGeometry geometry = new PathGeometry();
            PathFigure figure = new PathFigure();

            if (singleNode)
            {
                startPoint = new Point(startPoint.X - _halfPixelsPerPly, startPoint.Y);
            }

            path.StrokeThickness = 0;
            //path.Stroke = Brushes.Black;
            path.Data = GeneratePathGeometry(geometry, figure, startPoint, pathData);

            cnv.Children.Add(path);
        }


        //*****************************************************
        //
        //  PLACING ELEMENTS AFTER A MOUSE MOVE
        //
        //*****************************************************

        /// <summary>
        /// Repositions the labels.
        /// Based on the events position relative to the two canvases
        /// determines which label is visible and places it.
        /// </summary>
        /// <param name="posCnvWhite"></param>
        /// <param name="posCnvBlack"></param>
        private void RepositionEvaluationLabels(Point posCnvWhite, Point posCnvBlack, TreeNode nd)
        {
            if (0 <= posCnvWhite.X && posCnvWhite.X < _maxX && posCnvWhite.Y >= 0 && posCnvWhite.Y < _maxY)
            {
                PlaceLabel(_lblWhiteEval, posCnvWhite, nd);
                _lblBlackEval.Visibility = Visibility.Collapsed;
                _blackMarker.Visibility = Visibility.Collapsed;
            }
            else if (0 <= posCnvBlack.X && posCnvBlack.X < _maxX && posCnvBlack.Y >= 0 && posCnvBlack.Y < _maxY)
            {
                PlaceLabel(_lblBlackEval, posCnvBlack, nd);
                _lblWhiteEval.Visibility = Visibility.Collapsed;
                _whiteMarker.Visibility = Visibility.Collapsed;
            }
            else
            {
                _lblWhiteEval.Visibility = Visibility.Collapsed;
                _lblBlackEval.Visibility = Visibility.Collapsed;
                _whiteMarker.Visibility = Visibility.Collapsed;
                _blackMarker.Visibility = Visibility.Collapsed;
            }

        }

        /// <summary>
        /// Repositions the labels.
        /// Based on the events position relative to the two canvases
        /// determines which marker (or both) is visible and places it.
        /// </summary>
        /// <param name="xPos"></param>
        private void RepositionMarkers(Point xPos, TreeNode nd)
        {
            PlaceMarker(nd);
            if (nd != null && nd.NodeId != 0)
            {
                _lblMove.Content = MoveUtils.BuildSingleMoveText(nd, true, false, 0);
                if (!string.IsNullOrEmpty(nd.EngineEvaluation))
                {
                    _lblFixedPosEval.Content = nd.EngineEvaluation;
                }
                else
                {
                    _lblFixedPosEval.Content = "";
                }
            }
            else
            {
                _lblMove.Content = "";
                _lblFixedPosEval.Content = "";
            }
        }

        /// <summary>
        /// Places one of the markers (or both if necessary),
        /// and sets the visibility.
        /// </summary>
        /// <param name="nd"></param>
        private void PlaceMarker(TreeNode nd)
        {
            if (nd == null || string.IsNullOrEmpty(nd.EngineEvaluation) || nd.NodeId == 0)
            {
                _whiteMarker.Visibility = Visibility.Collapsed;
                _blackMarker.Visibility = Visibility.Collapsed;
            }
            else
            {
                Point? pt = GetPointForMove(nd);
                if (pt.HasValue)
                {
                    Point p = GetPointForMove(nd).Value;

                    bool isValuePositive;

                    Ellipse marker;
                    Ellipse otherMarker;
                    if (p.Y >= 0)
                    {
                        isValuePositive = true;
                        marker = _whiteMarker;
                        otherMarker = _blackMarker;
                        p.Y = CANVAS_HEIGHT - p.Y;
                    }
                    else
                    {
                        isValuePositive = false;
                        p.Y = -p.Y;
                        marker = _blackMarker;
                        otherMarker = _whiteMarker;
                    }

                    p.Y = p.Y - MARKER_SIZE;
                    p.X = p.X - (MARKER_SIZE / 2);

                    marker.Visibility = Visibility.Visible;
                    otherMarker.Visibility = Visibility.Collapsed;

                    if (!isValuePositive && p.Y < 0)
                    {
                        Point pOther = new Point();
                        pOther.X = p.X;
                        pOther.Y = CANVAS_HEIGHT + p.Y;
                        otherMarker.Visibility = Visibility.Visible;
                        Canvas.SetLeft(otherMarker, pOther.X);
                        Canvas.SetTop(otherMarker, pOther.Y);
                    }

                    Canvas.SetLeft(marker, p.X);
                    Canvas.SetTop(marker, p.Y);
                }
            }
        }

        /// <summary>
        /// Reposition the vertical lines.
        /// </summary>
        /// <param name="xCoord"></param>
        private void RepositionVerticalLines(double xCoord)
        {
            Canvas.SetLeft(_whiteMovingLine, xCoord);
            Canvas.SetLeft(_blackMovingLine, xCoord);
        }

        /// <summary>
        /// Places one of the evaluation labels.
        /// </summary>
        /// <param name="lbl"></param>
        /// <param name="pos"></param>
        private void PlaceLabel(Label lbl, Point pos, TreeNode nd)
        {
            Point posLabel = AdjustLabelPosition(pos);

            Canvas.SetLeft(lbl, posLabel.X);
            Canvas.SetTop(lbl, posLabel.Y);

            if (nd != null && !string.IsNullOrEmpty(nd.EngineEvaluation) && nd.NodeId != 0)
            {
                lbl.Visibility = Visibility.Visible;
                lbl.Content = nd.EngineEvaluation;
            }
            else
            {
                lbl.Visibility = Visibility.Collapsed;
            }
        }


        //*****************************************************
        //
        //  UTILITIES
        //
        //*****************************************************

        /// <summary>
        /// Determines if this is an evaluation "island" 
        /// i.e. the move before and after has no evaluation
        /// or there is no move.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        private bool IsSingleNode(int i)
        {
            bool res = false;

            // is first node or previous node's is null AND is the last node or next mode's eval is null
            if ((i == 1 || string.IsNullOrEmpty(_nodeList[i - 1].EngineEvaluation))
                && (i == _nodeList.Count - 1 || string.IsNullOrEmpty(_nodeList[i + 1].EngineEvaluation)))
            {
                res = true;
            }

            return res;
        }

        /// <summary>
        /// Removes paths from the canvas so that new ones can be inserted.
        /// </summary>
        /// <param name="cnv"></param>
        private void RemovePathsFromCanvas(Canvas cnv)
        {
            List<UIElement> pathsToRemove = new List<UIElement>();
            foreach (UIElement child in cnv.Children)
            {
                if (child is Path || child is Polyline)
                {
                    pathsToRemove.Add(child);
                }
            }
            foreach (var child in pathsToRemove)
            {
                cnv.Children.Remove(child);
            }
        }

        /// <summary>
        /// Checks if the passed line has the required minimum of moves
        /// with evaluations.
        /// </summary>
        /// <param name="line"></param>
        /// <param name="minMoves"></param>
        /// <returns></returns>
        private bool HasMovesWithEval(ObservableCollection<TreeNode> line, int minMoves)
        {
            bool res = false;

            if (line != null)
            {
                int count = 0;
                foreach (TreeNode node in line)
                {
                    if (node.NodeId != 0 && !string.IsNullOrWhiteSpace(node.EngineEvaluation))
                    {
                        count++;
                        if (count >= minMoves)
                        {
                            res = true;
                            break;
                        }
                    }
                }
            }

            return res;
        }

        /// <summary>
        /// Parses the evaluation string which can be:
        /// A: empty
        /// B: representation of a double value
        /// C: text indicating looming checkmate
        /// </summary>
        /// <param name="eval"></param>
        /// <returns></returns>
        private double? ParseEval(string eval, PieceColor color)
        {
            bool parsed = true;

            double dVal = 0;

            if (!string.IsNullOrEmpty(eval))
            {
                if (eval.StartsWith("#"))
                {
                    if (color == PieceColor.White)
                    {
                        dVal = -(_evalScale + 6);
                    }
                    else
                    {
                        dVal = _evalScale + 6;
                    }
                }
                else if (eval.StartsWith("#+") || eval.StartsWith("+#"))
                {
                    dVal = _evalScale + 5;
                }
                else if (eval.StartsWith("-#") || eval.StartsWith("#-"))
                {
                    dVal = -(_evalScale + 5);
                }
                else
                {
                    parsed = double.TryParse(eval, out dVal);
                }
            }

            return parsed ? (double?)dVal : null;
        }

        /// <summary>
        /// Calculates coordinates of the point from the move number,
        /// color to move and engine evaluation.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private Point? GetPointForMove(TreeNode node)
        {
            Point? p = null;

            if (node != null)
            {
                double numerator = (node.MoveNumber - 1) * 2;
                if (node.ColorToMove == ChessPosition.PieceColor.White)
                {
                    numerator += 1.0;
                }

                double x = _pixelsPerPly * numerator;

                double? dVal = ParseEval(node.EngineEvaluation, node.ColorToMove);
                // null will be returned if we fail to parse but we have to set it to something or the chart may look weird.
                double eval = dVal.HasValue ? dVal.Value : 0;
                double y = (eval / _evalScale) * _maxY;

                p = new Point(x, y);
            }

            return p;
        }

        /// <summary>
        /// Finds the ply (node) closest to the clicked point's
        /// X coordinate.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        private TreeNode GetNodeFromPosition(Point pos)
        {
            double fraction = pos.X / _maxX;
            int plyIndex = (int)Math.Round(fraction * (_plyCount - 1)) + 1;

            if (plyIndex < 1)
            {
                plyIndex = 1;
            }
            if (plyIndex > _plyCount)
            {
                plyIndex = _plyCount;
            }

            if (plyIndex > _nodeList.Count - 1)
            {
                return null;
            }
            else
            {
                return _nodeList[plyIndex];
            }
        }

        /// <summary>
        /// Adjusted the position of the label so that it is properly visible within the canvas.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        private Point AdjustLabelPosition(Point position)
        {
            Point adjusted = new Point();

            double left = Math.Min(position.X + 5, _maxX - 60);
            adjusted.X = left;

            double top = Math.Max(position.Y, 10);
            top = Math.Min(top, _maxY - 35);
            adjusted.Y = top;

            return adjusted;
        }

        /// <summary>
        /// Builds text for the scale label.
        /// </summary>
        /// <returns></returns>
        private void SetScaleLabelText()
        {
            UiBtnScale.Content = Properties.Resources.Scale + ": " + _evalScale.ToString("F1");
        }

        /// <summary>
        /// Repositions all floating elements to the requested positions.
        /// </summary>
        /// <param name="cnvWhite"></param>
        /// <param name="cnvBlack"></param>
        private void RepositionFloatingElements(Point cnvWhite, Point? cnvBlack, TreeNode nd)
        {
            if (cnvBlack != null)
            {
                RepositionEvaluationLabels(cnvWhite, cnvBlack.Value, nd);
            }
            else
            {
                RepositionEvaluationLabels(cnvWhite, new Point(cnvWhite.X, 50), nd);
            }

            RepositionMarkers(cnvWhite, nd);
            RepositionVerticalLines(cnvWhite.X);
        }

        //*****************************************************
        //
        //  EVENT HANDLERS
        //
        //*****************************************************

        /// <summary>
        /// A mouse movement was detected over the grid.
        /// Collect the positions relative to the two canvases
        /// and invoke methods positioning the vertical line,
        /// labels and markers.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Grid_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_plyCount > 1)
            {
                // first see whether it is for the White or Black canvas
                var posCnvWhite = e.GetPosition(UiCnvWhite);
                var posCnvBlack = e.GetPosition(UiCnvBlack);

                TreeNode nd = GetNodeFromPosition(posCnvWhite);
                if (nd != null)
                {
                    RepositionFloatingElements(posCnvWhite, posCnvBlack, nd);
                }
            }
        }

        /// <summary>
        /// If clicked inside either canvas select the nearest move in the current view.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Grid_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var posCnvWhite = e.GetPosition(UiCnvWhite);

            TreeNode node = GetNodeFromPosition(posCnvWhite);
            if (node != null)
            {
                string lineId = _nodeList.Last().LineId;
                AppState.MainWin.SetActiveLine(lineId, node.NodeId);
                if (AppState.MainWin.ActiveTreeView is StudyTreeView view)
                {
                    if (view.UncollapseMove(node))
                    {
                        view.BuildFlowDocumentForVariationTree();
                    }
                }

                AppState.MainWin.SelectLineAndMoveInWorkbookViews(AppState.MainWin.ActiveTreeView, lineId,
                    AppState.MainWin.ActiveLine.GetSelectedPlyNodeIndex(false), true);
            }
        }

        /// <summary>
        /// Decrease the max Y of the chart (i.e. zoom in)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnMinus_Click(object sender, RoutedEventArgs e)
        {
            _evalScale = Math.Max(MIN_EVAL_SCALE, _evalScale / 2);
            IsDirty = true;
            Update(_nodeList);
        }

        /// <summary>
        /// Increase the max Y of the chart (i.e. zoom out)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnPlus_Click(object sender, RoutedEventArgs e)
        {
            _evalScale = Math.Min(MAX_EVAL_SCALE, _evalScale * 2);
            IsDirty = true;
            Update(_nodeList);
        }
    }
}