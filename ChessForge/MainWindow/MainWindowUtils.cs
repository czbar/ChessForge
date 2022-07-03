using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Threading;
using ChessForge;
using ChessPosition;
using GameTree;
using System.Windows.Media.Animation;
using Microsoft.Win32;
using System.IO;

namespace ChessForge
{
    /// <summary>
    /// Utilities for the Main Window
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Finds the the row and column for the cell
        /// clicked in DataGrid.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="row"></param>
        /// <param name="column"></param>
        private void GetColumnRowFromMouseClick(MouseButtonEventArgs e, out int row, out int column)
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

                DataGridRow dr = (DataGridRow)(_dgActiveLine.ItemContainerGenerator.ContainerFromItem(info.Item));
                if (dr != null)
                {
                    row = dr.GetIndex();
                }
            }
        }

    }
}
