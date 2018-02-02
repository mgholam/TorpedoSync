using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
internal struct WIN32_FILE_ATTRIBUTE_DATA
{
    public FileAttributes dwFileAttributes;
    public System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
    public System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
    public System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
    public uint nFileSizeHigh;
    public uint nFileSizeLow;
}

internal enum GET_FILEEX_INFO_LEVELS
{
    GetFileExInfoStandard,
    GetFileExMaxInfoLevel
}

internal class LongFileInfo
{
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool GetFileAttributesEx(string lpFileName, GET_FILEEX_INFO_LEVELS fInfoLevelId, out WIN32_FILE_ATTRIBUTE_DATA fileData);

    public long Length;
    public DateTime CreationTime;
    public DateTime LastWriteTime;
    public DateTime LastAccessTime;
    public string Name;
    public string Extension;
    public string FullName;

    public LongFileInfo(string path)
    {
        // Check path here
        FullName = path;
        Name = path.Split(Path.DirectorySeparatorChar).Last();
        Extension = path.Split('.').Last();
        if (TorpedoSync.Global.isWindows)
        {
            WIN32_FILE_ATTRIBUTE_DATA fileData;

            // FIXx : ?? Append special suffix \\?\ to allow path lengths up to 32767
            //path = "\\\\?\\" + path;

            if (!GetFileAttributesEx(path, GET_FILEEX_INFO_LEVELS.GetFileExInfoStandard, out fileData))
            {
                throw new Win32Exception();
            }
            Length = (long)(((ulong)fileData.nFileSizeHigh << 32) + (ulong)fileData.nFileSizeLow);
            CreationTime = DateTime.FromFileTime((((long)fileData.ftCreationTime.dwHighDateTime) << 32) | ((uint)fileData.ftCreationTime.dwLowDateTime));
            LastWriteTime = DateTime.FromFileTime((((long)fileData.ftLastWriteTime.dwHighDateTime) << 32) | ((uint)fileData.ftLastWriteTime.dwLowDateTime));
            LastAccessTime = DateTime.FromFileTime((((long)fileData.ftLastAccessTime.dwHighDateTime) << 32) | ((uint)fileData.ftLastAccessTime.dwLowDateTime));
        }
        else
        {
            var fi = new FileInfo(path.Replace("\\","/"));
            Length = fi.Length;
            CreationTime = fi.CreationTime;
            LastWriteTime = fi.LastWriteTime;
            LastAccessTime = fi.LastAccessTime;
        }
    }
}