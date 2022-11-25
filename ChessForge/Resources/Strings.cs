using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessForge
{
    public class Strings
    {
        public static string QUICK_INSTRUCTION_CHAPTERS = "Click Study header or double click chapter\'s title to open the Study Tree \n "
            + "Right click to open context menu";
        public static string QUICK_INSTRUCTION_STUDY = "Click through the moves \n Double click to annotate move \n "
            + "Right click to open menu for advanced editing, FEN, creating exercise \n " + "Double click in the Scoresheet for auto-replay";
        public static string QUICK_INSTRUCTION_MODEL_GAMES_NON_EMPTY = "Click through the moves \n Double click to annotate move \n "
            + "Right click to open menu for creating, importing or editing games";
        public static string QUICK_INSTRUCTION_MODEL_GAMES_EMPTY = "Click through the moves \n Double click to annotate move \n "
            + "Right click to open menu for creating, importing or editing games";
        public static string QUICK_INSTRUCTION_BOOKMARKS = "Click a bookmark to start training\n" + 
            "Right click to open menu for Bookmark management options";
        public static string QUICK_INSTRUCTION_EXERCISES_EMPTY = "Right click to open menu to create or import Exercise";
        public static string QUICK_INSTRUCTION_EXERCISES_EDIT = "Make moves on the main board to enter solution \n"
            + "Double click a move to assign quiz points\n" 
            + "Select Solving Mode\n"
            + "Right click to open menu to edit or create exercises";
        public static string QUICK_INSTRUCTION_EXERCISES_HIDDEN = "Show the solution to view or edit\n"
            + "Select Solving Mode\n"
            + "Right click to open menu to edit or create exercises";
        public static string QUICK_INSTRUCTION_EXERCISES_SOLVING = "Make your moves on the main board";

        public static string ROW_COLOR_WHITE = "white";
        public static string ROW_COLOR_BLACK = "black";

        public static string EXERCISE_THIS_IS_SOLUTION = "This is the solution!";
    }
}
