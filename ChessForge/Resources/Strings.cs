using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using ChessForge.Properties;

namespace ChessForge
{
    public class Strings
    {
        /// <summary>
        /// Returns a string from the App's resources.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string GetResource(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }

            string res = Resources.ResourceManager.GetString(key);
            return res ?? "[-]";
        }

        /// <summary>
        /// Notes for the Comments Box in the Chapters view.
        /// </summary>
        public static string QuickInstructionForChapters
        {
            get
            {
                return
                      Properties.Resources.cbClickStudyHeader + "\n"
                    + Properties.Resources.cbClickForContextMenu;
            }
        }

        /// <summary>
        /// Notes for the Comments Box in the Study view.
        /// </summary>
        public static string QuickInstructionForIntro
        {
            get
            {
                return
                      Properties.Resources.cbEditText + "\n"
                    + Properties.Resources.cbEditCommands;
            }
        }

        /// <summary>
        /// Notes for the Comments Box in the Study view.
        /// </summary>
        public static string QuickInstructionForStudy
        {
            get
            {
                return
                      Properties.Resources.cbClickThruMoves + "\n"
                    + Properties.Resources.cbFindIdenticalPositions + "\n"
                    + Properties.Resources.cbDoubleClickToAnnotate + "\n"
                    + Properties.Resources.cbAdvancedEditig + "\n "
                    + Properties.Resources.cbDoubleClickAutoReplay;
            }
        }

        /// <summary>
        /// Notes for the Comments Box in the Bookmarks view.
        /// </summary>
        public static string QuickInstructionForBookmarks
        {
            get
            {
                return
                      Properties.Resources.cbClickBookmarkToTrain + "\n"
                    + Properties.Resources.cbRightClickBookmarkManager;
            }
        }

        /// <summary>
        /// Notes for the Comments Box in the Games view
        /// when there are no games..
        /// </summary>
        public static string QuickInstructionForGamesEmpty
        {
            get
            {
                return Properties.Resources.cbRightClickCreateGame;
            }
        }

        /// <summary>
        /// Notes for the Comments Box in the Games view.
        /// </summary>
        public static string QuickInstructionForGames
        {
            get
            {
                return
                      Properties.Resources.cbClickThruMoves + "\n"
                    + Properties.Resources.cbFindIdenticalPositions + "\n"
                    + Properties.Resources.cbDoubleClickToAnnotate + "\n"
                    + Properties.Resources.cbRightClickCreateGame;
            }
        }

        /// <summary>
        /// Notes for the Comments Box in the Exercise view
        /// when there are no exercises..
        /// </summary>
        public static string QuickInstructionForExercisesEmpty
        {
            get
            {
                return Properties.Resources.cbRightClickCreateExercise;
            }
        }

        /// <summary>
        /// Notes for the Comments Box in the Exercises view.
        /// </summary>
        public static string QuickInstructionForExercises
        {
            get
            {
                return
                    Properties.Resources.cbMoveToEnterSolution + "\n"
                  + Properties.Resources.cbFindIdenticalPositions + "\n"
                  + Properties.Resources.cbDoubleClickToAssignPoints + "\n"
                  + Properties.Resources.cbSelectSolvingMove + "\n"
                  + Properties.Resources.cbRightClickEditExercise;
            }
        }

        /// <summary>
        /// Notes for the Comments Box in the Exercises view
        /// when the solution is hidden.
        /// </summary>
        public static string QuickInstructionForExercisesHiddenSolution
        {
            get
            {
                return
                    Properties.Resources.cbShowSolution + "\n"
                  + Properties.Resources.cbSelectSolvingMove + "\n"
                  + Properties.Resources.cbRightClickEditExercise;
            }
        }

        /// <summary>
        /// Notes for the Comments Box in the Exercises view
        /// while solving.
        /// </summary>
        public static string QuickInstructionForExerciseSolving
        {
            get
            {
                return Properties.Resources.cbMakeMovesOnMainBoard;
            }
        }
    }
}
