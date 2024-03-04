using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessForge
{
    /// <summary>
    /// Speeds up browsing by monitoring the state of the mouse left button
    /// over Previous / Next arrows.
    /// </summary>
    public class ArticleBrowseBooster
    {
        // whether the browse booster is active
        private static bool _isBoosterActive = false;

        // current value of the pulse counter
        private static int _pulseCounter;

        // current value of the element index
        private static int _elementIndex;

        // type of Article we are dealing with
        private static GameData.ContentType _contentType;

        /// <summary>
        /// Called from the PulseManager.
        /// Updates the counter and, depending on the current value,
        /// updates the PrevNext GUI bar.
        /// </summary>
        public static void IncrementPulseCounter()
        {
            if (_isBoosterActive)
            {
                _pulseCounter++;

                // increment element index when counter is at 4 or higher
                if (_pulseCounter >= 4)
                {
                    // TODO: implement
                    // update the PrevNextBar

                    // if we reached the min or max boundary
                    // display the element and stop the counters 
                }
            }
        }

        /// <summary>
        /// A request to show the next article came from the main window.
        /// Show it and if it is not the last one, active "browse booster". 
        /// </summary>
        /// <param name="contentType"></param>
        public static void NextArticle(GameData.ContentType contentType)
        {
            _isBoosterActive=true;
        }

        /// <summary>
        /// A request to show the previous article came from the main window.
        /// Show it and if it is not the last one, active "browse booster". 
        /// </summary>
        /// <param name="contentType"></param>
        public static void PreviousArticle(GameData.ContentType contentType)
        {
            _isBoosterActive = true;
        }

        /// <summary>
        /// Stops the "browse booster".
        /// It will be called when we reached the min/max boundary, 
        /// or a mouse button up or mouse left event occured over the arrow
        /// that started the "browse booster".
        /// </summary>
        public static void StopCounting()
        {
            _isBoosterActive = false;
            _pulseCounter = 0;
        }
    }
}
