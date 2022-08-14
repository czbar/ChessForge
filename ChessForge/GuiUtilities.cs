using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ChessPosition;

namespace ChessForge
{
    /// <summary>
    /// Utilities for the Main Window
    /// </summary>
    public class GuiUtilities
    {
        /// <summary>
        /// Finds the the row and column for the cell
        /// clicked in DataGrid.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="row"></param>
        /// <param name="column"></param>
        public static void GetDataGridColumnRowFromMouseClick(DataGrid dgControl, MouseButtonEventArgs e, out int row, out int column)
        {
            row = -1;
            column = -1;

            DependencyObject dep = (DependencyObject)e.OriginalSource;
            // iteratively traverse the visual tree
            while ((dep != null) && !(dep is DataGridCell))
            {
                dep = VisualTreeHelper.GetParent(dep);
            }

            if (dep == null)
                return;


            if (dep is DataGridCell)
            {
                DataGridCell cell = dep as DataGridCell;
                DataGridCellInfo info = new DataGridCellInfo(cell);
                column = cell.Column.DisplayIndex;

                DataGridRow dr = (DataGridRow)(dgControl.ItemContainerGenerator.ContainerFromItem(info.Item));
                if (dr != null)
                {
                    row = dr.GetIndex();
                }
            }
        }

        /// <summary>
        /// Extracts the integer value from a string that has it
        /// as a suffix.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="lastChar"></param>
        /// <returns></returns>
        public static int GetNodeIdFromPrefixedString(string s, char lastChar = '_')
        {
            int nodeId = -1;

            int lastCharPos = s.LastIndexOf('_');
            if (lastCharPos >= 0 && lastCharPos < s.Length - 1)
            {
                if (!int.TryParse(s.Substring(lastCharPos + 1), out nodeId))
                {
                    nodeId = -1;
                }
            }

            return nodeId;
        }

        /// <summary>
        /// Builds evaluation text ready to be included in a GUI element.
        /// It will produce a double value with 2 decimal digits or an
        /// indication of mate in a specified number of moves.
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public static string BuildEvaluationText(MoveEvaluation line, PieceColor colorToMove)
        {
            string eval;

            if (!line.IsMateDetected)
            {
                int intEval = colorToMove == PieceColor.White ? line.ScoreCp : -1 * line.ScoreCp;
                eval = (((double)intEval) / 100.0).ToString("F2");
            }
            else
            {
                if (line.MovesToMate == 0)
                {
                    eval = "#";
                }
                else
                {
                    int movesToMate = colorToMove == PieceColor.White ? line.MovesToMate : -1 * line.MovesToMate;
                    string sign = Math.Sign(movesToMate) > 0 ? "+" : "-";
                    eval = sign + "#" + (Math.Abs(line.MovesToMate)).ToString();
                }
            }

            return eval;
        }

    }
}
