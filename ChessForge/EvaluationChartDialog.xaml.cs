using GameTree;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
        // length of the X-axis in either canvas
        private double _maxX;

        // length of the Y-axis in either canvas
        private double _maxY;

        // evaluation corresponding to the max Y-axis
        private double _maxEval = 1.0;

        // total number of plies in the represented line.
        private int _plyCount;

        // path geometry for the top canvas
        private PathGeometry _whiteGeometry = new PathGeometry();
        
        // path figure for the top canvas
        private PathFigure _whiteFigure = new PathFigure();
        
        // path for the top canvas
        private Path _whitePath = new Path();

        // path geometry for the bottom canvas
        private PathGeometry _blackGeometry = new PathGeometry();

        // path figure for the bottom canvas
        private PathFigure _blackFigure = new PathFigure();

        // path for the bottom canvas
        private Path _blackPath = new Path();

        // list of nodes passed by the caller
        private ObservableCollection<TreeNode> _nodeList;

        /// <summary>
        /// Creates the dialog and calculates the limits.
        /// </summary>
        /// <param name="NodeList"></param>
        public EvaluationChartDialog(ObservableCollection<TreeNode> NodeList)
        {
            InitializeComponent();
            _nodeList = NodeList;

            // we do not include node 0
            _plyCount = _nodeList.Count - 1;

            // get the maxX and maxY based on the size of the canvases (mins are 0)
            // TODO: how to get these from the Canvas?
            _maxX = 800;
            _maxY = 100;

            GeneratePaths();
        }

        /// <summary>
        /// Builds the lists of LineSegments for the "white" and "black" parts of the chart.
        /// Inserts them into the paths and adds paths to the canvases.
        /// </summary>
        private void GeneratePaths()
        {
            List<PathSegment> whitePathData = new List<PathSegment>();
            List<PathSegment> blackPathData = new List<PathSegment>();

            for (int i = 1; i < _nodeList.Count; i++)
            {
                TreeNode nd = _nodeList[i];
                BuildLineSegments(nd, out LineSegment segWhite, out LineSegment segBlack);
                if (segWhite != null)
                {
                    whitePathData.Add(segWhite);
                    blackPathData.Add(segBlack);
                }

                bool close = (i == _nodeList.Count - 1) || string.IsNullOrWhiteSpace(_nodeList[i + 1].EngineEvaluation);
                if (close)
                {
                    BuildCloseSegments(nd, out LineSegment segCloseWhite, out LineSegment segCloseBlack);
                    if (segWhite != null)
                    {
                        whitePathData.Add(segCloseWhite);
                        blackPathData.Add(segCloseBlack);
                    }
                }
            }

            _whitePath.Fill = Brushes.White;
            InsertPathInCanvas(UiCnvWhite, _whiteFigure, _whiteGeometry, _whitePath, whitePathData, new Point(0, _maxY));

            _blackPath.Fill = Brushes.DarkGray;
            InsertPathInCanvas(UiCnvBlack, _blackFigure, _blackGeometry, _blackPath, blackPathData, new Point(0, 0));

#if false
            /////
            Ellipse marker = new Ellipse();

            // Set the width and height of the ellipse (adjust values as needed)
            marker.Width = 5;
            marker.Height = 5;

            // Set the fill color of the ellipse (replace with desired color)
            marker.Fill = Brushes.Blue;

            // Set the stroke of the ellipse (optional)
            marker.Stroke = Brushes.Black;
            marker.StrokeThickness = 1; // Set stroke thickness (optional)

            UiCnvBlack.Children.Add(marker);
            Canvas.SetLeft(marker, 20);
            Canvas.SetTop(marker, 20);
#endif
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
                if (double.TryParse(node.EngineEvaluation, out double dVal))
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
                    double y = (dVal / _maxEval) * _maxY;

                    p = new Point(x, y);
                }
            }

            return p;
        }

        /// <summary>
        /// Inserts a path into a canvas.
        /// </summary>
        /// <param name="cnv"></param>
        /// <param name="path"></param>
        /// <param name="pathData"></param>
        /// <param name="startPoint"></param>
        private void InsertPathInCanvas(Canvas cnv, PathFigure figure, PathGeometry geometry, Path path, List<PathSegment> pathData, Point startPoint)
        {
            path.Data = GeneratePathGeometry(geometry, figure, startPoint, pathData);
            path.StrokeThickness = 1;
            path.Stroke = Brushes.Red;

            cnv.Children.Add(path);
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
                segWhite = new LineSegment(new Point(p.Value.X, 100 - p.Value.Y), true);
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
        private void BuildCloseSegments(TreeNode nd, out LineSegment segWhite, out LineSegment segBlack)
        {
            Point? p = GetPointForMove(nd);
            if (p.HasValue)
            {
                segWhite = new LineSegment(new Point(p.Value.X, 100), false);
                segBlack = new LineSegment(new Point(p.Value.X, 0), false);
            }
            else
            {
                segWhite = null;
                segBlack = null;
            }
        }
    }
}
