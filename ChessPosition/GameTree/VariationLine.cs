using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChessPosition;

namespace GameTree
{
    /// <summary>
    /// Holds a single line of the variation tree.
    /// </summary>
    public class VariationLine
    {
        /// <summary>
        /// Constructs the object and creates a list for Plies (half-moves)
        /// </summary>
        public VariationLine()
        {
            Plies = new List<PlyForTreeView>();
        }

        public List<PlyForTreeView> Plies { get; set; }

        /// <summary>
        /// Builds a list of nodes for a single variation, given
        /// the last node of the variation.
        /// Walks back from the passed node to the root and builds
        /// 2 lists of plies, one for White and one for Black.
        /// This facilitates easy data binding with the containing
        /// GridView where White's and Black's moves are displayed
        /// in separate rows (White above Black)
        /// 
        /// </summary>
        /// <param name="nd"></param>
        /// <returns></returns>
        public static List<VariationLine> BuildVariationLine(TreeNode nd)
        {
            VariationLine vlWhite = new VariationLine();
            VariationLine vlBlack = new VariationLine();

            bool dummyMoves = false;
            while (nd.Parent != null)
            {
                PlyForTreeView pl = new PlyForTreeView();

#if false // optional way to show repeating moves  
                if (dummyMoves)
                {
                    pl.AlgMove = ". . .";
                }
                else
                {
                    pl.AlgMove = nd.LastMoveAlgebraicNotation;
                }
#endif

                pl.AlgMove = nd.LastMoveAlgebraicNotation;

                if (dummyMoves)
                {
                    pl.GrayedOut = true;
                }

                pl.LineId = nd.LineId;
                pl.MoveNumber = nd.Position.MoveNumber;
                pl.NodeId = nd.NodeId;

                if (nd.ColorToMove() == PieceColor.Black)
                {
                    vlWhite.Plies.Insert(0, pl);
                }
                else
                {
                    vlBlack.Plies.Insert(0, pl);
                }

                // if this is a fork and we are not the first child set dummy moves mode
                if (nd.Parent != null && nd.Parent.Children.Count > 1 && nd.Parent.Children[0].NodeId != nd.NodeId)
                {
                    dummyMoves = true;
                }
                nd = nd.Parent;
            }

            List<VariationLine> vl = new List<VariationLine>();
            vl.Add(vlWhite);
            vl.Add(vlBlack);

            return vl;
        }
    }
}
