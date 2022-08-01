using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Collections.ObjectModel;
using System.Xml;
using System.IO;
using System.Text;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using ChessForge;
using ChessPosition;
using GameTree;

namespace ChessForge
{
    /// <summary>
    /// Functions handling the Game Text view (RichTextBox)
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<TreeTableRow> rows = new List<TreeTableRow>();

        /// <summary>
        /// Sets up data for the Table view and configures templates
        /// for the List View control.
        /// </summary>
        private void SetupDataInTreeView()
        {
            //TODO: currently WorkTable view is disabled!
            return;

#if false
            // get the stem of the tree to show on the TextBox control at the top. 
            List<TreeNode> stem = Workbook.BuildStem();

            // due to the way we want the table to look, remove the last move from the stem
            // if it was made by White.
            if (stem.Count > 0 && stem[stem.Count - 1].Position.ColorToMove == PieceColor.Black)
            {
                stem.RemoveAt(stem.Count - 1);
            }
            string stemText = BuildStemText(stem);
            _tbWorkbookTable.Text = stemText;

            // Stem includes the starting position (no move) and ends with a Black move
            // so this will give us the number of moves.
            WorkbookTableState.StemLength = (stem.Count - 1) / 2;

            ObservableCollection<VariationLine> vls = Workbook.VariationLines;

            // find the longest line
            WorkbookTableState.MaxMoves = 0;
            foreach (VariationLine vl in vls)
            {
                if (vl.Plies.Count > WorkbookTableState.MaxMoves)
                {
                    WorkbookTableState.MaxMoves = vl.Plies.Count;
                }

            }

            rows.Clear();

            int iRowColor = 0;

            // due to performance and the reasonableness of
            // having limited number of columns to view,
            // let's limit how many columns we are showing
            WorkbookTableState.MaxMoves = Math.Min(WorkbookTableState.MaxMoves, WorkbookTableState.StemLength + 15);

            for (int i = 0; i < vls.Count; i++)
            {
                VariationLine vl = vls[i];
                TreeTableRow row = new TreeTableRow();
                row.VariationLineNumber = i / 2;

                row.RowColor = iRowColor == 0 ? Strings.ROW_COLOR_WHITE : Strings.ROW_COLOR_BLACK;

                for (int j = 0; j < WorkbookTableState.MaxMoves; j++)
                {
                    TreeTableCell cell = new TreeTableCell();
                    row.Cells.Add(cell);

                    if (j < vl.Plies.Count)
                    {
                        //row.Moves.Add(vl.Plies[j].AlgMove);
                        //row.PlieAttrs.Add(vl.Plies[j].GrayedOut ? 1 : 0);
                        //row.NodeIds.Add(vl.Plies[j].NodeId);

                        cell.Ply = vl.Plies[j].AlgMove;
                        cell.PlyAttrs = vl.Plies[j].GrayedOut ? 1 : 0;
                        cell.NodeId = vl.Plies[j].NodeId;
        }
                    else
                    {
                        //row.Moves.Add("");
                        //row.PlieAttrs.Add(-1);
                        //row.NodeIds.Add(-1);

                        cell.Ply = "";
                        cell.PlyAttrs = -1;
                        cell.NodeId = -1;
                    }
                    //row.ToolTips.Add(null);
                    cell.ToolTip = null;

                    if (j > 0
                        && j == WorkbookTableState.MaxMoves - 1
                        && row.RowColor == Strings.ROW_COLOR_BLACK
                        && vl.Plies.Count >= j
                        && !string.IsNullOrWhiteSpace(cell.Ply))
//                        && !string.IsNullOrWhiteSpace(row.Moves[j]))
                    {
                //row.Moves[j] += " *";
                cell.Ply += " *";

                        string toolTip = BuildLineRemainder(i, j + 1);
                        //row.ToolTips[j] = toolTip;
                        cell.ToolTip = toolTip;

                        // the previous row (for White) also gets the same tool tip
                        //rows[rows.Count - 1].ToolTips[j] = toolTip;
                        TreeTableCell prevCell = rows[rows.Count - 1].Cells[j];
                        prevCell.ToolTip = toolTip;
                    }
                    else
                    {
                        if (row.RowColor == Strings.ROW_COLOR_BLACK)
                        {
                            VariationLine prevLine = vls[i - 1];
                            if (prevLine.Plies.Count <= j)
                            {
                                //row.ToolTips[j] = "Line ended";
                                cell.ToolTip = "Line ended";
                                //rows[rows.Count - 1].ToolTips[j] = "Line ended";
                                TreeTableCell prevCell = rows[rows.Count - 1].Cells[j];
                                prevCell.ToolTip = "Line ended";
                            }
                            else
                            {
                                StringBuilder sbTT = new StringBuilder();
                                sbTT.Append(prevLine.Plies[j].MoveNumber.ToString() + ".");
                                sbTT.Append(prevLine.Plies[j].AlgMove);
                                if (vl.Plies.Count > j)
                                {
                                    sbTT.Append(" " + vl.Plies[j].AlgMove);
                                }
                                //row.ToolTips[j] = sbTT.ToString();
                                cell.ToolTip = sbTT.ToString();
                                //rows[rows.Count - 1].ToolTips[j] = sbTT.ToString();
                                TreeTableCell prevCell = rows[rows.Count - 1].Cells[j];
                                prevCell.ToolTip = sbTT.ToString();
                            }
                        }
                    }

                }

                rows.Add(row);

                iRowColor = iRowColor == 0 ? 1 : 0;
            }

            WorkbookTableState.Reset();
            _lvWorkbookTable.ItemsSource = null;
            _gvWorkbookTable.Columns.Clear();

            for (int j = WorkbookTableState.StemLength; j < WorkbookTableState.MaxMoves; j++)
            {
                GridViewColumn gv = new GridViewColumn();
                gv.Width = 50;
                gv.Header = (j+1).ToString() + "   ";

                DataTemplate template = (DataTemplate)(_lvWorkbookTable.FindResource("WorkbookTable"));
                string savedTemplate = XamlWriter.Save(template);

                //string binding = String.Format("Moves[{0}]", j);
                string binding = String.Format("Cells[{0}].Ply", j);
                savedTemplate = savedTemplate.Replace("Text=\"\" TextAlignment", "Text=\"{Binding " + binding + "}\" TextAlignment");

                //string bindingGrayOut = String.Format("PlieAttrs[{0}]", j);
                string bindingGrayOut = String.Format("Cells[{0}].PlyAttrs", j);
                savedTemplate = savedTemplate.Replace("{Binding Path=GrayOut", "{Binding " + bindingGrayOut);

                //string bindingToolTip = String.Format("ToolTips[{0}]", j);
                string bindingToolTip = String.Format("Cells[{0}].ToolTip", j);
                savedTemplate = savedTemplate.Replace("TextBlock Text=\"\" /></UniformGrid", "TextBlock Text=\"{Binding " + bindingToolTip + "}\" /></UniformGrid");

                StringReader stringReader = new StringReader(savedTemplate);
                XmlReader xmlReader = XmlReader.Create(stringReader);
                DataTemplate dt = (DataTemplate)XamlReader.Load(xmlReader);

                gv.CellTemplate = dt;
                _gvWorkbookTable.Columns.Add(gv);
            }

            _lvWorkbookTable.ItemsSource = rows;
#endif
        }

