using Microsoft.Win32.SafeHandles;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

internal static class LongFile
{
    private const int MAX_PATH = 260;

    public static bool Exists(string path)
    {
        if (TorpedoSync.Global.isWindows == false)
            return System.IO.File.Exists(path.Replace("\\", "/"));

        if (path.Length < MAX_PATH)
            return System.IO.File.Exists(path);

        var attr = NativeMethods.GetFileAttributesW(GetWin32LongPath(path));
        return (attr != NativeMethods.INVALID_FILE_ATTRIBUTES && ((attr & NativeMethods.FILE_ATTRIBUTE_ARCHIVE) == NativeMethods.FILE_ATTRIBUTE_ARCHIVE));
    }

    public static void Delete(string path)
    {
        SetAttributes(path, FileAttributes.Normal);

        if (path.Length < MAX_PATH || TorpedoSync.Global.isWindows == false)
            System.IO.File.Delete(path.Replace("\\", "/"));
        else
        {
            bool ok = NativeMethods.DeleteFileW(GetWin32LongPath(path));
            if (!ok) ThrowWin32Exception();
        }
    }

    public static void AppendAllText(string path, string contents)
    {
        AppendAllText(path, contents, Encoding.Default);
    }

    public static void AppendAllText(string path, string contents, Encoding encoding)
    {
        if (TorpedoSync.Global.isWindows == false)
            System.IO.File.AppendAllText(path.Replace("\\", "/"), contents, encoding);

        else if (path.Length < MAX_PATH)
            System.IO.File.AppendAllText(path, contents, encoding);

        else
        {
            var fileHandle = CreateFileForAppend(GetWin32LongPath(path));
            using (var fs = new System.IO.FileStream(fileHandle, System.IO.FileAccess.Write))
            {
                var bytes = encoding.GetBytes(contents);
                fs.Position = fs.Length;
                fs.Write(bytes, 0, bytes.Length);
            }
        }
    }

    public static void WriteAllText(string path, string contents)
    {
        WriteAllText(path, contents, Encoding.Default);
    }

    public static void WriteAllText(string path, string contents, Encoding encoding)
    {
        if (TorpedoSync.Global.isWindows == false)
            System.IO.File.WriteAllText(path.Replace("\\", "/"), contents, encoding);

        else if (path.Length < MAX_PATH)
            System.IO.File.WriteAllText(path, contents, encoding);

        else
        {
            var fileHandle = CreateFileForWrite(GetWin32LongPath(path));

            using (var fs = new System.IO.FileStream(fileHandle, System.IO.FileAccess.Write))
            {
                var bytes = encoding.GetBytes(contents);
                fs.Write(bytes, 0, bytes.Length);
            }
        }
    }

    public static void WriteAllBytes(string path, byte[] bytes)
    {
        if (TorpedoSync.Global.isWindows == false)
            System.IO.File.WriteAllBytes(path.Replace("\\", "/"), bytes);

        else if (path.Length < MAX_PATH || TorpedoSync.Global.isWindows == false)
            System.IO.File.WriteAllBytes(path, bytes);

        else
        {
            var fileHandle = CreateFileForWrite(GetWin32LongPath(path));

            using (var fs = new System.IO.FileStream(fileHandle, System.IO.FileAccess.Write))
            {
                fs.Write(bytes, 0, bytes.Length);
            }
        }
    }

    public static void Copy(string sourceFileName, string destFileName)
    {
        Copy(sourceFileName, destFileName, false);
    }

    public static void Copy(string sourceFileName, string destFileName, bool overwrite)
    {
        if (TorpedoSync.Global.isWindows == false)
            System.IO.File.Copy(sourceFileName.Replace("\\", "/"), destFileName.Replace("\\", "/"), overwrite);

        else if (sourceFileName.Length < MAX_PATH && destFileName.Length < MAX_PATH)
            System.IO.File.Copy(sourceFileName, destFileName, overwrite);
        else
        {
            var ok = NativeMethods.CopyFileW(GetWin32LongPath(sourceFileName), GetWin32LongPath(destFileName), !overwrite);
            if (!ok) ThrowWin32Exception();
        }
    }

    public static void Move(string sourceFileName, string destFileName)
    {
        if (TorpedoSync.Global.isWindows == false)
            System.IO.File.Move(sourceFileName.Replace("\\", "/"), destFileName.Replace("\\", "/"));

        else if (sourceFileName.Length < MAX_PATH && destFileName.Length < MAX_PATH)
            System.IO.File.Move(sourceFileName, destFileName);
        else
        {
            var ok = NativeMethods.MoveFileW(GetWin32LongPath(sourceFileName), GetWin32LongPath(destFileName));
            if (!ok) ThrowWin32Exception();
        }
    }

    public static string ReadAllText(string path)
    {
        return ReadAllText(path, Encoding.Default);
    }

