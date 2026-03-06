using ChessForge;
using System;
using System.Collections.Generic;
using System.Media;
using System.Windows;

public static class SoundManager
{
    /// <summary>
    /// Holds loaded sound players, keyed by a string identifier.
    /// </summary>
    private static readonly Dictionary<string, SoundPlayer> _sounds = new Dictionary<string, SoundPlayer>();

    /// <summary>
    /// Initializes the SoundManager by loading all necessary sound resources.
    /// </summary>
    public static void Initialize()
    {
        Load("Move", new Uri("/Resources/Sounds/Move.wav", UriKind.Relative));
        Load("Capture", new Uri("/Resources/Sounds/Capture.wav", UriKind.Relative));
        Load("InvalidMove", new Uri("/Resources/Sounds/InvalidMove.wav", UriKind.Relative));
        Load("EndOfLine", new Uri("/Resources/Sounds/EndOfLine.wav", UriKind.Relative));
        Load("NotInWorkbook", new Uri("/Resources/Sounds/NotInWorkbook.wav", UriKind.Relative));
        Load("Confirmation", new Uri("/Resources/Sounds/Confirmation.wav", UriKind.Relative));
    }

    /// <summary>
    /// Plays the default move action.
    /// </summary>
    public static void PlayMove() => Play("Move");

    /// <summary>
    /// Plays the capture action.
    /// </summary>
    public static void PlayCapture() => Play("Capture");

    /// <summary>
    /// Plays the invalid move action.
    /// </summary>
    public static void PlayInvalidMove() => Play("InvalidMove");

    /// <summary>
    /// Plays the end of line action.
    /// </summary>
    public static void PlayEndOfLine() => Play("EndOfLine");

    /// <summary>
    /// Plays the not in workbook action.
    /// </summary>
    public static void PlayNotInWorkbook() => Play("NotInWorkbook");

    /// <summary>
    /// Plays the confirmation action.
    /// </summary>
    public static void PlayConfirmation() => Play("Confirmation");

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

        if (algMove.IndexOf('x') > 0)
        {
            PlayCapture();
        }
        else
        {
            PlayMove();
        }
    }

    /// <summary>
    /// Loads a sound resource from the specified URI and stores it in the _sounds dictionary with the given key.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="uri"></param>
    private static void Load(string key, Uri uri)
    {
        var res = Application.GetResourceStream(uri);

        if (res == null)
        {
            AppLog.Message(LogLevel.ERROR, $"Sound resource not found: {uri}");
        }
        else
        {
            var player = new SoundPlayer(res.Stream);
            player.Load();
            _sounds[key] = player;
        }
    }

    /// <summary>
    /// Plays the sound associated with the given key 
    /// if it exists in the _sounds dictionary and if sound is enabled in the configuration. 
    /// If the sound is currently playing, it will be stopped and replayed from the beginning.
    /// </summary>
    /// <param name="key"></param>
    private static void Play(string key)
    {
        if (!Configuration.SoundOn)
        {
            return;
        }

        if (_sounds.TryGetValue(key, out var player))
        {
            player.Stop();  // ensures replay if still playing
            player.Play();
        }
    }
}