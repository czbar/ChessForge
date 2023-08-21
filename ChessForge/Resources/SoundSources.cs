using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace ChessForge
{
    /// <summary>
    /// Chessboard sounds.
    /// </summary>
    public class SoundSources
    {
        public static Uri Move = new Uri("pack://siteoforigin:,,,/Resources/Sounds/Move.mp3", UriKind.RelativeOrAbsolute);
        public static Uri Capture = new Uri("pack://siteoforigin:,,,/Resources/Sounds/Capture.mp3", UriKind.RelativeOrAbsolute);
        public static Uri InvalidMove = new Uri("pack://siteoforigin:,,,/Resources/Sounds/InvalidMove.mp3", UriKind.RelativeOrAbsolute);
        public static Uri EndOfLine = new Uri("pack://siteoforigin:,,,/Resources/Sounds/EndOfLine.mp3", UriKind.RelativeOrAbsolute);
        public static Uri NotInWorkbook = new Uri("pack://siteoforigin:,,,/Resources/Sounds/NotInWorkbook.mp3", UriKind.RelativeOrAbsolute);
        public static Uri Confirmation = new Uri("pack://siteoforigin:,,,/Resources/Sounds/Confirmation.mp3", UriKind.RelativeOrAbsolute);
    }
}