    public static string ReadAllText(string path, Encoding encoding)
    {
        if (TorpedoSync.Global.isWindows == false)
            return System.IO.File.ReadAllText(path.Replace("\\", "/"), encoding);

        if (path.Length < MAX_PATH)
            return System.IO.File.ReadAllText(path, encoding);

        var fileHandle = GetFileHandle(GetWin32LongPath(path));

        using (var fs = new System.IO.FileStream(fileHandle, System.IO.FileAccess.Read))
        {
            var data = new byte[fs.Length];
            fs.Read(data, 0, data.Length);
            return encoding.GetString(data);
        }
    }

    public static string[] ReadAllLines(string path)
    {
        return ReadAllLines(path, Encoding.Default);
    }

    public static string[] ReadAllLines(string path, Encoding encoding)
    {
        if (TorpedoSync.Global.isWindows == false)
            return System.IO.File.ReadAllLines(path.Replace("\\", "/"), encoding);

        if (path.Length < MAX_PATH || TorpedoSync.Global.isWindows == false)
            return System.IO.File.ReadAllLines(path, encoding);

        var fileHandle = GetFileHandle(GetWin32LongPath(path));

        using (var fs = new System.IO.FileStream(fileHandle, System.IO.FileAccess.Read))
        {
            var data = new byte[fs.Length];
            fs.Read(data, 0, data.Length);
            var str = encoding.GetString(data);
            if (str.Contains("\r")) return str.Split(new[] { "\r\n" }, StringSplitOptions.None);
            return str.Split('\n');
        }
    }
    public static byte[] ReadAllBytes(string path)
    {
        if (TorpedoSync.Global.isWindows == false)
            return System.IO.File.ReadAllBytes(path.Replace("\\", "/"));

        if (path.Length < MAX_PATH || TorpedoSync.Global.isWindows == false)
            return System.IO.File.ReadAllBytes(path);

        var fileHandle = GetFileHandle(GetWin32LongPath(path));

        using (var fs = new System.IO.FileStream(fileHandle, System.IO.FileAccess.Read))
        {
            var data = new byte[fs.Length];
            fs.Read(data, 0, data.Length);
            return data;
        }
    }

    public static void SetAttributes(string path, FileAttributes attributes)
    {
        if (TorpedoSync.Global.isWindows == false)
            System.IO.File.SetAttributes(path.Replace("\\","/"), attributes);

        else if (path.Length < MAX_PATH)
            System.IO.File.SetAttributes(path, attributes);
        else
        {
            var longFilename = GetWin32LongPath(path);
            NativeMethods.SetFileAttributesW(longFilename, (int)attributes);
        }
    }

    #region Helper methods

    private static SafeFileHandle CreateFileForWrite(string filename)
    {
        if (filename.Length >= MAX_PATH) filename = GetWin32LongPath(filename);
        SafeFileHandle hfile = NativeMethods.CreateFile(filename, (int)NativeMethods.FILE_GENERIC_WRITE, NativeMethods.FILE_SHARE_NONE, IntPtr.Zero, NativeMethods.CREATE_ALWAYS, 0, IntPtr.Zero);
        if (hfile.IsInvalid) ThrowWin32Exception();
        return hfile;
    }

    private static SafeFileHandle CreateFileForAppend(string filename)
    {
        if (filename.Length >= MAX_PATH) filename = GetWin32LongPath(filename);
        SafeFileHandle hfile = NativeMethods.CreateFile(filename, (int)NativeMethods.FILE_GENERIC_WRITE, NativeMethods.FILE_SHARE_NONE, IntPtr.Zero, NativeMethods.CREATE_NEW, 0, IntPtr.Zero);
        if (hfile.IsInvalid)
        {
            hfile = NativeMethods.CreateFile(filename, (int)NativeMethods.FILE_GENERIC_WRITE, NativeMethods.FILE_SHARE_NONE, IntPtr.Zero, NativeMethods.OPEN_EXISTING, 0, IntPtr.Zero);
            if (hfile.IsInvalid) ThrowWin32Exception();
        }
        return hfile;
    }

    internal static SafeFileHandle GetFileHandle(string filename)
    {
        if (filename.Length >= MAX_PATH) filename = GetWin32LongPath(filename);
        SafeFileHandle hfile = NativeMethods.CreateFile(filename, (int)NativeMethods.FILE_GENERIC_READ, NativeMethods.FILE_SHARE_READ, IntPtr.Zero, NativeMethods.OPEN_EXISTING, 0, IntPtr.Zero);
        if (hfile.IsInvalid) ThrowWin32Exception();
        return hfile;
    }

    internal static SafeFileHandle GetFileHandleWithWrite(string filename)
    {
        if (filename.Length >= MAX_PATH) filename = GetWin32LongPath(filename);
        SafeFileHandle hfile = NativeMethods.CreateFile(filename, (int)(NativeMethods.FILE_GENERIC_READ | NativeMethods.FILE_GENERIC_WRITE | NativeMethods.FILE_WRITE_ATTRIBUTES), NativeMethods.FILE_SHARE_NONE, IntPtr.Zero, NativeMethods.OPEN_EXISTING, 0, IntPtr.Zero);
        if (hfile.IsInvalid) ThrowWin32Exception();
        return hfile;
    }

