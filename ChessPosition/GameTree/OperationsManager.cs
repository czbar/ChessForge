using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameTree
{
    public class OperationsManager
    {
        // queue of operations
        protected Queue<Operation> _operations = new Queue<Operation>();

        /// <summary>
        /// Queued a new operation.
        /// </summary>
        /// <param name="op"></param>
        public void QueueOperation(Operation op)
        {
            _operations.Enqueue(op);
        }

        /// <summary>
        /// Removes and returns the first operation in the queue.
        /// </summary>
        /// <returns></returns>
        public Operation DequeueOperation()
        {
            return _operations.Dequeue();
        }

        /// <summary>
        /// Returns the timestamp of the first Operation in the queue 
        /// </summary>
        public long Timestamp
        {
            get
            {
                if (_operations.Count > 0)
                {
                    return _operations.Peek().Timestamp;
                }
                else
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// Clears the Operations queue
        /// </summary>
        public void Reset()
        {
            _operations.Clear();
        }

    }
}
