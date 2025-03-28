using System;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using static ChessForge.ChessBoards;

namespace ChessForge
{
    /// <summary>
    /// Images for the main chess boards
    /// </summary>
    public class ChessBoards
    {
        /// <summary>
        /// Identifiers of board sets.
        /// NEVER change these numbers as they are used in the config file!!
        /// </summary>
        public enum ColorSet
        {
            BLUE = 1,
            LIGHT_BLUE = 2,
            LIGHT_GREEN = 3,
            GREEN = 4,
            PALE_BLUE = 5,
            BROWN = 6,
            ORANGE_SHADES = 7,
            GREY = 8
        }

        /// <summary>
        /// Supported chessboard sizes define by the square size.
        /// </summary>
        public enum SquareSize
        {
            SIZE_15 = 15,
            SIZE_18 = 18,
            SIZE_20 = 20,
            SIZE_30 = 30,
            SIZE_45 = 45,
            SIZE_60 = 60,
            SIZE_80 = 80,
        }

        public static BitmapImage ChessBoardBlue = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ChessBoardBlue.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage ChessBoardBlueSmall = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ChessBoardBlueSmall.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage ChessBoardBlueIcon = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ChessBoardBlueIcon.png", UriKind.RelativeOrAbsolute));

        public static BitmapImage ChessBoardLightBlue = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ChessBoardLightBlue.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage ChessBoardLightBlueSmall = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ChessBoardLightBlueSmall.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage ChessBoardLightBlueIcon = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ChessBoardLightBlueIcon.png", UriKind.RelativeOrAbsolute));

        public static BitmapImage ChessBoardLightGreen = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ChessBoardLightGreen.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage ChessBoardLightGreenSmall = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ChessBoardLightGreenSmall.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage ChessBoardLightGreenIcon = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ChessBoardLightGreenIcon.png", UriKind.RelativeOrAbsolute));

        public static BitmapImage ChessBoardGrey = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ChessBoardGrey.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage ChessBoardGreySmall = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ChessBoardGreySmall.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage ChessBoardGreyIcon = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ChessBoardGreyIcon.png", UriKind.RelativeOrAbsolute));

        public static BitmapImage ChessBoardGreen = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ChessBoardGreen.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage ChessBoardGreenSmall = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ChessBoardGreenSmall.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage ChessBoardGreenIcon = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ChessBoardGreenIcon.png", UriKind.RelativeOrAbsolute));

        public static BitmapImage ChessBoardPaleBlue = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ChessBoardPaleBlue.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage ChessBoardPaleBlueSmall = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ChessBoardPaleBlueSmall.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage ChessBoardPaleBlueIcon = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ChessBoardPaleBlueIcon.png", UriKind.RelativeOrAbsolute));

        public static BitmapImage ChessBoardBrown = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ChessBoardBrown.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage ChessBoardBrownSmall = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ChessBoardBrownSmall.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage ChessBoardBrownIcon = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ChessBoardBrownIcon.png", UriKind.RelativeOrAbsolute));

        public static BitmapImage ChessBoardOrangeShades = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ChessBoardOrangeShades.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage ChessBoardOrangeShadesSmall = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ChessBoardOrangeShadesSmall.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage ChessBoardOrangeShadesIcon = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ChessBoardOrangeShadesIcon.png", UriKind.RelativeOrAbsolute));

        /// <summary>
        /// Preconfigured board color sets.
        /// </summary>
        public static Dictionary<ColorSet, BoardSet> BoardSets = new Dictionary<ColorSet, BoardSet>()
        {
            {ColorSet.BLUE,          new BoardSet(ColorSet.BLUE, ChessBoardBlue, ChessBoardBlueSmall, ChessBoardBlueIcon) },
            {ColorSet.LIGHT_BLUE,    new BoardSet(ColorSet.LIGHT_BLUE, ChessBoardLightBlue, ChessBoardLightBlueSmall, ChessBoardLightBlueIcon) },
            {ColorSet.LIGHT_GREEN,    new BoardSet(ColorSet.LIGHT_GREEN, ChessBoardLightGreen, ChessBoardLightGreenSmall, ChessBoardLightGreenIcon) },
            {ColorSet.GREEN,         new BoardSet(ColorSet.GREEN, ChessBoardGreen, ChessBoardGreenSmall, ChessBoardGreenIcon) },
            {ColorSet.PALE_BLUE,     new BoardSet(ColorSet.PALE_BLUE, ChessBoardPaleBlue, ChessBoardPaleBlueSmall, ChessBoardPaleBlueIcon) },
            {ColorSet.BROWN,         new BoardSet(ColorSet.BROWN, ChessBoardBrown, ChessBoardBrownSmall, ChessBoardBrownIcon) },
            {ColorSet.ORANGE_SHADES, new BoardSet(ColorSet.ORANGE_SHADES, ChessBoardOrangeShades, ChessBoardOrangeShadesSmall, ChessBoardOrangeShadesIcon) },
            {ColorSet.GREY,          new BoardSet(ColorSet.GREY, ChessBoardGrey, ChessBoardGreySmall, ChessBoardGreyIcon) },
        };

    }

    /// <summary>
    /// Board set configuration.
    /// </summary>
    public class BoardSet
    {
        public BitmapImage MainBoard;
        public BitmapImage SmallBoard;
        public BitmapImage Icon;
        public ColorSet Colors;

        public BoardSet(ColorSet colors, BitmapImage main, BitmapImage small, BitmapImage icon)
        {
            MainBoard = main;
            SmallBoard = small;
            Icon = icon;
            Colors = colors;
        }
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

        public static BitmapImage OrangeTriangle = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ArrowTriangleOrange.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage OrangeStem = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ArrowStemOrange.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage OrangeHalfCircle = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ArrowHalfCircleOrange.png", UriKind.RelativeOrAbsolute));

        public static BitmapImage PurpleTriangle = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ArrowTrianglePurple.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage PurpleStem = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ArrowStemPurple.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage PurpleHalfCircle = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ArrowHalfCirclePurple.png", UriKind.RelativeOrAbsolute));

        public static BitmapImage DarkredTriangle = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ArrowTriangleDarkred.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage DarkredStem = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ArrowStemDarkred.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage DarkredHalfCircle = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ArrowHalfCircleDarkred.png", UriKind.RelativeOrAbsolute));

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

        public static BitmapImage OrangeCircle = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/CircleOrange.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage PurpleCircle = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/CirclePurple.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage DarkredCircle = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/CircleDarkred.png", UriKind.RelativeOrAbsolute));
    }
}
