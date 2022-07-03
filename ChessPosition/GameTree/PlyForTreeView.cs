using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameTree
{
    /// <summary>
    /// Represents a single ply
    /// in the form that then can be used 
    /// to bind to the Grid View (after being put
    /// in the TreeLine container). 
    /// </summary>
    public class PlyForTreeView
    {
        /// <summary>
        /// Algebraic notation of the ply.
        /// This is the only property that will 
        /// be bound in the DataGrid
        /// </summary>
        public string AlgMove { get; set; }

        /// <summary>
        /// Number of the move in the scoresheet sense i.e.
        /// the first move of White and Black gets number "1".
        /// </summary>
        public uint MoveNumber;

        /// <summary>
        /// Id of the line in which this object is displayed.
        /// </summary>
        public string LineId;

        /// <summary>
        /// NodeId represented by this object.
        /// Note that the same NodeId may appear in multiple Lines
        /// and this object is created for each such appearance.
        /// </summary>
        public int NodeId;

        public bool GrayedOut;
    }
}
