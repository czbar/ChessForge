using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace ChessForge
{
    /// <summary>
    /// Images for the main chess boards
    /// </summary>
    public class ChessBoards
    {
        public static BitmapImage ChessBoardBlue = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ChessBoardBlue.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage ChessBoardLightBlue = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ChessBoardLightBlue.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage ChessBoardLightGreen = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ChessBoardLightGreen.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage ChessBoardBlueSmall = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ChessBoardBlueSmall.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage ChessBoardBrown = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ChessBoardBrown.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage ChessBoardGreen = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ChessBoardGreen.png", UriKind.RelativeOrAbsolute));
    }

    /// <summary>
    /// Images for the arrows to be drawn on the board.
    /// </summary>
    public class ChessBoardArrows
    {
        public static BitmapImage YellowTriangle = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ArrowTriangleYellow.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage YellowStem = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ArrowStemYellow.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage YellowHalfCircle = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ArrowHalfCircleYellow.png", UriKind.RelativeOrAbsolute));

        public static BitmapImage GreenTriangle = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ArrowTriangleGreen.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage GreenStem = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ArrowStemGreen.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage GreenHalfCircle = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ArrowHalfCircleGreen.png", UriKind.RelativeOrAbsolute));

        public static BitmapImage BlueTriangle = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ArrowTriangleBlue.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage BlueStem = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ArrowStemBlue.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage BlueHalfCircle = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ArrowHalfCircleBlue.png", UriKind.RelativeOrAbsolute));

        public static BitmapImage RedTriangle = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ArrowTriangleRed.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage RedStem = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ArrowStemRed.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage RedHalfCircle = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ArrowHalfCircleRed.png", UriKind.RelativeOrAbsolute));
    }

    /// <summary>
    /// Images for the square selection circles on the board.
    /// </summary>
    public class ChessBoardCircles
    {
        public static BitmapImage YellowCircle = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/CircleYellow.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage GreenCircle = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/CircleGreen.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage BlueCircle = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/CircleBlue.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage RedCircle = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/CircleRed.png", UriKind.RelativeOrAbsolute));
    }
}
