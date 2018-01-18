using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

internal class LongDirectory
{
    private const int MAX_PATH = 260;

    public static string GetDirectoryName(string fn)
    {
        return fn.Substring(0, fn.LastIndexOf(Path.DirectorySeparatorChar));
    }

    public static void CreateDirectory(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        if (path.Length < MAX_PATH)
        {
            System.IO.Directory.CreateDirectory(path);
        }
        else
        {
            var paths = GetAllPathsFromPath(GetWin32LongPath(path));
            foreach (var item in paths)
            {
                if (!LongExists(item))
                {
                    var ok = NativeMethods.CreateDirectory(item, IntPtr.Zero);
                    if (!ok)
                    {
                        ThrowWin32Exception();
                    }
                }
            }
        }
    }

    public static void Delete(string path)
    {
        Delete(path, false);
    }

    public static void Delete(string path, bool recursive)
    {
        if (path.Length < MAX_PATH && !recursive)
        {
            System.IO.Directory.Delete(path, false);
        }
        else
        {
            if (!recursive)
            {
                bool ok = NativeMethods.RemoveDirectory(GetWin32LongPath(path));
                if (!ok) ThrowWin32Exception();
            }
            else
            {
                DeleteDirectories(new string[] { GetWin32LongPath(path) });
            }
        }
    }


    private static void DeleteDirectories(string[] directories)
    {
        foreach (string directory in directories)
        {
            string[] files = LongDirectory.GetFiles(directory, null, System.IO.SearchOption.TopDirectoryOnly);
            foreach (string file in files)
            {
                LongFile.Delete(file);
            }
            directories = LongDirectory.GetDirectories(directory, null, System.IO.SearchOption.TopDirectoryOnly);
            DeleteDirectories(directories);
            bool ok = NativeMethods.RemoveDirectory(GetWin32LongPath(directory));
            if (!ok) ThrowWin32Exception();
        }
    }

    public static bool Exists(string path)
    {
        if (path.Length < MAX_PATH) return System.IO.Directory.Exists(path);
        return LongExists(GetWin32LongPath(path));
    }

    private static bool LongExists(string path)
    {
        var attr = NativeMethods.GetFileAttributesW(path);
        return (attr != NativeMethods.INVALID_FILE_ATTRIBUTES && ((attr & NativeMethods.FILE_ATTRIBUTE_DIRECTORY) == NativeMethods.FILE_ATTRIBUTE_DIRECTORY));
    }


    public static string[] GetDirectories(string path)
    {
        return GetDirectories(path, null, SearchOption.TopDirectoryOnly);
    }

    public static string[] GetDirectories(string path, string searchPattern)
    {
        return GetDirectories(path, searchPattern, SearchOption.TopDirectoryOnly);
    }

    public static string[] GetDirectories(string path, string searchPattern, System.IO.SearchOption searchOption)
    {
        searchPattern = searchPattern ?? "*";
        var dirs = new List<string>();
        InternalGetDirectories(path, searchPattern, searchOption, ref dirs);
        return dirs.ToArray();
    }

    private static void InternalGetDirectories(string path, string searchPattern, System.IO.SearchOption searchOption, ref List<string> dirs)
    {
        NativeMethods.WIN32_FIND_DATA findData;
        IntPtr findHandle = NativeMethods.FindFirstFile(System.IO.Path.Combine(GetWin32LongPath(path), searchPattern), out findData);

        try
        {
            if (findHandle != new IntPtr(-1))
            {

                do
                {
                    if ((findData.dwFileAttributes & System.IO.FileAttributes.Directory) != 0)
                    {
                        if (findData.cFileName != "." && findData.cFileName != "..")
                        {
                            string subdirectory = System.IO.Path.Combine(path, findData.cFileName);
                            dirs.Add(GetCleanPath(subdirectory));
                            if (searchOption == SearchOption.AllDirectories)
                            {
                                InternalGetDirectories(subdirectory, searchPattern, searchOption, ref dirs);
                            }
                        }
                    }
                } while (NativeMethods.FindNextFile(findHandle, out findData));
                NativeMethods.FindClose(findHandle);
            }
            else
            {
                ThrowWin32Exception();
            }
        }
        catch (Exception)
        {
            NativeMethods.FindClose(findHandle);
            throw;
        }
    }

