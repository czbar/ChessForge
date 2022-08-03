using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessForge
{
    /// <summary>
    /// Manages the application state and transitions between states.
    /// The App State is an aggregation of the Learning Mode, Evaluation State
    /// and Game State.
    /// 
    /// The combination of those values determines what actions are available 
    /// to the user what GUI controls are shown etc.
    /// 
    /// The Learning Mode can be MANUAL_REVIEW or TRAINING (also IDLE if no file
    /// is loaded).
    /// 
    /// The Evaluation State determines whether any evaluation is being run at all
    /// and if so, whether this is a single move or line evaluation. 
    /// 
    /// Within either Learning Mode, there can be a game played by the user against 
    /// the engine. While being played, the game will be in one of a few modes e.g.
    /// ENGINE_THINKING or USER_THINKING.
    /// 
    /// </summary>
    public class AppStateManager
    {
        private static MainWindow _mainWin;

        public static MainWindow MainWin { get => _mainWin; set => _mainWin = value; }

        /// <summary>
        /// Current Learning Mode
        /// </summary>
        public static LearningMode.Mode CurrentLearningMode
        {
            get { return LearningMode.CurrentMode; }
        }

        /// <summary>
        /// Current Evaluation State
        /// </summary>
        public static EvaluationState.EvaluationMode CurrentEvaluationState
        {
            get { return MainWin.Evaluation.CurrentMode; }
        }

        /// <summary>
        /// Current Game State.
        /// </summary>
        public static EngineGame.GameState CurrentGameState
        {
            get { return EngineGame.CurrentState; }
        }

        /// <summary>
        /// Adjusts the GUI to the changed Learning Mode.
        /// </summary>
        /// <param name="newMode"></param>
        public static void ChangeLearningMode(LearningMode.Mode newMode)
        {
        }

        /// <summary>
        /// Adjusts the GUI to the changed Evaluation state.
        /// The state of the GUI will depend on the Learning Mode
        /// and Game State.
        /// </summary>
        /// <param name="newMode"></param>
        public static void ChangeEvaluationState(EvaluationState.EvaluationMode newMode)
        {
        }

        /// <summary>
        /// Adjusts the GUI to the changed game state.
        /// The state of the GUI will depend on the Learning Mode.
        /// </summary>
        /// <param name="newState"></param>
        public static void ChangeGameState(EngineGame.GameState newState)
        {
        }
    }
}
