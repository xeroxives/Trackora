using System;
using System.IO;
using System.Security.Cryptography;
using Un4seen.Bass;

namespace WpfApp14.Utils
{
    public static class FileHasher
    {
        /// <summary>
        /// Вычисляет стабильный хеш аудио-трека, игнорируя теги (ID3, Vorbis, обложки).
        /// Хеш меняется только если изменится сама звуковая дорожка (перекодирование, обрезка).
        /// Редактирование названия, исполнителя или альбома НЕ влияет на этот хеш.
        /// </summary>
        public static string ComputePartialHash(string filePath)
        {
            if (!File.Exists(filePath)) return "";

            int streamHandle = 0;
            try
            {
                streamHandle = Bass.BASS_StreamCreateFile(
                    filePath,
                    0,
                    0,
                    BASSFlag.BASS_STREAM_DECODE | BASSFlag.BASS_SAMPLE_FLOAT
                );
                Logger.Log($"[HASHER] streamHandle = {streamHandle}");
                if (streamHandle == 0)
                {
                    Logger.Error($"[HASHER] Failed to create decode stream for: {filePath}. BASS Error: {Bass.BASS_ErrorGetCode()}");
                    return FallbackFileHash(filePath);
                }

                const int bytesToRead = 512 * 1024;
                byte[] buffer = new byte[bytesToRead];

                int bytesRead = Bass.BASS_ChannelGetData(streamHandle, buffer, bytesToRead);

                if (bytesRead <= 0)
                {
                    Logger.Error($"[HASHER] Failed to read audio data from: {filePath}");
                    return FallbackFileHash(filePath);
                }

                long totalBytes = Bass.BASS_ChannelGetLength(streamHandle, BASSMode.BASS_POS_BYTE);
                byte[] lengthBytes = BitConverter.GetBytes(totalBytes);
                byte[] combinedBuffer = new byte[bytesRead + lengthBytes.Length];
                Array.Copy(buffer, 0, combinedBuffer, 0, bytesRead);
                Array.Copy(lengthBytes, 0, combinedBuffer, bytesRead, lengthBytes.Length);

                using (var sha256 = SHA256.Create())
                {
                    byte[] hashBytes = sha256.ComputeHash(combinedBuffer);
                    string base64Hash = Convert.ToBase64String(hashBytes);

                    Logger.Log($"[HASHER] Audio hash computed for: {Path.GetFileName(filePath)} -> {base64Hash.Substring(0, 12)}...");
                    return base64Hash;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[HASHER] Exception while hashing {filePath}: {ex.Message}");
                return FallbackFileHash(filePath);
            }
            finally
            {
                if (streamHandle != 0)
                {
                    Bass.BASS_StreamFree(streamHandle);
                }
            }
        }

        // Вычисляет хеш аудио-трека, но включая теги (ID3, Vorbis, обложки).
        // ХЕШ МЕНЯЕТСЯ ПРИ ИЗМЕНЕНИИ ТЕГОВ!
        // Редактирование названия, исполнителя или альбома ВЛИЯЕТ на этот хеш.
        private static string FallbackFileHash(string filePath)
        {
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

                    Logger.Log($"[HASHER] Fallback file hash computed for: {Path.GetFileName(filePath)} -> {base64Hash.Substring(0, 12)}...");
                    return base64Hash;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[HASHER] Fallback hash failed for {filePath}: {ex.Message}");
                return "";
            }
        }
    }
}