    public static string[] GetFiles(string path)
    {
        return GetFiles(path, null, SearchOption.TopDirectoryOnly);
    }

    public static string[] GetFiles(string path, string searchPattern)
    {
        return GetFiles(path, searchPattern, SearchOption.TopDirectoryOnly);
    }


    public static string[] GetFiles(string path, string searchPattern, System.IO.SearchOption searchOption)
    {
        searchPattern = searchPattern ?? "*";

        var files = new List<string>();
        var dirs = new List<string> { path };

        if (searchOption == SearchOption.AllDirectories)
        {
            //Add all the subpaths
            dirs.AddRange(LongDirectory.GetDirectories(path, null, SearchOption.AllDirectories));
        }

        foreach (var dir in dirs)
        {
            NativeMethods.WIN32_FIND_DATA findData;
            IntPtr findHandle = NativeMethods.FindFirstFile(System.IO.Path.Combine(GetWin32LongPath(dir), searchPattern), out findData);

            try
            {
                if (findHandle != new IntPtr(-1))
                {

                    do
                    {
                        if ((findData.dwFileAttributes & System.IO.FileAttributes.Directory) == 0)
                        {
                            string filename = System.IO.Path.Combine(dir, findData.cFileName);
                            files.Add(GetCleanPath(filename));
                        }
                    } while (NativeMethods.FindNextFile(findHandle, out findData));
                    NativeMethods.FindClose(findHandle);
                }
            }
            catch (Exception)
            {
                NativeMethods.FindClose(findHandle);
                throw;
            }
        }

        return files.ToArray();
    }



    public static void Move(string sourceDirName, string destDirName)
    {
        if (sourceDirName.Length < MAX_PATH || destDirName.Length < MAX_PATH)
        {
            System.IO.Directory.Move(sourceDirName, destDirName);
        }
        else
        {
            var ok = NativeMethods.MoveFileW(GetWin32LongPath(sourceDirName), GetWin32LongPath(destDirName));
            if (!ok) ThrowWin32Exception();
        }
    }

    #region Helper methods



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

        var newpath = path;
        if (newpath.StartsWith("\\"))
        {
            newpath = @"\\?\UNC\" + newpath.Substring(2);
        }
        else if (newpath.Contains(":"))
        {
            newpath = @"\\?\" + newpath;
        }
        else
        {
            var currdir = Environment.CurrentDirectory;
            newpath = Combine(currdir, newpath);
            while (newpath.Contains("\\.\\")) newpath = newpath.Replace("\\.\\", "\\");
            newpath = @"\\?\" + newpath;
        }
        return newpath.TrimEnd('.');
    }

    private static string GetCleanPath(string path)
    {
        if (path.StartsWith(@"\\?\UNC\")) return @"\\" + path.Substring(8);
        if (path.StartsWith(@"\\?\")) return path.Substring(4);
        return path;
    }

    private static List<string> GetAllPathsFromPath(string path)
    {
        bool unc = false;
        var prefix = @"\\?\";
        if (path.StartsWith(prefix + @"UNC\"))
        {
            prefix += @"UNC\";
            unc = true;
        }
        var split = path.Split('\\');
        int i = unc ? 6 : 4;
        var list = new List<string>();
        var txt = "";

        for (int a = 0; a < i; a++)
        {
            if (a > 0) txt += "\\";
            txt += split[a];
        }
        for (; i < split.Length; i++)
        {
            txt = Combine(txt, split[i]);
            list.Add(txt);
        }

        return list;
    }

    private static string Combine(string path1, string path2)
    {
        return path1.TrimEnd('\\') + "\\" + path2.TrimStart('\\').TrimEnd('.');
    }


    #endregion
}