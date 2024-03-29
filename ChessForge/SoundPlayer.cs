﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
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
        public enum Sound
        {
            END_OF_LINE,
            NOT_IN_WORKBOOK
        }

        // plays a simple move sound
        private static MediaPlayer _soundMove = new MediaPlayer();

        // plays a move-with-capture sound
        private static MediaPlayer _soundCapture = new MediaPlayer();

        // plays the Wrong Move sound
        private static MediaPlayer _soundWrongMove = new MediaPlayer();

        // plays the End of Line sound
        private static MediaPlayer _soundEndOfLine = new MediaPlayer();

        // plays the Not in the Workbook sound
        private static MediaPlayer _soundNotInWorkbook = new MediaPlayer();

        // plays the Confirmation sound
        private static MediaPlayer _soundConfirmation = new MediaPlayer();


        // indicates if the players have been initialized yet
        private static bool _isInitialized = false;

        /// <summary>
        /// Reads in sound files.
        /// </summary>
        public static void Initialize()
        {
            _soundMove.Open(SoundSources.Move);
            _soundCapture.Open(SoundSources.Capture);
            _soundWrongMove.Open(SoundSources.InvalidMove);
            _soundEndOfLine.Open(SoundSources.EndOfLine);
            _soundNotInWorkbook.Open(SoundSources.NotInWorkbook);
            _soundConfirmation.Open(SoundSources.Confirmation);
        }

        /// <summary>
        /// Closes all open media
        /// </summary>
        public static void CloseAll()
        {
            try
            {
                _soundMove.Close();
                _soundCapture.Close();
                _soundWrongMove.Close();
                _soundEndOfLine.Close();
                _soundNotInWorkbook.Close();
                _soundConfirmation.Close();
            }
            catch { }
        }

        /// <summary>
        /// Plays a sound according to the type of move.
        /// If the move's notation includes the 'x' character
        /// it is considered a capture.
        /// </summary>
        /// <param name="algMove"></param>
        public static void PlayMoveSound(string algMove)
        {
            if (!Configuration.SoundOn)
            {
                return;
            }

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

        /// <summary>
        /// Plays the wrong move (error) sound.
        /// </summary>
        public static void PlayWrongMoveSound()
        {
            if (!Configuration.SoundOn)
            {
                return;
            }

            if (!_isInitialized)
            {
                Initialize();
                _isInitialized = true;
            }

            _soundWrongMove.Dispatcher.Invoke(() =>
            {
                _soundWrongMove.Position = TimeSpan.FromMilliseconds(0);
                _soundWrongMove.Play();
            });
        }


        /// <summary>
        /// Playes the Confirmation sound.
        /// </summary>
        public static void PlayConfirmationSound()
        {
            if (!Configuration.SoundOn)
            {
                return;
            }

            if (!_isInitialized)
            {
                Initialize();
                _isInitialized = true;
            }

            _soundConfirmation.Dispatcher.Invoke(() =>
            {
                _soundConfirmation.Position = TimeSpan.FromMilliseconds(0);
                _soundConfirmation.Play();
            });
        }

        /// <summary>
        /// Plays one of the sounds in the training session
        /// </summary>
        /// <param name="sound"></param>
        public static void PlayTrainingSound(Sound sound)
        {
            if (!Configuration.SoundOn)
            {
                return;
            }

            if (!_isInitialized)
            {
                Initialize();
                _isInitialized = true;
            }

            switch (sound)
            {
                case Sound.NOT_IN_WORKBOOK:
                    _soundNotInWorkbook.Dispatcher.Invoke(() =>
                    {
                        _soundNotInWorkbook.Position = TimeSpan.FromMilliseconds(0);
                        _soundNotInWorkbook.Play();
                    });
                    break;
                case Sound.END_OF_LINE:
                    _soundEndOfLine.Dispatcher.Invoke(() =>
                    {
                        _soundEndOfLine.Position = TimeSpan.FromMilliseconds(0);
                        _soundEndOfLine.Play();
                    });
                    break;
            }
        }
    }
}
