using GameTree;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ChessForge
{
    public class PositionImageGenerator
    {
        /// <summary>
        /// Generates a png image for the passed position
        /// </summary>
        /// <param name="nd"></param>
        public static byte[] GenerateImage(TreeNode nd)
        {
            byte[] bytes;

            // Load chessboard image
            Image imageChessboard = new Image();
            imageChessboard.Source = ChessBoards.ChessBoardGreySmall;

            Canvas canvas = new Canvas();
            Rectangle border = new Rectangle();
            border.Height = 242;
            border.Width = 242;
            canvas.Children.Add(border);

            canvas.Children.Add(imageChessboard);
            Canvas.SetLeft(imageChessboard, 0);
            Canvas.SetTop(imageChessboard, 0);
            
            Image imagePiece = new Image();
            imagePiece.Source = PieceImageDictionaries.WhitePiecesSmall[ChessPosition.PieceType.King];
            canvas.Children.Add(imagePiece);
            Canvas.SetLeft(imagePiece, 100);
            Canvas.SetTop(imagePiece, 100);

            canvas.Measure(new Size(242, 242));
            canvas.Arrange(new Rect(new Size(242, 242)));
            canvas.UpdateLayout();

            RenderTargetBitmap bmp = new RenderTargetBitmap(242, 242, 96, 96, PixelFormats.Pbgra32);
            bmp.Render(canvas);
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
