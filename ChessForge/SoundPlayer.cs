using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using GameTree;

namespace ChessForge
{
    /// <summary>
    /// Plays game sounds.
    /// </summary>
    public class SoundPlayer
    {
        // plays a simple move sound
        private static MediaPlayer _soundMove = new MediaPlayer();

        // plays a move-with-capture sound
        private static MediaPlayer _soundCapture = new MediaPlayer();

        // indicates if the players have been initialized yet
        private static bool _isInitialized = false;

        /// <summary>
        /// Reads in sound files.
        /// </summary>
        public static void Initialize()
        {
            _soundMove.Open(new Uri("Resources/Sounds/Move.mp3", UriKind.Relative));
            _soundCapture.Open(new Uri("Resources/Sounds/Capture.mp3", UriKind.Relative));
        }

        /// <summary>
        /// Plays a sound according to the type of move.
        /// If the move's notation includes the 'x' character
        /// it is considered a capture.
        /// </summary>
        /// <param name="algMove"></param>
        public static void PlayMoveSound(string algMove)
        {
            if (!_isInitialized)
            {
                Initialize();
                _isInitialized = true;
            }

            if (algMove.IndexOf('x') > 0)
            {
                _soundCapture.Dispatcher.Invoke(() =>
                {
                    _soundCapture.Position = TimeSpan.FromMilliseconds(0);
                    _soundCapture.Play();
                });
            }
            else
            {
                _soundMove.Dispatcher.Invoke(() =>
                {
                    _soundMove.Position = TimeSpan.FromMilliseconds(0);
                    _soundMove.Play();
                });
            }
        }

    }
}
