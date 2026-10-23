using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Trackora.Core.Models;
using Trackora.Core.Utils;

namespace Trackora.Android.Services;

public class DBService
{
    #region Private
    private readonly string _connectionString;
    private bool _isInitialized = false;
    #endregion

    public DBService(string databasePath)
    {
        _connectionString = $"Data Source={databasePath}";
        try
        {
            InitializeDatabase();
            _isInitialized = true;
            Logger.Log("[DATA DB] SQLite database initialized successfully on Android.");
        }
        catch (Exception ex)
        {
            Logger.Error($"[DATA DB] Critical error initializing database: {ex.Message}");
        }
    }

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
        if (!_isInitialized || string.IsNullOrEmpty(fileHash) || seconds <= 0)
        {
            Debug.WriteLine($"[ConnSQL] Start Func | isInit: {_isInitialized} | fh: {fileHash} | sec: {seconds}");
            return;
        }

        Task.Run(() =>
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = @"
                            INSERT INTO ListeningStats (Title, FileHash, TotalSeconds)
                            VALUES (@title, @hash, @seconds)
                            ON CONFLICT(FileHash) DO UPDATE
                                SET TotalSeconds = TotalSeconds + @seconds, Title = @title;";
                command.Parameters.AddWithValue("@hash", fileHash);
                command.Parameters.AddWithValue("@seconds", seconds);
                command.Parameters.AddWithValue("@title", s.Title);
                command.ExecuteNonQuery();
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
        if (!_isInitialized) return results;

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
            return;
        }

        try
        {
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

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Logger.Error($"[VOLUME-DB] Failed to save volume: {ex.Message}");
        }
    }

    public int GetVol(Song s) => GetVol(s.FileHash);

    // Дописываем и закрываем пропущенный метод чтения громкости
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
}
