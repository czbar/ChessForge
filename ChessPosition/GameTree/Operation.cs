using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameTree
{
    public class Operation
    {
        // timestamp when the operation occured
        private long _timestamp;

        public Operation()
        {
            SetTimestamp();
        }

        /// <summary>
        /// This operation's creation time.
        /// </summary>
        public long Timestamp {get { return _timestamp; } } 

        /// <summary>
        /// Sets the timestamp
        /// </summary>
        private void SetTimestamp()
        {
            _timestamp = DateTime.Now.Ticks;
        }
    }
}