    //public static System.IO.FileStream GetFileStream(string filename, FileAccess access = FileAccess.Read)
    //{
    //    var longFilename = GetWin32LongPath(filename);
    //    SafeFileHandle hfile;
    //    if (access == FileAccess.Write)
    //    {
    //        hfile = NativeMethods.CreateFile(longFilename, (int)(NativeMethods.FILE_GENERIC_READ | NativeMethods.FILE_GENERIC_WRITE | NativeMethods.FILE_WRITE_ATTRIBUTES), NativeMethods.FILE_SHARE_NONE, IntPtr.Zero, NativeMethods.OPEN_EXISTING, 0, IntPtr.Zero);
    //    }
    //    else
    //    {
    //        hfile = NativeMethods.CreateFile(longFilename, (int)NativeMethods.FILE_GENERIC_READ, NativeMethods.FILE_SHARE_READ, IntPtr.Zero, NativeMethods.OPEN_EXISTING, 0, IntPtr.Zero);
    //    }

    //    if (hfile.IsInvalid) ThrowWin32Exception();

    //    return new System.IO.FileStream(hfile, access);
    //}


    [DebuggerStepThrough]
    public static void ThrowWin32Exception()
    {
        int code = Marshal.GetLastWin32Error();
        if (code != 0)
        {
            throw new System.ComponentModel.Win32Exception(code);
        }
    }

    public static string GetWin32LongPath(string path)
    {
        if (path.StartsWith(@"\\?\")) return path;

        if (path.StartsWith("\\"))
        {
            path = @"\\?\UNC\" + path.Substring(2);
        }
        else if (path.Contains(":"))
        {
            path = @"\\?\" + path;
        }
        else
        {
            var currdir = Environment.CurrentDirectory;
            path = Combine(currdir, path);
            while (path.Contains("\\.\\")) path = path.Replace("\\.\\", "\\");
            path = @"\\?\" + path;
        }
        return path.TrimEnd('.'); ;
    }

    private static string Combine(string path1, string path2)
    {
        return path1.TrimEnd('\\') + "\\" + path2.TrimStart('\\').TrimEnd('.'); ;
    }


    #endregion

    public static void SetCreationTime(string path, DateTime creationTime)
    {
        if (TorpedoSync.Global.isWindows == false)
        {
            System.IO.File.SetCreationTime(path.Replace("\\","/"), creationTime);
            return;
        }

        long cTime = 0;
        long aTime = 0;
        long wTime = 0;

        using (var handle = GetFileHandleWithWrite(path))
        {
            NativeMethods.GetFileTime(handle, ref cTime, ref aTime, ref wTime);
            var fileTime = creationTime.ToFileTimeUtc();
            if (!NativeMethods.SetFileTime(handle, ref fileTime, ref aTime, ref wTime))
            {
                throw new Win32Exception();
            }
        }
    }

    public static void SetLastAccessTime(string path, DateTime lastAccessTime)
    {
        if (TorpedoSync.Global.isWindows == false)
        {
            System.IO.File.SetLastAccessTime(path.Replace("\\", "/"), lastAccessTime);
            return;
        }

        long cTime = 0;
        long aTime = 0;
        long wTime = 0;

        using (var handle = GetFileHandleWithWrite(path))
        {
            NativeMethods.GetFileTime(handle, ref cTime, ref aTime, ref wTime);

            var fileTime = lastAccessTime.ToFileTimeUtc();
            if (!NativeMethods.SetFileTime(handle, ref cTime, ref fileTime, ref wTime))
            {
                throw new Win32Exception();
            }
        }
    }

    public static void SetLastWriteTime(string path, DateTime lastWriteTime)
    {
        if (TorpedoSync.Global.isWindows == false)
        {
            System.IO.File.SetLastWriteTime(path.Replace("\\", "/"), lastWriteTime);
            return;
        }

        long cTime = 0;
        long aTime = 0;
        long wTime = 0;

        using (var handle = GetFileHandleWithWrite(path))
        {
            NativeMethods.GetFileTime(handle, ref cTime, ref aTime, ref wTime);

            var fileTime = lastWriteTime.ToFileTimeUtc();
            if (!NativeMethods.SetFileTime(handle, ref cTime, ref aTime, ref fileTime))
            {
                throw new Win32Exception();
            }
        }
    }

    public static DateTime GetLastWriteTime(string path)
    {
        if (TorpedoSync.Global.isWindows == false)
            return System.IO.File.GetLastWriteTime(path.Replace("\\", "/"));

        long cTime = 0;
        long aTime = 0;
        long wTime = 0;

        using (var handle = GetFileHandle(path))
        {
            NativeMethods.GetFileTime(handle, ref cTime, ref aTime, ref wTime);

            return DateTime.FromFileTimeUtc(wTime);
        }
    }

}