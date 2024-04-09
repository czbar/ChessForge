using GameTree;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for EvaluationChartDialog.xaml
    /// </summary>
    public partial class EvaluationChartDialog : Window
    {
        // width of the 2 canvases
        private double CANVAS_WIDTH = 800;

        // height of the 2 canvases
        private double CANVAS_HEIGHT = 100;

        // diameter of the marker
        private int MARKER_SIZE = 8;

        // length of the X-axis in either canvas
        private double _maxX;

        // length of the Y-axis in either canvas
        private double _maxY;

        // evaluation corresponding to the max Y-axis
        private double _maxEval = 4.5;

        // total number of plies in the represented line.
        private int _plyCount;

        // list of nodes passed by the caller
        private ObservableCollection<TreeNode> _nodeList;

        // part of the vertical line in the white (top) half 
        private Line _whiteLine = new Line();

        // part of the vertical line in the black (bottom) half 
        private Line _blackLine = new Line();

        // evaluation label in the white (top) half 
        private Label _lblWhiteEval = new Label();

        // evaluation label in the black (bottom) half 
        private Label _lblBlackEval = new Label();

        // ply marker in the white (top) half  
        private Ellipse _whiteMarker = new Ellipse();

        // ply marker in the black (bottom) half  
        private Ellipse _blackMarker = new Ellipse();

        /// <summary>
        /// Creates the dialog and calculates the limits.
        /// </summary>
        /// <param name="NodeList"></param>
        public EvaluationChartDialog(ObservableCollection<TreeNode> NodeList)
        {
            InitializeComponent();
            _nodeList = NodeList;

            UiCnvWhite.Width = CANVAS_WIDTH;
            UiCnvWhite.Height = CANVAS_HEIGHT; ;

            UiCnvBlack.Width = CANVAS_WIDTH;
            UiCnvBlack.Height = CANVAS_HEIGHT; ;

            // we do not include node 0
            _plyCount = _nodeList.Count - 1;

            // get the maxX and maxY based on the size of the canvases (mins are 0)
            // TODO: how to get these from the Canvas?
            _maxX = CANVAS_WIDTH;
            _maxY = CANVAS_HEIGHT;

            ConfigureVerticalLine();
            ConfigureEvaluationLabels();
            ConfigureMarkers();

            GeneratePathSet();
        }

        //*****************************************************
        //
        //  INITIALIZATION OF DYNAMIC CONTROLS
        //
        //*****************************************************

        /// <summary>
        /// Creates the vertical line consisting of two "half lines".
        /// </summary>
        private void ConfigureVerticalLine()
        {
            ConfigureVerticalHalfLine(UiCnvWhite, _whiteLine);
            ConfigureVerticalHalfLine(UiCnvBlack, _blackLine);
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
            line.Stroke = Brushes.White;

            cnv.Children.Add(line);

            Canvas.SetZIndex(line, 1);
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
            lbl.Background = Brushes.White;
            lbl.Visibility = Visibility.Collapsed;
            cnv.Children.Add(lbl);
            Canvas.SetZIndex(lbl, 3);
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
            Canvas.SetZIndex(marker, 2);
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
            int index = 1;
            while (true)
            {
                index = GenerateWhiteBlackPaths(index) + 1;
                if (index >= _nodeList.Count - 1)
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

            int lastProcessed = 0;

            TreeNode firstNode = null;
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
                    BuildOpeningSegments(nd, out LineSegment segOpenWhite, out LineSegment segOpenBlack);
                    if (segOpenWhite != null)
                    {
                        whitePathData.Add(segOpenWhite);
                        blackPathData.Add(segOpenBlack);
                    }
                }
                BuildLineSegments(nd, out LineSegment segWhite, out LineSegment segBlack);
                if (segWhite != null)
                {
                    whitePathData.Add(segWhite);
                    blackPathData.Add(segBlack);
                }

                bool close = (i == _nodeList.Count - 1) || string.IsNullOrWhiteSpace(_nodeList[i + 1].EngineEvaluation);
                if (close)
                {
                    BuildClosingSegments(nd, out LineSegment segCloseWhite, out LineSegment segCloseBlack);
                    if (segWhite != null)
                    {
                        whitePathData.Add(segCloseWhite);
                        blackPathData.Add(segCloseBlack);
                    }
                    break;
                }
            }

            if (firstNode != null)
            {
                Point? p = GetPointForMove(firstNode);

                Path whitePath = new Path();
                whitePath.Fill = Brushes.LightGray;
                InsertPathInCanvas(UiCnvWhite, whitePath, whitePathData, new Point(p.Value.X, _maxY));

                Path blackPath = new Path();
                blackPath.Fill = Brushes.Black;
                InsertPathInCanvas(UiCnvBlack, blackPath, blackPathData, new Point(p.Value.X, 0));
            }

            return lastProcessed;
        }

        /// <summary>
        /// Build one LineSegment for the "white" canvas
        /// and one for the "black".
        /// The y-coordinate is handled differently for one than for the other.
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="segWhite"></param>
        /// <param name="segBlack"></param>
        private void BuildLineSegments(TreeNode nd, out LineSegment segWhite, out LineSegment segBlack)
        {
            Point? p = GetPointForMove(nd);
            if (p.HasValue)
            {
                segWhite = new LineSegment(new Point(p.Value.X, CANVAS_HEIGHT - p.Value.Y), true);
                segBlack = new LineSegment(new Point(p.Value.X, -p.Value.Y), true);
            }
            else
            {
                segWhite = null;
                segBlack = null;
            }
        }

        /// <summary>
        /// Builds line segments the go vertically down if this is the last node or the next node has no evaluation.
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="segWhite"></param>
        /// <param name="segBlack"></param>
        private void BuildClosingSegments(TreeNode nd, out LineSegment segWhite, out LineSegment segBlack)
        {
            Point? p = GetPointForMove(nd);
            if (p.HasValue)
            {
                segWhite = new LineSegment(new Point(p.Value.X, CANVAS_HEIGHT), false);
                segBlack = new LineSegment(new Point(p.Value.X, 0), false);
            }
            else
            {
                segWhite = null;
                segBlack = null;
            }
        }

        /// <summary>
        /// Builds line segments the go vertically down if this is the last node or the next node has no evaluation.
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="segWhite"></param>
        /// <param name="segBlack"></param>
        private void BuildOpeningSegments(TreeNode nd, out LineSegment segWhite, out LineSegment segBlack)
        {
            Point? p = GetPointForMove(nd);
            if (p.HasValue)
            {
                segWhite = new LineSegment(new Point(p.Value.X, CANVAS_HEIGHT - p.Value.Y), false);
                segBlack = new LineSegment(new Point(p.Value.X, -p.Value.Y), false);
            }
            else
            {
                segWhite = null;
                segBlack = null;
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
        private void InsertPathInCanvas(Canvas cnv, Path path, List<PathSegment> pathData, Point startPoint)
        {
            PathGeometry geometry = new PathGeometry();
            PathFigure figure = new PathFigure();

            path.Data = GeneratePathGeometry(geometry, figure, startPoint, pathData);
            path.StrokeThickness = 1;
            path.Stroke = Brushes.Red;

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
        private void RepositionEvaluationLabels(Point posCnvWhite, Point posCnvBlack)
        {
            if (0 <= posCnvWhite.X && posCnvWhite.X < _maxX && posCnvWhite.Y >= 0 && posCnvWhite.Y < _maxY)
            {
                PlaceLabel(_lblWhiteEval, posCnvWhite);
                _lblBlackEval.Visibility = Visibility.Collapsed;
                _blackMarker.Visibility = Visibility.Collapsed;
            }
            else if (0 <= posCnvBlack.X && posCnvBlack.X < _maxX && posCnvBlack.Y >= 0 && posCnvBlack.Y < _maxY)
            {
                PlaceLabel(_lblBlackEval, posCnvBlack);
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
        private void RepositionMarkers(Point xPos)
        {
            TreeNode nd = GetNodeFromPosition(xPos);
            PlaceMarker(nd);
        }


        /// <summary>
        /// Places one of the markers (or both if necessary),
        /// and sets the visibility.
        /// </summary>
        /// <param name="nd"></param>
        private void PlaceMarker(TreeNode nd)
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

        /// <summary>
        /// Reposition the vertical lines.
        /// </summary>
        /// <param name="xCoord"></param>
        private void RepositionVerticalLines(double xCoord)
        {
            Canvas.SetLeft(_whiteLine, xCoord);
            Canvas.SetLeft(_blackLine, xCoord);
        }

        /// <summary>
        /// Places one of the evaluation labels.
        /// </summary>
        /// <param name="lbl"></param>
        /// <param name="pos"></param>
        private void PlaceLabel(Label lbl, Point pos)
        {
            Point posLabel = AdjustLabelPosition(pos);

            Canvas.SetLeft(lbl, posLabel.X);
            Canvas.SetTop(lbl, posLabel.Y);

            TreeNode nd = GetNodeFromPosition(pos);

            if (nd != null && !string.IsNullOrEmpty(nd.EngineEvaluation))
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
        /// Parses the evaluation string which can be:
        /// A: empty
        /// B: representation of a double value
        /// C: text indicating looming checkmate
        /// </summary>
        /// <param name="eval"></param>
        /// <returns></returns>
        private double? ParseEval(string eval)
        {
            bool parsed = true;

            double dVal = 0;

            if (!string.IsNullOrEmpty(eval))
            {
                if (eval.StartsWith("#+") || eval.StartsWith("+#"))
                {
                    dVal = _maxEval + 0.1;
                }
                else if (eval.StartsWith("-#") || eval.StartsWith("#-"))
                {
                    dVal = -(_maxEval + 0.1);
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

            if (!string.IsNullOrWhiteSpace(node.EngineEvaluation))
            {
                double? dVal = ParseEval(node.EngineEvaluation);
                if (dVal.HasValue)
                {
                    double numerator = (node.MoveNumber - 1) * 2;
                    if (node.ColorToMove == ChessPosition.PieceColor.White)
                    {
                        numerator += 1.0;
                    }

                    double pixelsPerPly = _maxX;
                    if (_plyCount > 1)
                    {
                        pixelsPerPly = _maxX / ((double)_plyCount - 1);
                    }

                    double x = pixelsPerPly * numerator;
                    double y = (dVal.Value / _maxEval) * _maxY;

                    p = new Point(x, y);
                }
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

            return _nodeList[plyIndex];
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
            // first see whether it is for the White or Black canvas
            var posCnvWhite = e.GetPosition(UiCnvWhite);
            var posCnvBlack = e.GetPosition(UiCnvBlack);

            RepositionEvaluationLabels(posCnvWhite, posCnvBlack);
            RepositionMarkers(posCnvWhite);
            RepositionVerticalLines(posCnvWhite.X);
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
                AppState.MainWin.SetActiveLine(node.LineId, node.NodeId);
                if (AppState.MainWin.ActiveTreeView is StudyTreeView view)
                {
                    if (view.UncollapseMove(node))
                    {
                        view.BuildFlowDocumentForVariationTree();
                    }
                }
                AppState.MainWin.SelectLineAndMoveInWorkbookViews(AppState.MainWin.ActiveTreeView, node.LineId,
                    AppState.MainWin.ActiveLine.GetSelectedPlyNodeIndex(false), true);
            }
        }
    }
}
