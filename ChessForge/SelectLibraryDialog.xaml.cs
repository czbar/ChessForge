using System;
using System.Collections.Generic;
using System.Linq;
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
        public SelectLibraryDialog()
        {
            InitializeComponent();

            UiBtnAddLibrary.Content = Properties.Resources.Add + "...";
        }
    }
}
