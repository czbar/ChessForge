using System;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace ChessForge
{
    /// <summary>
    /// Class for accessing drag and drop cursors.
    /// </summary>
    public class DragAndDropCursors
    {
        [DllImport("kernel32.dll")]
        public static extern IntPtr LoadLibrary(string dllToLoad);

        [DllImport("user32.dll")]
        public static extern IntPtr LoadCursor(IntPtr hInstance, UInt16 lpCursorName);

        /// <summary>
        /// Builds the black "drop" cursor.
        /// </summary>
        /// <returns></returns>
        public static Cursor GetAllowDropCursor()
        {
            return GetDropCursor(5);
        }

        /// <summary>
        /// Builds the "barred" cursor.
        /// </summary>
        /// <returns></returns>
        public static Cursor GetBarredDropCursor()
        {
            return GetDropCursor(1);
        }

        /// <summary>
        /// Builds the requested cursor.
        /// </summary>
        /// <param name="id">
        /// 1-barred, 
        /// 2-white drop, 
        /// 3-white drop plus, 
        /// 4-white drop arrow, 
        /// 5-black drop, 
        /// 6-black drop plus, 
        /// 7-black drop arrow</param>
        /// <returns></returns>
        private static Cursor GetDropCursor(ushort id)
        {
            IntPtr oleLib = LoadLibrary("ole32.dll");
            IntPtr handle = LoadCursor(oleLib, id);
            return CursorInteropHelper.Create(new SafeCursorHandle(handle));
        }
    }

    /// <summary>
    /// Interop for creating drag-and-drop cursors.
    /// </summary>
    public class SafeCursorHandle : Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid
    {
        /// <summary>
        /// Creates a safe handle.
        /// </summary>
        /// <param name="handle"></param>
        public SafeCursorHandle(IntPtr handle) : base(true)
        {
            base.SetHandle(handle);
        }

        /// <summary>
        /// Releases the handle.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.ComponentModel.Win32Exception"></exception>
        protected override bool ReleaseHandle()
        {
            if (!this.IsInvalid)
            {
                if (!DestroyCursor(this.handle))
                    throw new System.ComponentModel.Win32Exception();
                this.handle = IntPtr.Zero;
            }
            return true;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyCursor(IntPtr handle);
    }
}
