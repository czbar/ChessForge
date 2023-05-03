using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
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
        /// Checks if the KeyEvent in the TextBox indicates the insertion of a figurine symbol
        /// and if so, performs it.
        /// </summary>
        /// <param name="textBox"></param>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        public static bool InsertFigurine(TextBox textBox, object sender, System.Windows.Input.KeyEventArgs e)
        {
            bool res = false;

            if ((Keyboard.Modifiers & ModifierKeys.Alt) > 0)
            {
                var key = (e.Key == Key.System ? e.SystemKey : e.Key);

                char charToInsert = GetFigurineChar(key);

                if (charToInsert != '\0')
                {
                    res = true;

                    if (!string.IsNullOrEmpty(textBox.SelectedText))
                    {
                        textBox.SelectedText = "";
                    }

                    int caretIndex = textBox.CaretIndex;
                    string newText = textBox.Text.Insert(textBox.CaretIndex, charToInsert.ToString());
                    textBox.Text = newText;
                    textBox.CaretIndex = caretIndex + 1;
                }
            }

            return res;
        }

        /// <summary>
        /// Checks if the KeyEvent in the RichTextBox indicates the insertion of a figurine symbol
        /// and if so, performs it.
        /// </summary>
        /// <param name="rtb"></param>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        public static bool InsertFigurine(RichTextBox rtb, object sender, System.Windows.Input.KeyEventArgs e)
        {
            bool res = false;

            if ((Keyboard.Modifiers & ModifierKeys.Alt) > 0)
            {
                var key = (e.Key == Key.System ? e.SystemKey : e.Key);

                char charToInsert = GetFigurineChar(key);

                if (charToInsert != '\0')
                {
                    res = true;

                    // Get the current selection
                    TextSelection selection = rtb.Selection;

                    // If there is a non-empty selection, replace it with the character
                    if (!selection.IsEmpty)
                    {
                        selection.Text = charToInsert.ToString();
                    }
                    else // Otherwise, insert the character at the current caret position
                    {
                        rtb.CaretPosition.InsertTextInRun(charToInsert.ToString());
                    }

                }
            }

            return res;
        }

        /// <summary>
        /// Builds text for display representing the passed millisecond value
        /// in human friendly way.
        /// The text will consist of at most 2 parts e.g. days and hours, or minutes and seconds.
        /// </summary>
        /// <param name="milliseconds"></param>
        /// <returns></returns>
        public static string TimeStringInTwoParts(long estTime)
        {
            TimeSpan ts = TimeSpan.FromMilliseconds(estTime);
            bool hasDays = false;
            bool hasHours = false;
            bool hasMinutes = false;

            StringBuilder sb = new StringBuilder();

            bool done = false;
            if (ts.Days > 0)
            {
                sb.Append(ts.Days.ToString() + " " + Properties.Resources.Days);
                hasDays = true;
                if (ts.Days > 10)
                {
                    done = true;
                }
                if (!done)
                {
                    sb.Append(", ");
                }
            }

            if (!done && (hasDays || ts.Hours > 0))
            {
                sb.Append(ts.Hours.ToString() + " " + Properties.Resources.Hours);
                hasHours = true;
                if (ts.Hours > 10 || hasDays)
                {
                    done = true;
                }
                if (!done)
                {
                    sb.Append(", ");
                }
            }

            if (!done && (hasHours || ts.Minutes > 0))
            {
                sb.Append(ts.Minutes.ToString() + " " + Properties.Resources.Minutes);
                hasMinutes = true;
                if (ts.Minutes > 10 || hasHours)
                {
                    done = true;
                }
                if (!done)
                {
                    sb.Append(", ");
                }
            }

            if (!done && (hasMinutes || ts.Seconds >= 0))
            {
                sb.Append(ts.Seconds.ToString() + " " + Properties.Resources.Seconds);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Produces text for user interface from the received ParserException.
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public static string TranslateParseException(ParserException ex)
        {
            StringBuilder sb = new StringBuilder();
            switch (ex.ParseError)
            {
                case ParserException.ParseErrorType.PGN_GAME_EXPECTED_MOVE_NUMBER:
                    sb.Append(Properties.Resources.ErrFound 
                        + " "+ ex.CurrentToken 
                        + " " + Properties.Resources.ErrInsteadOfMoveNumber 
                        + ", " + Properties.Resources.ErrAfterMove + " " + ex.PreviousMove);
                    break;
                default:
                    return ex.Message;
            }

            return sb.ToString();
        }

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

        /// <summary>
        /// Returns the figurine symbol corresponding to the pressed keys.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private static char GetFigurineChar(Key key)
        {
            char charToInsert = '\0';

            bool isShift = (Keyboard.Modifiers & ModifierKeys.Shift) > 0;
            switch (key)
            {
                case Key.K:
                    charToInsert = isShift ? Languages.BlackFigurinesMapping['K'] : Languages.WhiteFigurinesMapping['K'];
                    break;
                case Key.Q:
                    charToInsert = isShift ? Languages.BlackFigurinesMapping['Q'] : Languages.WhiteFigurinesMapping['Q'];
                    break;
                case Key.R:
                    charToInsert = isShift ? Languages.BlackFigurinesMapping['R'] : Languages.WhiteFigurinesMapping['R'];
                    break;
                case Key.B:
                    charToInsert = isShift ? Languages.BlackFigurinesMapping['B'] : Languages.WhiteFigurinesMapping['B'];
                    break;
                case Key.N:
                    charToInsert = isShift ? Languages.BlackFigurinesMapping['N'] : Languages.WhiteFigurinesMapping['N'];
                    break;
                default:
                    break;
            }

            return charToInsert;
        }

    }
}
