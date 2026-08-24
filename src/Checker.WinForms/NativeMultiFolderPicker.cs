using System.Runtime.InteropServices;

namespace IDCLogChecker.WinForms;

internal static class NativeMultiFolderPicker
{
    private const int CancelledHResult = unchecked((int)0x800704C7);
    private static readonly Guid FileOpenDialogClassId = new("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7");

    public static IReadOnlyList<string> Show(nint ownerHandle)
    {
        var dialogType = Type.GetTypeFromCLSID(FileOpenDialogClassId, throwOnError: true)!;
        var dialog = (IFileOpenDialog)Activator.CreateInstance(dialogType)!;
        try
        {
            dialog.GetOptions(out var options);
            dialog.SetOptions(options |
                FileOpenOptions.PickFolders |
                FileOpenOptions.AllowMultiSelect |
                FileOpenOptions.ForceFileSystem |
                FileOpenOptions.PathMustExist);
            dialog.SetTitle("选择一个或多个巡检结果文件夹");
            var result = dialog.Show(ownerHandle);
            if (result == CancelledHResult) return [];
            Marshal.ThrowExceptionForHR(result);

            dialog.GetResults(out var items);
            try
            {
                items.GetCount(out var count);
                var paths = new List<string>((int)count);
                for (uint index = 0; index < count; index++)
                {
                    items.GetItemAt(index, out var item);
                    try
                    {
                        item.GetDisplayName(ShellDisplayName.FileSystemPath, out var pointer);
                        try
                        {
                            var path = Marshal.PtrToStringUni(pointer);
                            if (!string.IsNullOrWhiteSpace(path)) paths.Add(path);
                        }
                        finally
                        {
                            Marshal.FreeCoTaskMem(pointer);
                        }
                    }
                    finally
                    {
                        Marshal.ReleaseComObject(item);
                    }
                }
                return paths;
            }
            finally
            {
                Marshal.ReleaseComObject(items);
            }
        }
        finally
        {
            Marshal.ReleaseComObject(dialog);
        }
    }

    [Flags]
    private enum FileOpenOptions : uint
    {
        PickFolders = 0x00000020,
        ForceFileSystem = 0x00000040,
        AllowMultiSelect = 0x00000200,
        PathMustExist = 0x00000800,
    }

    private enum ShellDisplayName : uint
    {
        FileSystemPath = 0x80058000,
    }

    [ComImport, Guid("D57C7288-D4AD-4768-BE02-9D969532D960"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IFileOpenDialog
    {
        [PreserveSig] int Show(nint parent);
        void SetFileTypes(uint count, nint filterSpec);
        void SetFileTypeIndex(uint index);
        void GetFileTypeIndex(out uint index);
        void Advise(nint events, out uint cookie);
        void Unadvise(uint cookie);
        void SetOptions(FileOpenOptions options);
        void GetOptions(out FileOpenOptions options);
        void SetDefaultFolder(IShellItem folder);
        void SetFolder(IShellItem folder);
        void GetFolder(out IShellItem folder);
        void GetCurrentSelection(out IShellItem item);
        void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string name);
        void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string name);
        void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string title);
        void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string text);
        void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string label);
        void GetResult(out IShellItem item);
        void AddPlace(IShellItem item, int placement);
        void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string extension);
        void Close(int hResult);
        void SetClientGuid(in Guid guid);
        void ClearClientData();
        void SetFilter(nint filter);
        void GetResults(out IShellItemArray items);
        void GetSelectedItems(out IShellItemArray items);
    }

    [ComImport, Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItem
    {
        void BindToHandler(nint bindContext, in Guid handler, in Guid interfaceId, out nint result);
        void GetParent(out IShellItem parent);
        void GetDisplayName(ShellDisplayName displayName, out nint name);
        void GetAttributes(uint mask, out uint attributes);
        void Compare(IShellItem item, uint hint, out int order);
    }

    [ComImport, Guid("B63EA76D-1F85-456F-A19C-48159EFA858B"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItemArray
    {
        void BindToHandler(nint bindContext, in Guid handler, in Guid interfaceId, out nint result);
        void GetPropertyStore(int flags, in Guid interfaceId, out nint result);
        void GetPropertyDescriptionList(nint propertyKey, in Guid interfaceId, out nint result);
        void GetAttributes(uint flags, uint mask, out uint attributes);
        void GetCount(out uint count);
        void GetItemAt(uint index, out IShellItem item);
        void EnumItems(out nint enumShellItems);
    }
}
