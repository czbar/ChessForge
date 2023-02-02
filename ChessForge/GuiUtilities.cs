using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ChessPosition;
using GameTree;

namespace ChessForge
{
    /// <summary>
    /// Utilities for GUI controls
    /// </summary>
    public class GuiUtilities
    {
        /// <summary>
        /// Builds a line of text for display in the processing errors list.
        /// </summary>
        /// <param name="gm"></param>
        /// <param name="gameNo"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static string BuildGameProcessingErrorText(GameData gm, int gameNo, string message)
        {
            StringBuilder sbErrors = new StringBuilder();

            if (gm != null)
            {
                sbErrors.Append(Properties.Resources.Game + " #" + gameNo.ToString() + " : " + gm.Header.BuildGameHeaderLine(false));
                sbErrors.Append(Environment.NewLine);
                sbErrors.Append("     " + message);
                sbErrors.Append(Environment.NewLine);
            }

            return sbErrors.ToString();
        }

        /// <summary>
        /// Checks validity of a position.
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="errorText"></param>
        /// <returns></returns>
        public static bool ValidatePosition(ref BoardPosition pos, out string errorText)
        {
            StringBuilder sb = new StringBuilder();

            bool result = true;

            PositionUtils.KingCount(out int whiteKings, out int blackKings, pos);
            if (whiteKings != 1 || blackKings != 1)
            {
                result = false;
                if (whiteKings > 1)
                {
                    sb.AppendLine(Properties.Resources.PosValTooManyWhiteKings);
                }
                else if (whiteKings == 0)
                {
                    sb.AppendLine(Properties.Resources.PosValWhiteKingMissing);
                }

                if (blackKings > 1)
                {
                    sb.AppendLine(Properties.Resources.PosValTooManyBlackKings);
                }
                else if (blackKings == 0)
                {
                    sb.AppendLine(Properties.Resources.PosValBlackKingMissing);
                }
            }

            // only check if we know we have 1 king each side (otherwise we may get an exception)
            if (result == true)
            {
                if (pos.ColorToMove == PieceColor.White && PositionUtils.IsKingInCheck(pos, PieceColor.Black))
                {
                    result = false;
                    sb.AppendLine(Properties.Resources.PosValBlackKingInCheck);
                }
                if (pos.ColorToMove == PieceColor.Black && PositionUtils.IsKingInCheck(pos, PieceColor.White))
                {
                    result = false;
                    sb.AppendLine(Properties.Resources.PosValWhiteKingInCheck);
                }
            }

            // remove any incorrect castling rights if we are good so far
            if (result)
            {
                PositionUtils.CorrectCastlingRights(ref pos);
            }

            errorText = sb.ToString();
            return result;
        }

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
