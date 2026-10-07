using System;
using System.IO;
using System.Security.Cryptography;

namespace WpfApp14.Utils
{
    public static class FileHasher
    {
        public static string ComputePartialHash(string filePath)
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
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}