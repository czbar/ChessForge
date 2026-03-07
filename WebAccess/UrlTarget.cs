namespace WebAccess
{
    /// <summary>
    /// Contains urls for accessing various web resources related to chess and Chess Forge.
    /// </summary>
    public class UrlTarget
    {
        /// <summary>
        /// Url of the file with information about Chess Forge's releases. 
        /// It contains version numbers for the latest releases at Source Forge and Microsoft App Store, as well as messages to be displayed to the user, if any.
        /// </summary>
        public static string ChessForgeReleasesFile = @"https://chessforge.sourceforge.io/Releases/Releases.json";

        /// <summary>
        /// Url of a small file that is used to count daily active users.
        /// </summary>
        public static string ChessForgePingFile = @"https://sourceforge.net/projects/chessforge/files/ping.txt";

        /// <summary>
        /// List of archives for a chess.com user. Each archive corresponds to one month of games and can be downloaded using the ChesscomGames url below.
        /// </summary>
        public static string ChesscomArchiveList = @"https://api.chess.com/pub/player/{0}/games/archives";

        /// <summary>
        /// Chess.com url for downloading games for a user for a given month. 
        /// The {1} and {2} parameters are year and month, which can be obtained from the archive list url above.
        /// </summary>
        public static string ChesscomGames = @"https://api.chess.com/pub/player/{0}/games/{1}/{2}/pgn";

        /// <summary>
        /// Lichess url for creating an authorization token. 
        /// The user needs to create a token and copy it to Chess Forge in order to access the Opening Explorer.
        /// </summary>
        public static string LichessCreateAuthToken = @"https://lichess.org/account/oauth/token/create" + "?description=Chess Forge Authorization";

        /// <summary>
        /// Url for downloading a game from lichess.org. It is followed by the game id.
        /// </summary>
        public static string LichessGameDownload = @"https://lichess.org/game/export/";

        /// <summary>
        /// Url for downloading games of a user from lichess.org. The {0} argument is replaced by the use name.
        /// </summary>
        public static string LichessUserGames = @"https://lichess.org/api/games/user/{0}";

        /// <summary>
        /// Url for lichess opening explorer. 
        /// It is followed by the FEN of the position we want to get stats for.
        /// </summary>
        public static string LichessOpeningExplorer = @"https://explorer.lichess.ovh/masters?";

        /// <summary>
        /// Url for lichess tablebase lookup. It is followed by the FEN of the position we want to look up.
        /// </summary>
        public static string LichessTablebaseLookup = @"http://tablebase.lichess.ovh/standard?";

        /// <summary>
        /// Url of Chess Forge's public library.
        /// </summary>
        public static string PublicLibrary = @"https://chessforge.sourceforge.io/Library/";
    }
}
