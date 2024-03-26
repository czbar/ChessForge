using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for SelectLibraryDialog.xaml
    /// </summary>
    public partial class SelectLibraryDialog : Window
    {
        // set to true if any editing has occured.
        private bool _isModified;

        /// <summary>
        /// URL of the selected library.
        /// </summary>
        public string LibraryToOpen;

        /// <summary>
        /// Creates the dialog.
        /// Populates the list.
        /// </summary>
        public SelectLibraryDialog()
        {
            InitializeComponent();

            // add elipsis to the Add button's label
            UiBtnAddLibrary.Content = Properties.Resources.Add + "...";

            // populate the ListBox
            foreach (string library in Configuration.PrivateLibraries)
            {
                UiLbLibraries.Items.Add(library);
                if (library == Configuration.LastPrivateLibrary)
                {
                    UiLbLibraries.SelectedItem = library;
                }
            }

            EnableControls(false);
        }

        /// <summary>
        /// Adjust the appearance of the controls per the current state of the data. 
        /// </summary>
        /// <param name="dataChanged">True if invoked due to a change in data.</param>
        private void EnableControls(bool dataChanged)
        {
            if (dataChanged)
            {
                _isModified = true;
            }

            UiBtnDeleteLibrary.IsEnabled = UiLbLibraries.Items.Count > 0;
            UiBtnOpenPrivateLibrary.IsEnabled = UiLbLibraries.SelectedIndex >= 0;
            UiBtnSaveAndExit.IsEnabled = _isModified;
        }

        /// <summary>
        /// Saves the configured private libraries.
        /// </summary>
        /// <param name="saveLastPrivateLib">If true, saves the currently selected library as the last one invoked.</param>
        private void SaveConfiguration(bool saveLastPrivateLib)
        {
            try
            {
                Configuration.PrivateLibraries.Clear();

                foreach (object item in UiLbLibraries.Items)
                {
                    Configuration.PrivateLibraries.Add(item as string);
                }

                if (saveLastPrivateLib && UiLbLibraries.SelectedIndex >= 0)
                {
                    Configuration.LastPrivateLibrary = UiLbLibraries.SelectedItem as string;
                }

                Configuration.WriteOutConfiguration();
            }
            catch { }
        }

        /// <summary>
        /// One of the Open buttons was clicked.
        /// Warn the user if there are unsaved changes, act on their response
        /// and return proceed / don't proceed. 
        /// </summary>
        /// <param name="isPrivate"></param>
        /// <returns></returns>
        private bool ProceedOnOpen(bool isPrivate)
        {
            bool proceed = true;

            if (_isModified)
            {
                // ask the user whether to save chnages
                MessageBoxResult res = MessageBox.Show(Properties.Resources.MsgSaveLibraryChanges,
                    Properties.Resources.DlgOnlineLibraries,
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                // proceed whether they answer yes or no, but do not proceed on Cancel.
                if (res == MessageBoxResult.Yes)
                {
                    SaveConfiguration(isPrivate);
                    proceed = true;
                }
                else if (res == MessageBoxResult.No)
                {
                    proceed = true;
                }
                else
                {
                    proceed = false;
                }
            }
            else
            {
                if (isPrivate)
                {
                    Configuration.LastPrivateLibrary = UiLbLibraries.SelectedItem as string;
                }
            }

            return proceed;
        }

        /// <summary>
        /// Invokes the dialog for editing the text of an item.
        /// If this is a new item it will be added to the list,
        /// otherwise the currently selected item will be updated.
        /// </summary>
        /// <param name="add">True if an itme is being added, false if edited.</param>
        /// <returns></returns>
        private bool EditItem(bool add)
        {
            bool res = false;
            int selIndex;

            string textOnEntry = "";

            if (!add)
            {
                selIndex = UiLbLibraries.SelectedIndex;
                textOnEntry = UiLbLibraries.Items[selIndex] as string;
            }

            SingleLineEditor dlg = new SingleLineEditor(Properties.Resources.LibraryUrl, textOnEntry);
            GuiUtilities.PositionDialog(dlg, this, 100);
            if (dlg.ShowDialog() == true)
            {
                string txt = dlg.UiTbText.Text;
                if (!string.IsNullOrEmpty(txt))
                {
                    if (add)
                    {
                        selIndex = UiLbLibraries.Items.Add(txt);
                    }
                    else
                    {
                        selIndex = UiLbLibraries.SelectedIndex;
                        if (selIndex >= 0)
                        {
                            UiLbLibraries.Items[selIndex] = txt;
                        }
                    }
                    UiLbLibraries.SelectedIndex = selIndex;
                    EnableControls(true);
                    res = true;
                }
            }

            return res;
        }

        /// <summary>
        /// The user wants to add a new entry.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnAddLibrary_Click(object sender, RoutedEventArgs e)
        {
            EditItem(true);
        }

        /// <summary>
        /// The user deleted a private library entry.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnDeleteLibrary_Click(object sender, RoutedEventArgs e)
        {
            int sel = UiLbLibraries.SelectedIndex;
            if (sel >= 0)
            {
                UiLbLibraries.Items.RemoveAt(sel);

                int count = UiLbLibraries.Items.Count;
                if (sel < count)
                {
                    UiLbLibraries.SelectedIndex = sel;
                }
                else if (count > 0)
                {
                    UiLbLibraries.SelectedIndex = count - 1;
                }

                EnableControls(true);
            }
        }

        /// <summary>
        /// Check for unsaved changes and open the public library.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnOpenPublicLibrary_Click(object sender, RoutedEventArgs e)
        {
            if (ProceedOnOpen(false))
            {
                LibraryToOpen = Configuration.PUBLIC_LIBRARY_URL;
                DialogResult = true;
            }
        }

        /// <summary>
        /// Check for unsaved changes and open the selected private library.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnOpenPrivateLibrary_Click(object sender, RoutedEventArgs e)
        {
            if (ProceedOnOpen(true))
            {
                LibraryToOpen = UiLbLibraries.SelectedItem as string;
                DialogResult = true;
            }
        }

        /// <summary>
        /// Save the changes and exit
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnSaveAndExit_Click(object sender, RoutedEventArgs e)
        {
            SaveConfiguration(false);
            DialogResult = true;
        }


        /// <summary>
        /// Links to the relevant Wiki page.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnHelp_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/czbar/ChessForge/wiki/Online-Libraries");
        }

        /// <summary>
        /// A ListBox received a double-click.
        /// If an item was clicked, invoke the dialog to edit that item.
        /// Note that strictly speaking we are not checking which item was clicked but 
        /// rely on the framework to select that item before we check the selection.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiLbLibraries_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ListBoxItem item = GuiUtilities.GetListBoxItemFromPoint(UiLbLibraries, e.GetPosition(UiLbLibraries));

            if (item != null)
            {
                EditItem(false);
            }
        }

        /// <summary>
        /// Update states of the controls when the list selection changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiLbLibraries_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            EnableControls(false);
        }
    }
}
