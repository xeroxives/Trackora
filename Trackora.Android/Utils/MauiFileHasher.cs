using System;
using System.IO;
using System.Security.Cryptography;
using Trackora.Core.Interfaces;
using Trackora.Core.Utils;

namespace Trackora.Android.Utils;

public class MauiFileHasher : I_FileHasher
{
    public string ComputePartialHash(string filePath)
    {
        return ComputePureManagedHash(filePath);
    }

    private static string ComputePureManagedHash(string filePath)
    {
        if (!File.Exists(filePath)) return "";

        try
        {
            const int bytesToRead = 64 * 1024;

            using (var stream = File.OpenRead(filePath))
            using (var sha256 = SHA256.Create())
            {
                long fileSize = stream.Length;
                byte[] buffer;

                if (fileSize <= bytesToRead * 2)
                {
                    buffer = new byte[fileSize];
                    stream.Read(buffer, 0, (int)fileSize);
                }
                else
                {
                    buffer = new byte[bytesToRead * 2 + sizeof(long)];
                    stream.Read(buffer, 0, bytesToRead);
                    stream.Seek(-bytesToRead, SeekOrigin.End);
                    stream.Read(buffer, bytesToRead, bytesToRead);
                    BitConverter.GetBytes(fileSize).CopyTo(buffer, bytesToRead * 2);
                }

                byte[] hashBytes = sha256.ComputeHash(buffer);
                string base64Hash = Convert.ToBase64String(hashBytes);

                Logger.Log($"[HASHER-ANDROID] Hash computed for: {Path.GetFileName(filePath)} -> {base64Hash.Substring(0, 12)}...");
                return base64Hash;
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"[HASHER-ANDROID] Hash failed for {filePath}: {ex.Message}");
            return "";
        }
    }
}
