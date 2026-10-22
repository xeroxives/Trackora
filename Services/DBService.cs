using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using System.IO;
using System.Threading.Tasks;
using Trackora.Core.Models;
using System.Diagnostics;
using WpfApp14.Utils;
using Trackora.Core.Utils;

namespace WpfApp14.Services
{
    public class DBService
    {
        #region Private
        private readonly string _connectionString;
        private bool _isInitialized = false;
        private readonly object _lock = new();
        #endregion

        #region InitDB
        private void InitializeDatabase()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                        CREATE TABLE IF NOT EXISTS ListeningStats (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Title TEXT,
                            FileHash TEXT NOT NULL UNIQUE,
                            TotalSeconds REAL NOT NULL DEFAULT 0
                        );";
            Debug.WriteLine($"[DATA DB] Init Stats: {command.ExecuteNonQuery()}");

            command.CommandText = @"
                        CREATE TABLE IF NOT EXISTS Volumes (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Title TEXT,
                            FileHash TEXT NOT NULL UNIQUE,
                            Volume INTEGER NOT NULL DEFAULT 100
                        );";
            Debug.WriteLine($"[DATA DB] Init Volume: {command.ExecuteNonQuery()}");
        }
        #endregion

        #region ListeningTime
        public void AddListeningTime(double seconds, Song s)
        {
            string fileHash = s.FileHash;
            if (!_isInitialized || string.IsNullOrEmpty(fileHash) || seconds <= 0){
				Debug.WriteLine($"[ConnSQL] Start Func | isInit: {_isInitialized} | fh: {fileHash} | sec: {seconds}");
				return;
			}
			

            Task.Run(() =>
            {
				Debug.WriteLine($"[ConnSQL] Start-Task");
				try
                {
					Debug.WriteLine($"[ConnSQL] Try");
                    using var connection = new SqliteConnection(_connectionString);
                    connection.Open();

                    Debug.WriteLine($"[ConnSQL] open: {connection}");

                    using var command = connection.CreateCommand();
                    command.CommandText = @"
                                INSERT INTO ListeningStats (Title, FileHash, TotalSeconds)
                                VALUES (@title, @hash, @seconds)
                                ON CONFLICT(FileHash) DO UPDATE
                                    SET TotalSeconds = TotalSeconds + @seconds, Title = @title;";
                    command.Parameters.AddWithValue("@hash", fileHash);
                    command.Parameters.AddWithValue("@seconds", seconds);
                    command.Parameters.AddWithValue("@title", s.Title);
                    Debug.WriteLine($"[ConnSQL] exec: {command.ExecuteNonQuery()}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[STATS] Failed to save listening time: {ex.Message}");
                }
            });
        }

        public double GetListeningTime(Song s)
        {
            if (!_isInitialized || string.IsNullOrEmpty(s.FileHash))
                return 0;

            try
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = "SELECT TotalSeconds FROM ListeningStats WHERE FileHash = @hash;";
                command.Parameters.AddWithValue("@hash", s.FileHash);

                var result = command.ExecuteScalar();
                return result != null ? Convert.ToDouble(result) : 0;
            }
            catch (Exception ex)
            {
                Logger.Error($"[STATS] Failed to get time: {ex.Message}");
                return 0;
            }
        }

        public List<TrackStat> GetTopTracks(int count = 10)
        {
            var results = new List<TrackStat>();

            if (!_isInitialized)
                return results;

            try
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = @"
                            SELECT FileHash, TotalSeconds 
                            FROM ListeningStats 
                            ORDER BY TotalSeconds DESC 
                            LIMIT @count;";
                command.Parameters.AddWithValue("@count", count);

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    results.Add(new TrackStat
                    {
                        FileHash = reader.GetString(0),
                        TotalSeconds = reader.GetDouble(1)
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[STATS] Failed to get top tracks: {ex.Message}");
            }

            return results;
        }
        #endregion

        #region VolumeDB
        public async Task UpdateVolAsync(Song s, CancellationToken cancellationToken = default)
        {
            if (!_isInitialized || string.IsNullOrEmpty(s.FileHash) || s.DefaultVolume <= 0)
            {
                Logger.Log($"[VOLUME-DB] Start Func | isInit: {_isInitialized} | fh: {s.FileHash} | data: {s.DefaultVolume}");
                return;
            }

            Logger.Log($"[VOLUME-DB] Start-AsyncTask");
            try
            {
                Logger.Log($"[VOLUME-DB] Try");

                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO Volumes (Title, FileHash, Volume)
                    VALUES (@title, @hash, @data)
                    ON CONFLICT(FileHash) DO UPDATE
                        SET Volume = @data, Title = @title;";

                command.Parameters.AddWithValue("@hash", s.FileHash);
                command.Parameters.AddWithValue("@data", s.DefaultVolume);
                command.Parameters.AddWithValue("@title", s.Title);

                int rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);

                Logger.Log($"[VOLUME-DB] exec: {rowsAffected}: |{s.FileHash}|{s.DefaultVolume}|{s.CuttedTitle(10)}|");
            }
            catch (OperationCanceledException)
            {
                Logger.Log("[VOLUME-DB] Operation was canceled by debounce.");
            }
            catch (Exception ex)
            {
                Logger.Error($"[VOLUME-DB] Failed to save volume: {ex.Message}");
                // В реальном приложении здесь лучше использовать ILogger 
                // или пробрасывать исключение дальше, а не просто глотать его.
            }
        }
        public int GetVol(Song s)
        {
            if (!_isInitialized || string.IsNullOrEmpty(s.FileHash))
                return 100;

            try
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = "SELECT Volume FROM Volumes WHERE FileHash = @hash;";
                command.Parameters.AddWithValue("@hash", s.FileHash);

                var result = command.ExecuteScalar();
                return result != null ? Convert.ToInt32(result) : 100;
            }
            catch (Exception ex)
            {
                Logger.Error($"[VOLUME-DB] Failed to get volume: {ex.Message}");
                return 100;
            }
        }
        public int GetVol(string filehash)
        {
            if (!_isInitialized || string.IsNullOrEmpty(filehash))
                return 100;

            try
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = "SELECT Volume FROM Volumes WHERE FileHash = @hash;";
                command.Parameters.AddWithValue("@hash", filehash);

                var result = command.ExecuteScalar();
                return result != null ? Convert.ToInt32(result) : 100;
            }
            catch (Exception ex)
            {
                Logger.Error($"[VOLUME-DB] Failed to get volume: {ex.Message}");
                return 100;
            }
        }
        #endregion

        #region Constructor
        public DBService()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dbPath = Path.Combine(appData, "Trackora", "Data", "data.db");
            _connectionString = $"Data Source={dbPath};";

            Debug.WriteLine($"[STATS] DB path: {dbPath}");
            Debug.WriteLine($"[STATS] DB exists before init: {File.Exists(dbPath)}");

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(dbPath));
                InitializeDatabase();
                _isInitialized = true;
                Debug.WriteLine($"[STATS] DB exists after init: {File.Exists(dbPath)}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[STATS] Failed to initialize database: {ex.Message}");
                _isInitialized = false;
            }
        }
        #endregion
    }
}