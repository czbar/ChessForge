using GameTree;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ChessForge
{
    /// <summary>
    /// Handles generation of diagram images.
    /// </summary>
    public class PositionImageGenerator
    {
        /// <summary>
        /// Generates a png image for the passed position
        /// to use in the RTF export
        /// </summary>
        /// <param name="nd"></param>
        public static byte[] GenerateImage(TreeNode node, bool isFlipped)
        {
            BitmapEncoder encoder = EncodePositionAsImage(node, isFlipped);

            byte[] bytes;
            using (MemoryStream ms = new MemoryStream())
            {
                encoder.Save(ms);
                bytes = ms.ToArray();
            }

            return bytes;
        }

        /// <summary>
        /// Saves the diagram as an image file
        /// for the "Save Diagram as Picture..." menu item.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="isFlipped"></param>
        /// <param name="filePath"></param>
        /// <param name="pixelSize"></param>
        /// <param name="dpi"></param>
        public static void SaveDiagramAsImage(TreeNode node, bool isFlipped, string filePath, int pixelSize = 240, double dpi = 96)
        {
            BitmapEncoder encoder = EncodePositionAsImage(node, isFlipped, Configuration.DiagramImageBorderWidth, Configuration.DiagramImageColors, pixelSize, dpi);

            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            {
                encoder.Save(fs);
            }
        }

        /// <summary>
        /// Encodes the position as an image.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="isFlipped"></param>
        /// <param name="pixelSize"></param>
        /// <param name="dpi"></param>
        /// <returns></returns>
        private static BitmapEncoder EncodePositionAsImage(TreeNode node, bool isFlipped, int borderWidth = 2, int colorId = 1, int pixelSize = 240, double dpi = 96)
        {
            const int maxSmallChessBoardSize = 240;

            int smallBoardBaseSize = 240 + 2 * borderWidth;
            int largeBoardBaseSize = 640 + 2 * borderWidth;

            bool useSmallBoard = pixelSize <= maxSmallChessBoardSize;

            // Load chessboard image
            Image imageChessboard = new Image();

            switch(colorId)
            {
                case 1:
                    imageChessboard.Source = useSmallBoard ? ChessBoards.ChessBoardGreySmall : ChessBoards.ChessBoardGrey;
                    break;
                case 2:
                    imageChessboard.Source = useSmallBoard ? ChessBoards.ChessBoardBlueSmall : ChessBoards.ChessBoardBlue;
                    break;
                case 3:
                    imageChessboard.Source = useSmallBoard ? ChessBoards.ChessBoardBrownSmall : ChessBoards.ChessBoardBrown;
                    break;
                case 4:
                    imageChessboard.Source = useSmallBoard ? ChessBoards.ChessBoardGreenSmall : ChessBoards.ChessBoardGreen;
                    break;
                case 5:
                    imageChessboard.Source = useSmallBoard ? ChessBoards.ChessBoardVistulaSmall : ChessBoards.ChessBoardVistula;
                    break;
                case 6:
                    imageChessboard.Source = useSmallBoard ? ChessBoards.ChessBoardLightBlueSmall : ChessBoards.ChessBoardLightBlue;
                    break;
                default:
                    imageChessboard.Source = useSmallBoard ? ChessBoards.ChessBoardGreySmall : ChessBoards.ChessBoardGrey;
                    break;
            }

            Canvas mainCanvas = new Canvas();
            mainCanvas.Background = Brushes.Black;

            Canvas boardCanvas = new Canvas();
            boardCanvas.Children.Add(imageChessboard);

            mainCanvas.Children.Add(boardCanvas);
            Canvas.SetLeft(boardCanvas, borderWidth);
            Canvas.SetTop(boardCanvas, borderWidth);

            ChessBoard chessBoard = useSmallBoard ? new ChessBoardSmall(boardCanvas, imageChessboard, null, null, false, false)
                                                  : new ChessBoard(false, boardCanvas, imageChessboard, null, false, false);
            chessBoard.EnableShapes(true, node);
            chessBoard.DisplayPosition(node, true);

            if (isFlipped)
            {
                chessBoard.FlipBoard();
            }

            int sizeToUse = useSmallBoard ? smallBoardBaseSize : largeBoardBaseSize;
            mainCanvas.Measure(new Size(sizeToUse, sizeToUse));
            mainCanvas.Arrange(new Rect(new Size(sizeToUse, sizeToUse)));

            mainCanvas.UpdateLayout();

            RenderTargetBitmap originalBitmap = new RenderTargetBitmap(sizeToUse, sizeToUse, 96, 96, PixelFormats.Pbgra32);
            originalBitmap.Render(mainCanvas);

            double scale = (double)pixelSize / sizeToUse;
            RenderTargetBitmap scaledBitmap = HighQualityScaleAndSave(originalBitmap, scale);

            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(scaledBitmap));
            return encoder;
        }

        /// <summary>
        /// Scales the original bitmap to the desired size.
        /// </summary>
        /// <param name="originalBitmap"></param>
        /// <param name="scale"></param>
        /// <returns></returns>
        private static RenderTargetBitmap HighQualityScaleAndSave(BitmapSource originalBitmap, double scale)
        {
            var scaledBitmap = new RenderTargetBitmap(
                (int)(originalBitmap.PixelWidth * scale),
                (int)(originalBitmap.PixelHeight * scale),
                originalBitmap.DpiX,
                originalBitmap.DpiY,
                PixelFormats.Pbgra32);

            // Create a drawing visual with the exact scale we want
            var drawingVisual = new DrawingVisual();
            RenderOptions.SetBitmapScalingMode(drawingVisual, BitmapScalingMode.HighQuality);
            using (var drawingContext = drawingVisual.RenderOpen())
            {
                // Apply only one scale transform
                drawingContext.PushTransform(new ScaleTransform(scale, scale));
                drawingContext.DrawImage(originalBitmap,
                    new Rect(0, 0, originalBitmap.PixelWidth, originalBitmap.PixelHeight));
            }

            // Render to the bitmap
            scaledBitmap.Render(drawingVisual);
            return scaledBitmap;
        }
    }
}
