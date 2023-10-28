using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for ArticleSearchCriteriaDialog.xaml
    /// </summary>
    public partial class ArticleSearchCriteriaDialog : Window
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public ArticleSearchCriteriaDialog()
        {
            InitializeComponent();

            UiTbWhite.Focus();
        }

        /// <summary>
        /// The user clicked OK to exit dialog.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
