using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for ReorderLinesDialog.xaml
    /// </summary>
    public partial class ReorderLinesDialog : Window
    {
        // node whose children are being reordered.
        private TreeNode _node;

        // move number offset for the node's tree
        private uint _moveNumberOffset;

        // maps move text to TreeNode
        private Dictionary<string, TreeNode> _dictMoveToNode = new Dictionary<string, TreeNode>();

        /// <summary>
        /// Creates the dialog and populates the list of moves. 
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="moveNumberOffset"></param>
        public ReorderLinesDialog(TreeNode nd, uint moveNumberOffset = 0)
        {
            _node = nd;
            _moveNumberOffset = moveNumberOffset;

            InitializeComponent();

            foreach (TreeNode child in nd.Children)
            {
                string moveText = MoveUtils.BuildSingleMoveText(child, true, true, moveNumberOffset);
                UiLbLines.Items.Add(moveText);
                _dictMoveToNode.Add(moveText, child);
            }

            UiLbLines.SelectedIndex = 0;
        }

        /// <summary>
        /// Reorders the children on OK exit.
        /// </summary>
        private void ReorderChildren()
        {
            for (int i = 0; i < UiLbLines.Items.Count; i++) 
            {
                object item = UiLbLines.Items[i];
                string itemText = item.ToString();
                _node.Children[i] = _dictMoveToNode[itemText];
            }
        }

        /// <summary>
        /// Reorder the children per the state of the ListBox
        /// and exit.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnOk_Click(object sender, RoutedEventArgs e)
        {
            ReorderChildren();
            DialogResult = true;
        }

        /// <summary>
        /// Move the selected item up the list.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnUp_Click(object sender, RoutedEventArgs e)
        {
            int selIndex = UiLbLines.SelectedIndex;
            if (selIndex > 0)
            {
                object item = UiLbLines.Items[selIndex - 1];
                UiLbLines.Items[selIndex - 1] = UiLbLines.Items[selIndex];
                UiLbLines.Items[selIndex] = item;
                UiLbLines.SelectedIndex = selIndex - 1;
            }
        }

        /// <summary>
        /// Move the selected item down the list.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnDown_Click(object sender, RoutedEventArgs e)
        {
            int selIndex = UiLbLines.SelectedIndex;
            int count = UiLbLines.Items.Count;
            if (selIndex < count - 1)
            {
                object item = UiLbLines.Items[selIndex + 1];
                UiLbLines.Items[selIndex + 1] = UiLbLines.Items[selIndex];
                UiLbLines.Items[selIndex] = item;
                UiLbLines.SelectedIndex = selIndex + 1;
            }
        }

        /// <summary>
        /// Link to the relevant Wiki page
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnHelp_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/czbar/ChessForge/wiki/Working-with-Moves-and-Lines#reordering-lines");
        }
    }
}
