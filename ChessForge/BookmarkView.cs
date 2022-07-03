using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameTree;
using ChessPosition;

namespace ChessForge
{
    /// <summary>
    /// Combines the Bookmark's content
    /// and its GUI visualization
    /// </summary>
    public class BookmarkView
    {
        public BookmarkView(ChessBoard board)
        {
            GuiBoard = board;
        }

        public ChessBoard GuiBoard;
        public Bookmark BookmarkData;

        public void SetOpacity(double opacity)
        {
            GuiBoard.SetBoardOpacity(opacity);
        }
        public void Activate()
        {
            GuiBoard.DisplayPosition(BookmarkData.Node.Position);
            GuiBoard.SetLabelText(BookmarkData.Node.GetPlyText(true));
            SetOpacity(1);
        }
    }
}
