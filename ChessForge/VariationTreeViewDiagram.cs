using GameTree;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace ChessForge
{
    /// <summary>
    /// Utilities for creating an inline diagram in the Variation Tree views.
    /// </summary>
    public class VariationTreeViewDiagram
    {
        /// <summary>
        /// The length of the ViewBox's side on the main line
        /// </summary>
        private static int VIEWBOX_MAIN_LINE_SIDE_LENGTH = 160;

        /// <summary>
        /// The length of the ViewBox's side on a side line
        /// </summary>
        private static int VIEWBOX_SIDE_LINE_SIDE_LENGTH = 140;

        /// <summary>
        /// The actual width and height of the image to use for the chessboard 
        /// </summary>
        private static int CHESSBOARD_IMAGE_SIDE_LENGTH = 240;

        /// <summary>
        /// The margin between the chessboard and the canvas.
        /// </summary>
        private static int CHESSBOARD_MARGIN = 20;

        /// <summary>
        /// Addition left margin between the chessboard 
        /// and the canvas.
        /// </summary>
        private static int CHESSBOARD_EXTRA_LEFT_MARGIN = 20;

        /// <summary>
        /// The width of the chessboard's border.
        /// </summary>
        private static int CHESSBOARD_BOARD_WIDTH = 2;

        /// <summary>
        /// Builds an InlineUIContainer showing the diagram for
        /// the passed position
        /// </summary>
        /// <param name="nd"></param>
        /// <returns></returns>
        public static InlineUIContainer CreateDiagram(TreeNode nd, out ChessBoardSmall chessboard, bool largeDiagram)
        {
            InlineUIContainer iuc = null;
            chessboard = null;

            if (nd != null)
            {
                try
                {
                    Canvas baseCanvas = SetupDiagramCanvas(CHESSBOARD_IMAGE_SIDE_LENGTH);
                    Image imgChessBoard = CreateChessBoard(baseCanvas, out chessboard);
                    if (nd.IsDiagramFlipped)
                    {
                        chessboard.FlipBoard();
                    }
                    baseCanvas.Children.Add(imgChessBoard);
                    
                    Viewbox viewBox = SetupDiagramViewbox(baseCanvas, largeDiagram);

                    iuc = new InlineUIContainer();
                    iuc.Child = viewBox;
                    iuc.Name = RichTextBoxUtilities.InlineDiagramIucPrefix + nd.NodeId.ToString();
                }
                catch
                {
                }
            }

            return iuc;
        }

        /// <summary>
        /// Creates a Canvas for the chessboard. 
        /// </summary>
        /// <returns></returns>
        private static Canvas SetupDiagramCanvas(int imageSide)
        {
            Canvas topCanvas = new Canvas();
            topCanvas.Background = Brushes.Transparent;
            topCanvas.Width = imageSide + CHESSBOARD_MARGIN + CHESSBOARD_EXTRA_LEFT_MARGIN;
            topCanvas.Height = imageSide + CHESSBOARD_MARGIN;

            Canvas inCanvas = new Canvas();
            inCanvas.Background = Brushes.Black;
            inCanvas.Width = imageSide + CHESSBOARD_BOARD_WIDTH * 2;
            inCanvas.Height = imageSide + CHESSBOARD_BOARD_WIDTH * 2;
            inCanvas.Margin = new Thickness((CHESSBOARD_MARGIN / 2 + CHESSBOARD_EXTRA_LEFT_MARGIN) - CHESSBOARD_BOARD_WIDTH, CHESSBOARD_MARGIN / 2 - CHESSBOARD_BOARD_WIDTH, CHESSBOARD_MARGIN / 2 - CHESSBOARD_BOARD_WIDTH, CHESSBOARD_MARGIN / 2 - CHESSBOARD_BOARD_WIDTH);

            topCanvas.Children.Add(inCanvas);

            return topCanvas;
        }

        /// <summary>
        /// Creates the chessboard control.
        /// </summary>
        /// <param name="canvas"></param>
        /// <returns></returns>
        private static Image CreateChessBoard(Canvas canvas, out ChessBoardSmall chessboard)
        {
            Image imgChessBoard = new Image();
            imgChessBoard.Margin = new Thickness((CHESSBOARD_MARGIN / 2) + CHESSBOARD_EXTRA_LEFT_MARGIN, CHESSBOARD_MARGIN / 2, CHESSBOARD_MARGIN / 2, CHESSBOARD_MARGIN / 2);
            imgChessBoard.Source = ChessBoards.ChessBoardGreySmall;

            chessboard = new ChessBoardSmall(canvas, imgChessBoard, null, null, false, false);

            return imgChessBoard;
        }

        /// <summary>
        /// Creates a Viewbox for the chessboard
        /// </summary>
        /// <param name="canvas"></param>
        /// <returns></returns>
        private static Viewbox SetupDiagramViewbox(Canvas canvas, bool largeDiagram)
        {
            Viewbox viewBox = new Viewbox();
            viewBox.Child = canvas;
            viewBox.Width = largeDiagram ? VIEWBOX_MAIN_LINE_SIDE_LENGTH : VIEWBOX_SIDE_LINE_SIDE_LENGTH;
            viewBox.Height = largeDiagram ? VIEWBOX_MAIN_LINE_SIDE_LENGTH : VIEWBOX_SIDE_LINE_SIDE_LENGTH;
            viewBox.Visibility = Visibility.Visible;

            return viewBox;
        }



    }
}
