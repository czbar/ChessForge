using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ChessForge
{
    /// <summary>
    /// WPF for .NET 4.8 does not have a built-in folder picker dialog, so we need to use the COM-based FileOpenDialog with appropriate options.
    /// </summary>
    public static class FolderPicker
    {
        private const uint FOS_PICKFOLDERS = 0x00000020;
        private const uint FOS_FORCEFILESYSTEM = 0x00000040;
        private const uint FOS_PATHMUSTEXIST = 0x00000800;

        public static string ShowDialog(
            Window owner = null,
            string title = null,
            string initialFolder = null,
            bool forceInitialFolder = true)
        {
            var dialog = (IFileOpenDialog)new FileOpenDialogRCW();

            try
            {
                dialog.GetOptions(out uint options);
                dialog.SetOptions(options | FOS_PICKFOLDERS | FOS_FORCEFILESYSTEM | FOS_PATHMUSTEXIST);

                if (!string.IsNullOrWhiteSpace(title))
                    dialog.SetTitle(title);

                if (!string.IsNullOrWhiteSpace(initialFolder) && Directory.Exists(initialFolder))
                {
                    IShellItem folderItem = CreateShellItem(initialFolder);

                    try
                    {
                        if (forceInitialFolder)
                            dialog.SetFolder(folderItem);
                        else
                            dialog.SetDefaultFolder(folderItem);
                    }
                    finally
                    {
                        Marshal.FinalReleaseComObject(folderItem);
                    }
                }

                IntPtr hwnd = IntPtr.Zero;
                if (owner != null)
                    hwnd = new WindowInteropHelper(owner).Handle;

                int hr = dialog.Show(hwnd);

                // ERROR_CANCELLED
                if ((uint)hr == 0x800704C7)
                    return null;

                Marshal.ThrowExceptionForHR(hr);

                dialog.GetResult(out var item);
                item.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out IntPtr pszString);

                try
                {
                    return Marshal.PtrToStringAuto(pszString);
                }
                finally
                {
                    Marshal.FreeCoTaskMem(pszString);
                }
            }
            finally
            {
                Marshal.FinalReleaseComObject(dialog);
            }
        }

        private static IShellItem CreateShellItem(string path)
        {
            Guid iid = typeof(IShellItem).GUID;
            int hr = SHCreateItemFromParsingName(path, IntPtr.Zero, ref iid, out IShellItem item);
            Marshal.ThrowExceptionForHR(hr);
            return item;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
        private static extern int SHCreateItemFromParsingName(
            string pszPath,
            IntPtr pbc,
            ref Guid riid,
            [MarshalAs(UnmanagedType.Interface)] out IShellItem ppv);
    }


    [ComImport]
    [Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7")]
    internal class FileOpenDialogRCW
    {
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("d57c7288-d4ad-4768-be02-9d969532d960")]
    internal interface IFileOpenDialog
    {
        [PreserveSig] int Show(IntPtr parent);
        void SetFileTypes(uint cFileTypes, IntPtr rgFilterSpec);
        void SetFileTypeIndex(uint iFileType);
        void GetFileTypeIndex(out uint piFileType);
        void Advise(IntPtr pfde, out uint pdwCookie);
        void Unadvise(uint dwCookie);
        void SetOptions(uint fos);
        void GetOptions(out uint pfos);
        void SetDefaultFolder(IShellItem psi);
        void SetFolder(IShellItem psi);
        void GetFolder(out IShellItem ppsi);
        void GetCurrentSelection(out IShellItem ppsi);
        void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);
        void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
        void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszText);
        void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
        void GetResult(out IShellItem ppsi);
        void AddPlace(IShellItem psi, uint fdap);
        void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);
        void Close(int hr);
        void SetClientGuid(ref Guid guid);
        void ClearClientData();
        void SetFilter(IntPtr pFilter);
        void GetResults(out IntPtr ppenum);
        void GetSelectedItems(out IntPtr ppsai);
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
    internal interface IShellItem
    {
        void BindToHandler(IntPtr pbc, ref Guid bhid, ref Guid riid, out IntPtr ppv);
        void GetParent(out IShellItem ppsi);
        void GetDisplayName(SIGDN sigdnName, out IntPtr ppszName);
        void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);
        void Compare(IShellItem psi, uint hint, out int piOrder);
    }

    internal enum SIGDN : uint
    {
        SIGDN_FILESYSPATH = 0x80058000
    }
}