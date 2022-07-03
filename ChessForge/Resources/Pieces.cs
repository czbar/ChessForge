using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace ChessForge
{
    public class Pieces
    {
        public static BitmapImage WhiteKing = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/WhiteKing.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage WhiteQueen = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/WhiteQueen.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage WhiteBishop = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/WhiteBishop.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage WhiteKnight = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/WhiteKnight.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage WhiteRook = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/WhiteRook.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage WhitePawn = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/WhitePawn.png", UriKind.RelativeOrAbsolute));

        public static BitmapImage BlackKing = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/BlackKing.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage BlackQueen = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/BlackQueen.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage BlackBishop = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/BlackBishop.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage BlackKnight = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/BlackKnight.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage BlackRook = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/BlackRook.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage BlackPawn = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/BlackPawn.png", UriKind.RelativeOrAbsolute));
    }
}
