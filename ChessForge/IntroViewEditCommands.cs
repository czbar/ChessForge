using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;

namespace ChessForge
{
    public partial class IntroView : RichTextBuilder
    {
        /// <summary>
        /// RichTextBox application command.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Command_Undo(object sender, RoutedEventArgs e)
        {
            try
            {
                ApplicationCommands.Undo.Execute(null, _rtb);
            }
            catch (Exception ex)
            {
                AppLog.Message("Command_Undo()", ex);
            }
        }

        /// <summary>
        /// RichTextBox editing command.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Command_ToggleBold(object sender, RoutedEventArgs e)
        {
            try
            {
                EditingCommands.ToggleBold.Execute(null, _rtb);
            }
            catch (Exception ex)
            {
                AppLog.Message("Command_ToggleBold()", ex);
            }
        }

        /// <summary>
        /// RichTextBox editing command.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Command_ToggleItalic(object sender, RoutedEventArgs e)
        {
            try
            {
                EditingCommands.ToggleItalic.Execute(null, _rtb);
            }
            catch (Exception ex)
            {
                AppLog.Message("Command_ToggleItalic()", ex);
            }
        }

        /// <summary>
        /// RichTextBox editing command.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Command_ToggleUnderline(object sender, RoutedEventArgs e)
        {
            try
            {
                EditingCommands.ToggleUnderline.Execute(null, _rtb);
            }
            catch (Exception ex)
            {
                AppLog.Message("Command_ToggleUnderline()", ex);
            }
        }

        /// <summary>
        /// RichTextBox editing command.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Command_FontSizeUp(object sender, RoutedEventArgs e)
        {
            try
            {
                EditingCommands.IncreaseFontSize.Execute(null, _rtb);
            }
            catch (Exception ex)
            {
                AppLog.Message("Command_FontSizeUp()", ex);
            }
        }

        /// <summary>
        /// RichTextBox editing command.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Command_FontSizeDown(object sender, RoutedEventArgs e)
        {
            try
            {
                EditingCommands.DecreaseFontSize.Execute(null, _rtb);
            }
            catch (Exception ex)
            {
                AppLog.Message("Command_FontSizeDown()", ex);
            }
        }

        /// <summary>
        /// RichTextBox editing command.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Command_IncreaseIndent(object sender, RoutedEventArgs e)
        {
            try
            {
                EditingCommands.IncreaseIndentation.Execute(null, _rtb);
            }
            catch (Exception ex)
            {
                AppLog.Message("Command_IncreaseIndent()", ex);
            }
        }

        /// <summary>
        /// RichTextBox editing command.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Command_DecreaseIndent(object sender, RoutedEventArgs e)
        {
            try
            {
                EditingCommands.DecreaseIndentation.Execute(null, _rtb);
            }
            catch (Exception ex)
            {
                AppLog.Message("Command_DecreaseIndent()", ex);
            }
        }

    }
}
