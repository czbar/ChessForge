using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for IntroMoveDialog.xaml
    /// </summary>
    public partial class IntroMoveDialog : Window
    {
        public IntroMoveDialog(TreeNode nd)
        {
            InitializeComponent();

            UiTbMoveText.Text = nd.LastMoveAlgebraicNotation;
        }

        /// <summary>
        /// Exit on OK
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        /// <summary>
        /// Exit on Cancel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult= false;
        }

        /// <summary>
        /// Invoke the Diagram setup dialog.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnEditPosition_Click(object sender, RoutedEventArgs e)
        {

        }

        /// <summary>
        /// Insert a diagram into the Intro view and exit with Ok.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnInsertDiagram_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
