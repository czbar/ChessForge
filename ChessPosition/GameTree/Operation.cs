using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static GameTree.EditOperation;

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
        public long Timestamp { get { return _timestamp; } }

        /// <summary>
        /// Returns Operation's data.
        /// </summary>
        public object OpData_1
        {
            get { return _opData_1; }
            set { _opData_1 = value; }
        }

        /// <summary>
        /// Returns Operation's data.
        /// </summary>
        public object OpData_2
        {
            get { return _opData_2; }
            set { _opData_2 = value; }
        }

        /// <summary>
        /// Returns Operation's data.
        /// </summary>
        public object OpData_3
        {
            get { return _opData_3; }
            set { _opData_3 = value; }
        }

        /// <summary>
        /// Operation's data
        /// </summary>
        protected object _opData_1;

        /// <summary>
        /// Operation's data
        /// </summary>
        protected object _opData_2;

        /// <summary>
        /// Operation's data
        /// </summary>
        protected object _opData_3;

        /// <summary>
        /// Sets the timestamp
        /// </summary>
        private void SetTimestamp()
        {
            _timestamp = DateTime.Now.Ticks;
        }
    }
}
