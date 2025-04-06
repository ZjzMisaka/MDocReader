using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;

public class ExeResourceManager
{
    private const string FILE_INDEX_RESOURCE = "FILE_INDEX";
    private const char FILE_SEPARATOR = '|';

    private static readonly byte[] DATA_MARKER = Encoding.ASCII.GetBytes("MDEXEDATA");

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
        {
            return new List<string>();
        }

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
        string resourceName = filePath;
        string exePath = Process.GetCurrentProcess().MainModule.FileName;
        if (!File.Exists(exePath))
        {
            return null;
        }
        byte[] allData = File.ReadAllBytes(exePath);
        int markerLength = DATA_MARKER.Length;
        int footerLength = markerLength + 4;
        if (allData.Length < footerLength)
        {
            return null;
        }
        int footerOffset = allData.Length - footerLength;
        byte[] footer = new byte[footerLength];
        Array.Copy(allData, footerOffset, footer, 0, footerLength);
        bool markerMatches = true;
        for (int i = 0; i < markerLength; i++)
        {
            if (footer[4 + i] != DATA_MARKER[i])
            {
                markerMatches = false;
                break;
            }
        }
        if (!markerMatches)
        {
            return null;
        }
        int dataBlockLength = BitConverter.ToInt32(footer, 0);
        int dataBlockOffset = allData.Length - footerLength - dataBlockLength;
        if (dataBlockOffset < 0)
        {
            return null;
        }
        byte[] dataBlock = new byte[dataBlockLength];
        Array.Copy(allData, dataBlockOffset, dataBlock, 0, dataBlockLength);
        Dictionary<string, string> resources = DeserializeFiles(dataBlock);
        if (resources != null && resources.ContainsKey(resourceName))
        {
            return resources[resourceName];
        }
        return null;
    }

    private static void UpdateResource(Dictionary<string, string> files)
    {
        string exePath = Process.GetCurrentProcess().MainModule.FileName;
        string currentDirectory = Directory.GetCurrentDirectory();
        string folderName = Path.GetFileName(currentDirectory);
        string newExePath = Path.Combine(Directory.GetCurrentDirectory(), folderName + ".exe");
        if (File.Exists(newExePath))
        {
            newExePath = Path.Combine(Directory.GetCurrentDirectory(), folderName + "-" + Guid.NewGuid().ToString() + ".exe");
        }
        string scriptPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".bat");

        File.Copy(exePath, newExePath, true);

        byte[] serializedDataBlock = SerializeFiles(files);
        using (FileStream fs = new FileStream(newExePath, FileMode.Append, FileAccess.Write))
        {
            fs.Write(serializedDataBlock, 0, serializedDataBlock.Length);
            byte[] lengthBytes = BitConverter.GetBytes(serializedDataBlock.Length);
            fs.Write(lengthBytes, 0, lengthBytes.Length);
            fs.Write(DATA_MARKER, 0, DATA_MARKER.Length);
        }

        Process.Start(newExePath);

        //string script = $@"
        //    @echo off
        //    :WAIT
        //    tasklist /FI ""IMAGENAME eq {Path.GetFileName(exePath)}"" 2>NUL | find /I ""{Path.GetFileName(exePath)}"" >NUL
        //    if %ERRORLEVEL% == 0 (
        //        timeout /t 1 /nobreak >NUL
        //        goto WAIT
        //    )
        //    del ""{exePath}""
        //    del ""{scriptPath}""
        //    ";

        //File.WriteAllText(scriptPath, script);
        //Process.Start(scriptPath);

        //Environment.Exit(0);
    }

    private static byte[] SerializeFiles(Dictionary<string, string> files)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            foreach (var kvp in files)
            {
                byte[] keyBytes = Encoding.UTF8.GetBytes(kvp.Key);
                byte[] valueBytes = Encoding.UTF8.GetBytes(kvp.Value);

                ms.Write(BitConverter.GetBytes(keyBytes.Length), 0, 4);
                ms.Write(keyBytes, 0, keyBytes.Length);
                ms.Write(BitConverter.GetBytes(valueBytes.Length), 0, 4);
                ms.Write(valueBytes, 0, valueBytes.Length);
            }
            return ms.ToArray();
        }
    }

    private static Dictionary<string, string> DeserializeFiles(byte[] data)
    {
        Dictionary<string, string> result = new Dictionary<string, string>();
        using (MemoryStream ms = new MemoryStream(data))
        {
            while (ms.Position < ms.Length)
            {
                byte[] intBuffer = new byte[4];
                if (ms.Read(intBuffer, 0, 4) != 4)
                {
                    break;
                }
                int keyLength = BitConverter.ToInt32(intBuffer, 0);
                byte[] keyBuffer = new byte[keyLength];
                if (ms.Read(keyBuffer, 0, keyLength) != keyLength)
                {
                    break;
                }
                string key = Encoding.UTF8.GetString(keyBuffer);

                if (ms.Read(intBuffer, 0, 4) != 4)
                {
                    break;
                }
                int valueLength = BitConverter.ToInt32(intBuffer, 0);
                byte[] valueBuffer = new byte[valueLength];
                if (ms.Read(valueBuffer, 0, valueLength) != valueLength)
                {
                    break;
                }
                string value = Encoding.UTF8.GetString(valueBuffer);

                result[key] = value;
            }
        }
        return result;
    }
}
