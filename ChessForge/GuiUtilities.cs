using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ChessPosition;

namespace ChessForge
{
    /// <summary>
    /// Utilities for GUI controls
    /// </summary>
    public class GuiUtilities
    {
        /// <summary>
        /// Determines if any of the special keys is pressed
        /// </summary>
        /// <returns></returns>
        public static bool IsSpecialKeyPressed()
        {
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)
                || Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)
                || Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt)
                )
            {
                return true;

            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Calculate distance between 2 points.
        /// </summary>
        /// <param name="pStart"></param>
        /// <param name="pEnd"></param>
        /// <returns></returns>
        public static double CalculateDistance(Point pStart, Point pEnd)
        {
            return Point.Subtract(pEnd, pStart).Length;
        }

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

    }
}
