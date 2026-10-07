using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Threading.Tasks;
using WpfApp14.Models;

namespace WpfApp14.Utils
{
    public class ListeningStatsService
    {
        private readonly string _connectionString;
        private bool _isInitialized = false;
        private readonly object _lock = new object();

        public ListeningStatsService()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dbPath = Path.Combine(appData, "Trackora", "Stats", "stats.db");
            _connectionString = $"Data Source={dbPath};Version=3;Journal Mode=WAL;";

            Logger.Log($"[STATS] DB path: {dbPath}");
            Logger.Log($"[STATS] DB exists before init: {File.Exists(dbPath)}");

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(dbPath));
                InitializeDatabase();
                _isInitialized = true;
                Logger.Log($"[STATS] DB exists after init: {File.Exists(dbPath)}");
            }
            catch (Exception ex)
            {
                Logger.Error($"[STATS] Failed to initialize database: {ex.Message}");
                _isInitialized = false;
            }
        }

        private void InitializeDatabase()
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        CREATE TABLE IF NOT EXISTS ListeningStats (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            FileHash TEXT NOT NULL UNIQUE,
                            TotalSeconds REAL NOT NULL DEFAULT 0
                        );";
                    command.ExecuteNonQuery();
                }
            }
        }

        public void AddListeningTime(string fileHash, double seconds)
        {
            if (!_isInitialized || string.IsNullOrEmpty(fileHash) || seconds <= 0)
                return;

            Task.Run(() =>
            {
                try
                {
                    using (var connection = new SQLiteConnection(_connectionString))
                    {
                        connection.Open();

                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = @"
                                INSERT INTO ListeningStats (FileHash, TotalSeconds)
                                VALUES (@hash, @seconds)
                                ON CONFLICT(FileHash) DO UPDATE 
                                    SET TotalSeconds = TotalSeconds + @seconds;";
                            command.Parameters.AddWithValue("@hash", fileHash);
                            command.Parameters.AddWithValue("@seconds", seconds);
                            command.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"[STATS] Failed to save listening time: {ex.Message}");
                }
            });
        }

        public double GetListeningTime(string fileHash)
        {
            if (!_isInitialized || string.IsNullOrEmpty(fileHash))
                return 0;

            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT TotalSeconds FROM ListeningStats WHERE FileHash = @hash;";
                        command.Parameters.AddWithValue("@hash", fileHash);

                        var result = command.ExecuteScalar();
                        return result != null ? Convert.ToDouble(result) : 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[STATS] Failed to get listening time: {ex.Message}");
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
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            SELECT FileHash, TotalSeconds 
                            FROM ListeningStats 
                            ORDER BY TotalSeconds DESC 
                            LIMIT @count;";
                        command.Parameters.AddWithValue("@count", count);

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                results.Add(new TrackStat
                                {
                                    FileHash = reader.GetString(0),
                                    TotalSeconds = reader.GetDouble(1)
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[STATS] Failed to get top tracks: {ex.Message}");
            }

            return results;
        }
    }
}