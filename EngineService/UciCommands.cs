namespace EngineService
{
    public class UciCommands
    {
        // requests to the engine
        public const string ENG_UCI = "uci";
        public const string ENG_ISREADY = "isready";
        public const string ENG_UCI_NEW_GAME = "ucinewgame";
        public const string ENG_POSITION = "position";
        public const string ENG_POSITION_FEN = "position fen";
        public const string ENG_POSITION_STARTPOS = "position startpos moves";
        public const string ENG_GO = "go";
        public const string ENG_GO_MOVE_TIME = "go movetime";
        public const string ENG_GO_INFINITE = "go infinite";
        public const string ENG_STOP = "stop";
        public const string ENG_SET_MULTIPV = "setoption name multipv value";

        public const string ENG_SET_OPTION = "setoption name {0} value {1}";

        // responses from the engine
        public const string ENG_UCI_OK = "uciok";
        public const string ENG_READY_OK = "readyok";
        public const string ENG_BEST_MOVE = "bestmove";
        public const string ENG_INFO = "info";
        public const string ENG_MULTIPV_1 = "multipv 1";
        public const string ENG_CURRMOVE = "currmove";
        public const string ENG_BESTMOVE_NONE = "none";
        public const string ENG_BESTMOVE_NONE_LEILA = "a1a1";

        // prefix in the engine's message naming itself
        public const string ENG_ID_NAME = "id name";

        // special ChessForge constants
        public const string CHF_TREE_ID_PREFIX = "TreeId=";
        public const string CHF_NODE_ID_PREFIX = "NodeId=";
        public const string CHF_EVAL_MODE_PREFIX = "Mode=";
        public const string CHF_DELAYED_PREFIX = "DEL";
    }
}

