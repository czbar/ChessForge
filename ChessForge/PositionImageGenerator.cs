using GameTree;
using System;
using System.Collections.Generic;
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
            byte[] bytes;

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

            RenderTargetBitmap bmp = new RenderTargetBitmap(242, 242, 96, 96, PixelFormats.Pbgra32);
            bmp.Render(mainCanvas);
            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bmp));

            using (MemoryStream ms = new MemoryStream())
            {
                encoder.Save(ms);
                bytes = ms.ToArray();
            }

            return bytes;
        }
    }
}
