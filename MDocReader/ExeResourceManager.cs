using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;

public class ExeResourceManager
{
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr BeginUpdateResource(string pFileName, bool bDeleteExistingResources);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool EndUpdateResource(IntPtr hUpdate, bool fDiscard);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool UpdateResource(IntPtr hUpdate, IntPtr lpType, string lpName, ushort wLanguage, byte[] lpData, uint cbData);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindResource(IntPtr hModule, string lpName, IntPtr lpType);

    [DllImport("kernel32.dll")]
    private static extern IntPtr LoadResource(IntPtr hModule, IntPtr hResInfo);

    [DllImport("kernel32.dll")]
    private static extern IntPtr LockResource(IntPtr hResData);

    [DllImport("kernel32.dll")]
    private static extern uint SizeofResource(IntPtr hModule, IntPtr hResInfo);

    [DllImport("kernel32.dll")]
    private static extern IntPtr LoadLibrary(string lpFileName);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool FreeLibrary(IntPtr hModule);

    private static readonly IntPtr RT_RCDATA = new IntPtr(10);
    private const string FILE_INDEX_RESOURCE = "FILE_INDEX";
    private const char FILE_SEPARATOR = '|';

    public static void PersistTextFiles(Dictionary<string, string> files)
    {
        List<string> currentFiles = GetPersistedFilesList();
        if (currentFiles == null)
        {
            currentFiles = new List<string>();
        }
        Dictionary<string, string> updatedFiles = new Dictionary<string, string>(files);

        foreach (string fileName in files.Keys)
        {
            if (!currentFiles.Contains(fileName))
            {
                currentFiles.Add(fileName);
            }
        }

        updatedFiles[FILE_INDEX_RESOURCE] = SerializeFileList(currentFiles);

        UpdateResource(updatedFiles);
    }

    private static string SerializeFileList(List<string> files)
    {
        if (files == null || files.Count == 0)
        {
            return string.Empty;
        }

        List<string> encodedNames = new List<string>();
        foreach (string file in files)
        {
            encodedNames.Add(file.Replace("|", "%7C"));
        }

        return string.Join(FILE_SEPARATOR.ToString(), encodedNames);
    }

    private static List<string> DeserializeFileList(string serializedList)
    {
        if (string.IsNullOrEmpty(serializedList))
            return new List<string>();

        string[] encodedNames = serializedList.Split(FILE_SEPARATOR);
        List<string> fileNames = new List<string>();

        foreach (string encoded in encodedNames)
        {
            if (!string.IsNullOrEmpty(encoded))
            {
                fileNames.Add(encoded.Replace("%7C", "|"));
            }
        }

        return fileNames;
    }

    public static List<string> GetPersistedFilesList()
    {
        string serializedList = ReadTextFile(FILE_INDEX_RESOURCE);
        if (string.IsNullOrEmpty(serializedList))
        {
            return null;
        }

        return DeserializeFileList(serializedList);
    }

    public static string ReadTextFile(string filePath)
    {
        string resourceName = GetResourceNameFromFilePath(filePath);

        IntPtr hModule = LoadLibrary(Process.GetCurrentProcess().MainModule.FileName);
        if (hModule == IntPtr.Zero) return null;

        try
        {
            IntPtr hResInfo = FindResource(hModule, resourceName, RT_RCDATA);
            if (hResInfo == IntPtr.Zero)
            {
                return null;
            }

            IntPtr hResData = LoadResource(hModule, hResInfo);
            if (hResData == IntPtr.Zero)
            {
                return null;
            }

            IntPtr pResData = LockResource(hResData);
            if (pResData == IntPtr.Zero)
            {
                return null;
            }

            uint size = SizeofResource(hModule, hResInfo);
            byte[] buffer = new byte[size];
            Marshal.Copy(pResData, buffer, 0, (int)size);

            return Encoding.UTF8.GetString(buffer);
        }
        finally
        {
            FreeLibrary(hModule);
        }
    }

    private static string GetResourceNameFromFilePath(string filePath)
    {
        if (filePath == FILE_INDEX_RESOURCE)
        {
            return FILE_INDEX_RESOURCE;
        }

        return "File_" + filePath.Replace("\\", "_").Replace("/", "_").Replace(".", "_").Replace(" ", "_");
    }

    private static void UpdateResource(Dictionary<string, string> files)
    {
        string exePath = Process.GetCurrentProcess().MainModule.FileName;
        string currentDirectory = Directory.GetCurrentDirectory();
        string folderName = Path.GetFileName(currentDirectory);
        string tempExePath = Path.Combine(Directory.GetCurrentDirectory(), folderName + "-" + Guid.NewGuid().ToString() + ".exe");

        File.Copy(exePath, tempExePath, true);

        IntPtr handle = BeginUpdateResource(tempExePath, false);
        if (handle != IntPtr.Zero)
        {
            try
            {
                foreach (var entry in files)
                {
                    string resourceName = GetResourceNameFromFilePath(entry.Key);
                    byte[] dataBytes = Encoding.UTF8.GetBytes(entry.Value);

                    bool res = UpdateResource(handle, RT_RCDATA, resourceName, 0, dataBytes, (uint)dataBytes.Length);
                }

                EndUpdateResource(handle, false);
            }
            catch
            {
                EndUpdateResource(handle, true);
                throw;
            }
        }
    }
}