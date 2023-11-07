using ChessPosition;
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
using System.Xml.Linq;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for IntroMoveDialog.xaml
    /// </summary>
    public partial class IntroMoveDialog : Window
    {
        /// <summary>
        /// Text of the move for display.
        /// </summary>
        public string MoveText;

        /// <summary>
        /// Text of the Font Size text box.
        /// </summary>
        public string MoveFontSize;

        /// <summary>
        /// Set to true when the user exits the dialog
        /// with the request to create a dialog for the position.
        /// </summary>
        public bool InsertDialogRequest = false;

        /// <summary>
        /// TreeNode handled in this dialog.
        /// </summary>
        private TreeNode _node;

        /// <summary>
        /// Position in the node at the time of opening the dialog.
        /// To be restored on Cancel.
        /// </summary>
        private BoardPosition _originalPos;

        /// <summary>
        /// Run for this move in IntroView
        /// </summary>
        private Run _run;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="nd"></param>
        public IntroMoveDialog(TreeNode nd, Run run)
        {
            InitializeComponent();
            
            _node = nd;
            _run = run;

            _originalPos = new BoardPosition(nd.Position);
            UiTbMoveText.Text = nd.LastMoveAlgebraicNotation;
            UiTbMoveFontSize.Text = run.FontSize.ToString();

            UiTbMoveText.Focus();
        }

        /// <summary>
        /// Exit on OK
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnOk_Click(object sender, RoutedEventArgs e)
        {
            MoveText = UiTbMoveText.Text;
            MoveFontSize = UiTbMoveFontSize.Text;
            DialogResult = true;
        }

        /// <summary>
        /// Exit on Cancel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnCancel_Click(object sender, RoutedEventArgs e)
        {
            _node.Position = new BoardPosition(_originalPos);
            AppState.MainWin.DisplayPosition(_node);
            DialogResult = false;
        }

        /// <summary>
        /// Invoke the Diagram setup dialog.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnEditPosition_Click(object sender, RoutedEventArgs e)
        {
            DiagramSetupDialog dlg = new DiagramSetupDialog(_node);
            //{
            //    Left = AppState.MainWin.ChessForgeMain.Left + 150,
            //    Top = AppState.MainWin.Top + 150,
            //    Topmost = false,
            //    Owner = AppState.MainWin
            //};
            GuiUtilities.PositionDialog(dlg, AppState.MainWin, 150);

            if (dlg.ShowDialog() == true)
            {
                BoardPosition pos = dlg.PositionSetup;
                _node.Position = new BoardPosition(pos);
                AppState.MainWin.MainChessBoard.DisplayPosition(_node, false);
            }
        }

        /// <summary>
        /// Insert a diagram into the Intro view and exit with Ok.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnInsertDiagram_Click(object sender, RoutedEventArgs e)
        {
            MoveText = UiTbMoveText.Text;
            InsertDialogRequest = true;
            DialogResult = true;
        }

        /// <summary>
        /// Handles the key down event in the text box. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiTbMoveText_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (GuiUtilities.InsertFigurine(UiTbMoveText, sender, e))
            {
                e.Handled = true;
            }
        }
    }
}
