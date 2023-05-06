using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessForge
{
    public class WorkbookLocationNavigator
    {
        /// <summary>
        /// The list of location history in this workbook session
        /// </summary>
        private static List<WorkbookLocation> _locations = new List<WorkbookLocation>();

        // index of the current location in the _locations list
        private int _currentLocationIndex = -1;

        /// <summary>
        /// Returns true if there is no newer location after the current one
        /// </summary>
        public bool IsLastLocation
        {
            get { return _currentLocationIndex == _locations.Count - 1; }
        }

        /// <summary>
        /// Returns true if there is no older location before the current one
        /// </summary>
        public bool IsFirstLocation
        {
            get { return _currentLocationIndex <= 0; }
        }

        /// <summary>
        /// Adds a new location after the current location. 
        /// Removes all the later locations.
        /// </summary>
        /// <param name="location"></param>
        public void AddNewLocation(WorkbookLocation location)
        {
        }

        /// <summary>
        /// Moves to the next location in the list if there is one.
        /// </summary>
        public void MoveToNextLocation()
        { 
        }

        /// <summary>
        /// Moves to the previous loaction if there is one.
        /// </summary>
        public void MoveToPreviousLocation()
        {
        }
    }
}
