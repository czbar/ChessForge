﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.RightsManagement;
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
        /// Adjusts the configured font size per configuration parameters.
        /// </summary>
        /// <param name="origSize"></param>
        /// <returns></returns>
        public static int AdjustFontSize(int origSize)
        {
            if (Configuration.UseFixedFont)
            {
                return Constants.BASE_FIXED_FONT_SIZE + Configuration.FontSizeDiff;
            }
            else
            {
                return origSize + Configuration.FontSizeDiff;
            }
        }

        /// <summary>
        /// Positions a dialog in relation to the main window. 
        /// </summary>
        /// <param name="dlg"></param>
        /// <param name="owner"></param>
        /// <param name="offset"></param>
        public static void PositionDialog(Window dlg, Window owner, double offset = 100)
        {
            Point leftTop = owner.PointToScreen(new Point(0, 0));

            dlg.Left = leftTop.X + offset;
            dlg.Top = leftTop.Y + offset;
            dlg.Topmost = false;
            dlg.Owner = owner;
        }

        /// <summary>
        /// Identifies a List View item from the click coordinates. 
        /// </summary>
        /// <param name="listView"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static ListViewItem GetListViewItemFromPoint(ListView listView, Point point)
        {
            HitTestResult result = VisualTreeHelper.HitTest(listView, point);
            if (result == null)
            {
                return null;
            }

            DependencyObject hitObject = result.VisualHit;
            while (hitObject != null && !(hitObject is ListViewItem))
            {
                hitObject = VisualTreeHelper.GetParent(hitObject);
            }

            return hitObject as ListViewItem;
        }

        /// <summary>
        /// Opens a game preview dialog.
        /// </summary>
        /// <param name="article"></param>
        public static void InvokeGamePreviewDialog(Article article, Window owner)
        {
            if (article != null)
            {
                List<string> gameIdList = new List<string>();
                List<Article> games = new List<Article> { article };
                gameIdList.Add(article.Tree.Header.GetGuid(out _));

                SingleGamePreviewDialog dlg = new SingleGamePreviewDialog(gameIdList, games);
                PositionDialog(dlg, owner, 20);
                dlg.ShowDialog();
            }
        }

        /// <summary>
        /// Converts centipawns to accuracy percentage.
        /// </summary>
        /// <param name="centipawns"></param>
        /// <returns></returns>
        public static uint ConvertCentipawnsToAccuracy(uint centipawns)
        {
            centipawns = Math.Min(centipawns, 400);
            return (400 - centipawns) / 4;
        }

        /// <summary>
        /// Converts accuracy percentage to centipawns.
        /// </summary>
        /// <param name="accuracy"></param>
        /// <returns></returns>
        public static uint ConvertAccuracyToCentipawns(uint accuracy)
        {
            accuracy = Math.Min(100, accuracy);
            return (100 - accuracy) * 4;
        }


        /// <summary>
        /// Rebuilds the ChaptersView if it has focus or marks for refresh.
        /// Brings the passed chapter into view of not null.
        /// </summary>
        public static void RefreshChaptersView(Chapter chapterToView)
        {
            if (AppState.ActiveTab == TabViewType.CHAPTERS || AppState.MainWin.UiTabChapters.IsFocused)
            {
                AppState.MainWin.ChaptersView.BuildFlowDocumentForChaptersView();
                if (chapterToView != null)
                {
                    PulseManager.ChaperIndexToBringIntoView = chapterToView.Index;
                }
            }
        }

        /// <summary>
        /// Rebuilds the ChaptersView if it has focus or marks for refresh.
        /// Brings the passed article into view if set.
        /// </summary>
        /// <param name="chapterToView"></param>
        /// <param name="articleToView"></param>
        /// <param name="articleToViewIndex"></param>
        public static void RefreshChaptersView(Chapter chapterToView, Article articleToView, int articleToViewIndex)
        {
            if (AppState.ActiveTab == TabViewType.CHAPTERS || AppState.MainWin.UiTabChapters.IsFocused)
            {
                AppState.MainWin.ChaptersView.BuildFlowDocumentForChaptersView();
                if (chapterToView != null && articleToView != null)
                {
                    PulseManager.SetArticleToBringIntoView(chapterToView.Index, articleToView.ContentType, articleToViewIndex);
                }
            }
        }

        /// <summary>
        /// Call this method when we need to ensure that both, the tab has focus and
        /// the focus-received handler is run.
        /// If the tab does not have a focus, it is enough to call the Focus() method,
        /// otherwise the handler will not be invoked so we have to call it explicitly.
        /// </summary>
        /// <param name="vt"></param>
        public static void ForceFocus(TabViewType vt, TabViewType vtDefault = TabViewType.NONE)
        {
            switch (vt)
            {
                case TabViewType.CHAPTERS:
                    if (AppState.MainWin.UiTabChapters.IsFocused)
                    {
                        AppState.MainWin.UiTabChapters_GotFocus(null, null);
                    }
                    else
                    {
                        AppState.MainWin.UiTabChapters.Focus();
                    }
                    break;
                case TabViewType.STUDY:
                    if (AppState.MainWin.UiTabStudyTree.IsFocused)
                    {
                        AppState.MainWin.UiTabStudyTree_GotFocus(null, null);
                    }
                    else
                    {
                        AppState.MainWin.UiTabStudyTree.Focus();
                    }
                    break;
                case TabViewType.INTRO:
                    if (AppState.MainWin.UiTabIntro.IsFocused)
                    {
                        AppState.MainWin.UiTabIntro_GotFocus(null, null);
                    }
                    else
                    {
                        AppState.MainWin.UiTabIntro.Focus();
                    }
                    break;
                case TabViewType.MODEL_GAME:
                    if (AppState.MainWin.UiTabModelGames.IsFocused)
                    {
                        AppState.MainWin.UiTabModelGames_GotFocus(null, null);
                    }
                    else
                    {
                        AppState.MainWin.UiTabModelGames.Focus();
                    }
                    break;
                case TabViewType.EXERCISE:
                    if (AppState.MainWin.UiTabExercises.IsFocused)
                    {
                        AppState.MainWin.UiTabExercises_GotFocus(null, null);
                    }
                    else
                    {
                        AppState.MainWin.UiTabExercises.Focus();
                    }
                    break;
                default:
                    if (AppState.MainWin.UiTabChapters.IsFocused)
                    {
                        AppState.MainWin.UiTabChapters_GotFocus(null, null);
                    }
                    else
                    {
                        AppState.MainWin.UiTabChapters.Focus();
                    }
                    break;
            }
        }

        /// <summary>
        /// Get the length in pixel that the passed text would take in the passed label.
        /// </summary>
        /// <param name="label"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public static Size GetTextLength(Label label, string text)
        {
            var formattedText = new FormattedText(
                text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(label.FontFamily, label.FontStyle, label.FontWeight, label.FontStretch),
                label.FontSize,
                Brushes.Black,
                VisualTreeHelper.GetDpi(label).PixelsPerDip);

            return new Size(formattedText.Width, formattedText.Height);
        }

        /// <summary>
        /// Shortens the text if necessary to fit into a label.
        /// Appends 3 dots at the end if the shortening occurred.
        /// </summary>
        /// <param name="label"></param>
        /// <param name="text"></param>
        /// <param name="maxChars"></param>
        /// <returns></returns>
        public static string AdjustTextToFit(Label label, string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            string adjusted = text;
            double maxWidth = label.MaxWidth - (label.BorderThickness.Left + label.BorderThickness.Right);

            double width = GetTextLength(label, text).Width;
            if (width > maxWidth)
            {
                double elipsisWidth = GetTextLength(label, "...").Width;
                for (int i = text.Length - 1; i > 0; i--)
                {
                    string sub = text.Substring(0, i);
                    if (GetTextLength(label, sub).Width < (maxWidth - (elipsisWidth + 10)))
                    {
                        adjusted = sub + "...";
                        break;
                    }
                }
            }

            return adjusted;
        }

        /// <summary>
        /// Displays info advising the user to exit Training Mode.
        /// </summary>
        public static void ShowExitTrainingInfoMessage()
        {
            MessageBox.Show(Properties.Resources.ExitTrainingAdvice, Properties.Resources.Training, MessageBoxButton.OK, MessageBoxImage.Information);
        }

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

            try
            {
                if ((Keyboard.Modifiers & ModifierKeys.Alt) > 0)
                {
                    var key = (e.Key == Key.System ? e.SystemKey : e.Key);

                    char charToInsert = GetFigurineChar(key);

                    if (charToInsert != '\0')
                    {
                        string stringToInsert = charToInsert.ToString();
                        res = true;

                        // Get the current selection
                        TextSelection selection = rtb.Selection;

                        // get the data to then place the caret after the insertion
                        TextPointer startPtr = rtb.Document.ContentStart;
                        int start = startPtr.GetOffsetToPosition(rtb.CaretPosition);

                        // If there is a non-empty selection, replace it with the character
                        if (!selection.IsEmpty)
                        {
                            selection.Text = stringToInsert;
                        }
                        else // Otherwise, insert the character at the current caret position
                        {
                            rtb.CaretPosition.InsertTextInRun(stringToInsert);
                        }

                        // place the caret after insertion
                        rtb.CaretPosition = startPtr.GetPositionAtOffset((start) + stringToInsert.Length);
                    }
                }
            }
            catch
            {
                res = true;
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
                        + " " + ex.CurrentToken
                        + " " + Properties.Resources.ErrInsteadOfMoveNumber
                        + ", " + Properties.Resources.ErrAfterMove + " " + ex.PreviousMove);
                    break;
                case ParserException.ParseErrorType.PGN_INVALID_MOVE:
                    sb.Append(Properties.Resources.PgnParsingError
                        + ": " + Properties.Resources.InvalidMove + " "
                        + ex.CurrentToken);
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
