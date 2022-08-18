namespace EngineService
{
    public class UciCommands
    {
        // requests to the engine
        public const string ENG_UCI = "uci";
        public const string ENG_ISREADY = "isready";
        public const string ENG_UCI_NEW_GAME = "ucinewgame";
        public const string ENG_POSITION = "position startpos moves";
        public const string ENG_GO_MOVE_TIME = "go movetime";
        public const string ENG_STOP = "stop";

        // responses from the engine
        public const string ENG_UCI_OK = "uciok";
        public const string ENG_READY_OK = "readyok";
        public const string ENG_BEST_MOVE = "bestmove";

        // other strings
        public const string ENG_MULTIPV = "mpv";

        // prefix in the engine's message naming itself
        public const string ENG_ID_NAME = "id name";
    }
}

