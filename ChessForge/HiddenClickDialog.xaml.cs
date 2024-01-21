using ChessPosition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ChessForge
{
    /// <summary>
    /// This is an "invisible" dialog that can be invoked
    /// as modal with ShowDialog.
    /// It will exit after the first click was detected.
    /// The coordinates of the clicked point are stored
    /// in ClickPoint that can be queried after the dialog exits.
    /// </summary>
    public partial class HiddenClickDialog : Window
    {
        /// <summary>
        /// Coordinates of the clicked point.
        /// </summary>
        public Point ClickPoint;

        /// <summary>
        /// Constructor.
        /// </summary>
        public HiddenClickDialog()
        {
            InitializeComponent();
            ClickPoint.X = -1;
            ClickPoint.Y = -1;
        }

        /// <summary>
        /// Saves the clicked point's coordinates,
        /// marks event as handled and exits.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ClickPoint = e.GetPosition(AppState.MainWin.MainCanvas);
            e.Handled = true;
            DialogResult = true;
        }

        /// <summary>
        /// Allow inspection of mouse events outside of the dialog.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Activated(object sender, EventArgs e)
        {
            System.Windows.Input.Mouse.Capture(this, System.Windows.Input.CaptureMode.SubTree);
        }
    }
}