        private string BuildLineRemainder(int vlIdx, int moveIdx)
        {
            StringBuilder sb = new StringBuilder();
            VariationLine vlWhite = Workbook.VariationLines[vlIdx - 1];
            VariationLine vlBlack = Workbook.VariationLines[vlIdx];

            // we start with White and have to watch for ending on a null Black move
            for (int i = moveIdx - 1; i < vlWhite.Plies.Count; i++)
            {
                PlyForTreeView ptv = vlWhite.Plies[i];
                sb.Append(ptv.MoveNumber.ToString() + "." + ptv.AlgMove + " ");
                if (i < vlBlack.Plies.Count)
                {
                    ptv = vlBlack.Plies[i];
                    sb.Append(ptv.AlgMove + " ");
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Build text for the stem line to display.
        /// </summary>
        /// <param name="stem"></param>
        /// <returns></returns>
        private string BuildStemText(List<TreeNode> stem)
        {
            return TextUtils.BuildTextForLine(stem);
        }

        private void _lvWorkbookTable_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selItems = ((ListView)sender).SelectedItems;

            // check what selection we have, if there are 2 selected lines for the same move we are good
            if (selItems.Count == 2 &&
                (((TreeTableRow)selItems[0]).VariationLineNumber == ((TreeTableRow)selItems[1]).VariationLineNumber))
            {
                return;
            }

            if (selItems.Count > 1)
            {
                // remove previous selections
                // removing will keep calling this function until 1 is left
                selItems.RemoveAt(0);
            }

            // it could be that this event was caused by a click on an already selected item.
            // if so it will be in the Removed list and we need to restore its selection
            if (selItems.Count == 1)
            {
                var remItems = e.RemovedItems;
                if (remItems.Count > 0)
                {
                    if (((TreeTableRow)selItems[0]).VariationLineNumber == ((TreeTableRow)remItems[0]).VariationLineNumber)
                    {
                        _lvWorkbookTable.SelectedItems.Add((TreeTableRow)remItems[0]);
                        return;
                    }
                }
            }

            if (e.AddedItems.Count > 0)
            {
                TreeTableRow item = (TreeTableRow)e.AddedItems[0];
                int selIndex = ((ListView)sender).SelectedIndex;
                int secondSel = item.RowColor == Strings.ROW_COLOR_WHITE ? selIndex + 1 : selIndex - 1;
                _lvWorkbookTable.SelectedItems.Add(((ListView)sender).Items[secondSel]);
            }
        }

        /// <summary>
        /// Identifies a column in the Workbook Tree ListView
        /// from the coordinates of the clicked point.
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        private int _lvWorkbookTable_GetColumnFromPoint(Point p)
        {
            double offset = 0;
            // iterate over the columns until the aggregated column width exceed clicked Y coordinate
            for (int i = 0; i < _gvWorkbookTable.Columns.Count; i++)
            {
                offset += _gvWorkbookTable.Columns[i].ActualWidth;
                if (offset > p.X)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Identifies the clicked row and selects the
        /// Active Line accordingly.
        /// </summary>
        /// <param name="column"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private bool _lvWorkbookTable_SetActiveLineFromRow(int column, MouseButtonEventArgs e)
        {
            DependencyObject dep = (DependencyObject)e.OriginalSource;
            // Traverse the visual tree to get the row data.
            while ((dep != null) && !(dep is GridViewRowPresenter))
            {
                dep = VisualTreeHelper.GetParent(dep);
            }

            if (dep is GridViewRowPresenter)
            {
                if (((GridViewRowPresenter)dep).Content is TreeTableRow)
                {
                    TreeTableRow ttr = ((GridViewRowPresenter)dep).Content as TreeTableRow;

                    // get nodeId
                    int variationLineIndex = ttr.VariationLineNumber * 2 + (ttr.RowColor == Strings.ROW_COLOR_WHITE ? 0 : 1);
                    int moveInVariation = (Workbook.Stem.Count - 1) / 2 + column;

                    if (variationLineIndex > Workbook.VariationLines.Count - 1)
                    {
                        return false;
                    }

                    VariationLine vl = Workbook.VariationLines[variationLineIndex];
                    if (moveInVariation > vl.Plies.Count - 1)
                    {
                        return false;
                    }

                    PlyForTreeView ptv = vl.Plies[moveInVariation];
                    // the line is uniquely identified by the last move in it
                    var line = Workbook.SelectLine(vl.Plies[vl.Plies.Count - 1].LineId);
                    SetActiveLine(line, ptv.NodeId);
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Figure out which move in which line was clicked.
        /// Highlight the move and set the ActiveLine 
        /// accordingly.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _lvWorkbookTable_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // cliked point
            Point p = e.GetPosition(_lvWorkbookTable);
            int column = _lvWorkbookTable_GetColumnFromPoint(p);
            if (column < 0)
            {
                e.Handled = true;
                return;
            }

            if (!_lvWorkbookTable_SetActiveLineFromRow(column, e))
            {
                e.Handled = true;
                return;
            }

            // un-highlight the previously selected move
            if (WorkbookTableState.SelectedTextBlock != null)
            {
                WorkbookTableState.SelectedTextBlock.Foreground = CHF_Colors.WORKBOOK_TABLE_REGULAR_FORE;
            }

            // highlight the clicked cell and store it for later un-highlighting
            UIElement element = (UIElement)e.MouseDevice.DirectlyOver;
            if (element is TextBlock)
            {
                WorkbookTableState.SelectedTextBlock = (TextBlock)element;
//                WorkbookTableState.HighlightForeground = ((TextBlock)element).Foreground as SolidColorBrush;

                ((TextBlock)element).Foreground = CHF_Colors.WORKBOOK_TABLE_HILITE_FORE;

            }
        }

        private void _lvWorkbookTable_ReHighlightSelectedCell()
        {
            if (WorkbookTableState.SelectedTextBlock != null)
            {
                WorkbookTableState.SelectedTextBlock.Foreground = CHF_Colors.WORKBOOK_TABLE_HILITE_FORE;
            }
        }

        private void _lvWorkbookTable_HighlightCell(TextBlock tb)
        {
            // reset the previously selected cell
            if (WorkbookTableState.SelectedTextBlock != null)
            {
                WorkbookTableState.SelectedTextBlock.Foreground = CHF_Colors.WORKBOOK_TABLE_REGULAR_FORE;
                WorkbookTableState.SelectedTextBlock = null;
            }

            if (tb == null)
            {
                return;
            }

            // highlight the clicked cell and store it for later un-highlighting
            {
                WorkbookTableState.SelectedTextBlock = tb;
//                WorkbookTableState.HighlightForeground = tb.Foreground as SolidColorBrush;

                tb.Foreground = CHF_Colors.WORKBOOK_TABLE_HILITE_FORE;
            }
        }

        /// <summary>
        /// Highlights cell identified by its row and column.
        /// Un-highlights the one currently highlighted.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="column"></param>
        private void _lvWorkbookTable_HighlightCell(int row, int column)
        {
            TextBlock tbCell = ListViewHelper.GetElementFromCellTemplate(_lvWorkbookTable, row, column, "");
            if (tbCell != null)
            {
                tbCell.Foreground = CHF_Colors.WORKBOOK_TABLE_HILITE_FORE;
            }
        }

        /// <summary>
        /// Get TextBlock element for a cell.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="column"></param>
        /// <returns></returns>
        private TextBlock _lvWorkbookTable_GetCell(int row, int column)
        {
            if (row < 0 || column < 0)
                return null;

            return ListViewHelper.GetElementFromCellTemplate(_lvWorkbookTable, row, column, "");
        }

        /// <summary>
        /// Selects the move and the line in this view on a request from another view (as opposed
        /// to a user request).
        /// Therefore it does not request other views to follow the selection.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="lineId"></param>
        public void _lvWorkbookTable_SelectLineAndMove(string lineId, int nodeId)
        {
            //TODO: currently WorkTable view is disabled!
            return;

#if false
            int rowIndex = -1;
            // The passed Line Id should be the one found in the last (leaf) node of the line.
            // Otherwise we will not find it.

            if (!string.IsNullOrEmpty(lineId))
            {
                for (int i = 0; i < Workbook.VariationLines.Count; i++)
                {
                    VariationLine vl = Workbook.VariationLines[i];
                    string currLineId = vl.Plies[vl.Plies.Count - 1].LineId;
                    if (currLineId == lineId)
                    {
                        rowIndex = i;
                        break;
                    }
                }
            }

            if (rowIndex < 0)
            {
                rowIndex = WorkbookTableState.SelectedRow;
            }

            if (rowIndex >= 0)
            {
                TreeTableRow ttr = rows[rowIndex];
                _lvWorkbookTable.SelectedItems.Add(ttr);
                // find nodeId
                int row = rowIndex;
                int column = GetColumnForNode(ttr, nodeId) - WorkbookTableState.StemLength;
                if (column < 0)
                {
                    rowIndex++;
                    column = GetColumnForNode(rows[rowIndex], nodeId) - WorkbookTableState.StemLength;
                }

                TextBlock tb = _lvWorkbookTable_GetCell(rowIndex, column);
                WorkbookTableState.SelectedTextBlock = tb;

                _lvWorkbookTable_HighlightCell(tb);

                // selected row must be an even number (there are really 
                // 2 selected rows, for White and Black)
                WorkbookTableState.SelectedRow = row;// - (row % 2);
                WorkbookTableState.SelectedColumn = column;
            }
#endif
        }

        private int GetColumnForNode(TreeTableRow ttr, int nodeId)
        {
            int column = -1;
            for (int i = 0; i < ttr.Cells.Count; i++)
            {
                if (ttr.Cells[i].NodeId == nodeId)
                {
                    column = i;
                    break;
                }
            }
            return column;
        }

        /// <summary>
        /// Due to the time it takes ListView to render and allow access to its data, we have to refresh the selection
        /// on getting Focus.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _lvWorkbookTable_GotFocus(object sender, RoutedEventArgs e)
        {
            // TODO Add selecting the line

            if (WorkbookTableState.SelectedTextBlock == null && WorkbookTableState.SelectedColumn >= 0)
            {
                TextBlock tb = _lvWorkbookTable_GetCell(WorkbookTableState.SelectedRow, WorkbookTableState.SelectedColumn); 
                _lvWorkbookTable_HighlightCell(tb);
            }
        }
    }
}

