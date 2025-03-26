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
        /// Saves the diagram as an image file.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="isFlipped"></param>
        /// <param name="filePath"></param>
        /// <param name="pixelSize"></param>
        /// <param name="dpi"></param>
        public static void SaveDiagramAsImage(TreeNode node, bool isFlipped, string filePath, int pixelSize = 240, double dpi = 96)
        {
            BitmapEncoder encoder = EncodePositionAsImage(node, isFlipped, pixelSize, dpi);

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
        private static BitmapEncoder EncodePositionAsImage(TreeNode node, bool isFlipped, int pixelSize = 240, double dpi = 96)
        {
            // Load chessboard image
            Image imageChessboard = new Image();
            imageChessboard.Source = ChessBoards.ChessBoardGreySmall;

            Canvas mainCanvas = new Canvas();
            mainCanvas.Background = Brushes.Black;

            Canvas boardCanvas = new Canvas();
            boardCanvas.Children.Add(imageChessboard);

            mainCanvas.Children.Add(boardCanvas);
            Canvas.SetLeft(boardCanvas, 1);
            Canvas.SetTop(boardCanvas, 1);

            ChessBoardSmall chessBoard = new ChessBoardSmall(boardCanvas, imageChessboard, null, null, false, false);
            chessBoard.EnableShapes(true, node);
            chessBoard.DisplayPosition(node, true);

            if (isFlipped)
            {
                chessBoard.FlipBoard();
            }

            mainCanvas.Measure(new Size(242, 242));
            mainCanvas.Arrange(new Rect(new Size(242, 242)));
            mainCanvas.UpdateLayout();

            RenderTargetBitmap originalBitmap = new RenderTargetBitmap(242, 242, 96, 96, PixelFormats.Pbgra32);
            originalBitmap.Render(mainCanvas);

            double scale = (double)pixelSize / 240;
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
                originalBitmap.DpiX,                      // Keep original DPI
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
