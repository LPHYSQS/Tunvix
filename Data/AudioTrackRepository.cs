using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Tunvix.Models;

namespace Tunvix.Data
{
    public class AudioTrackRepository
    {
        private bool _hasBeenInitialized;
        private readonly ILogger _logger;

        public AudioTrackRepository(ILogger<AudioTrackRepository> logger)
        {
            _logger = logger;
        }

        private async Task Init()
        {
            if (_hasBeenInitialized)
            {
                return;
            }

            await using var connection = new SqliteConnection(Constants.DatabasePath);
            await connection.OpenAsync();

            try
            {
                var createTableCmd = connection.CreateCommand();
                createTableCmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS AudioTrack (
                    ID INTEGER PRIMARY KEY AUTOINCREMENT,
                    TrackKey TEXT NULL,
                    Title TEXT NOT NULL,
                    Artist TEXT NOT NULL,
                    DurationMilliseconds INTEGER NOT NULL,
                    SourceUri TEXT NOT NULL UNIQUE,
                    SourcePath TEXT NOT NULL,
                    MimeType TEXT NOT NULL,
                    ImportScope TEXT NOT NULL,
                    FolderTreeUri TEXT NULL
                );";
                await createTableCmd.ExecuteNonQueryAsync();

                await EnsureTrackKeyColumnAsync(connection);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error creating AudioTrack table");
                throw;
            }

            _hasBeenInitialized = true;
        }

        public async Task<List<AudioTrackRecord>> ListAsync()
        {
            await Init();
            await using var connection = new SqliteConnection(Constants.DatabasePath);
            await connection.OpenAsync();

            var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = @"
                SELECT ID, TrackKey, Title, Artist, DurationMilliseconds, SourceUri, SourcePath, MimeType, ImportScope, FolderTreeUri
                FROM AudioTrack
                ORDER BY Title COLLATE NOCASE, Artist COLLATE NOCASE;";

            var tracks = new List<AudioTrackRecord>();
            await using var reader = await selectCmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                tracks.Add(new AudioTrackRecord
                {
                    ID = reader.GetInt32(0),
                    TrackKey = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                    Title = reader.GetString(2),
                    Artist = reader.GetString(3),
                    DurationMilliseconds = reader.GetInt64(4),
                    SourceUri = reader.GetString(5),
                    SourcePath = reader.GetString(6),
                    MimeType = reader.GetString(7),
                    ImportScope = reader.GetString(8),
                    FolderTreeUri = reader.IsDBNull(9) ? null : reader.GetString(9)
                });
            }

            return tracks;
        }

        public async Task InsertRangeAsync(IReadOnlyCollection<AudioTrackRecord> tracks)
        {
            if (tracks.Count == 0)
            {
                return;
            }

            await Init();
            await using var connection = new SqliteConnection(Constants.DatabasePath);
            await connection.OpenAsync();
            await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync();

            try
            {
                foreach (var track in tracks)
                {
                    var insertCmd = connection.CreateCommand();
                    insertCmd.Transaction = transaction;
                    insertCmd.CommandText = @"
                        INSERT INTO AudioTrack (
                            TrackKey,
                            Title,
                            Artist,
                            DurationMilliseconds,
                            SourceUri,
                            SourcePath,
                            MimeType,
                            ImportScope,
                            FolderTreeUri)
                        VALUES (
                            @TrackKey,
                            @Title,
                            @Artist,
                            @DurationMilliseconds,
                            @SourceUri,
                            @SourcePath,
                            @MimeType,
                            @ImportScope,
                            @FolderTreeUri);";

                    insertCmd.Parameters.AddWithValue("@TrackKey", track.TrackKey);
                    insertCmd.Parameters.AddWithValue("@Title", track.Title);
                    insertCmd.Parameters.AddWithValue("@Artist", track.Artist);
                    insertCmd.Parameters.AddWithValue("@DurationMilliseconds", track.DurationMilliseconds);
                    insertCmd.Parameters.AddWithValue("@SourceUri", track.SourceUri);
                    insertCmd.Parameters.AddWithValue("@SourcePath", track.SourcePath);
                    insertCmd.Parameters.AddWithValue("@MimeType", track.MimeType);
                    insertCmd.Parameters.AddWithValue("@ImportScope", track.ImportScope);
                    insertCmd.Parameters.AddWithValue("@FolderTreeUri", (object?)track.FolderTreeUri ?? DBNull.Value);

                    await insertCmd.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error inserting AudioTrack rows");
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task ReplaceAllAsync(IReadOnlyCollection<AudioTrackRecord> tracks)
        {
            await Init();
            await using var connection = new SqliteConnection(Constants.DatabasePath);
            await connection.OpenAsync();
            await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync();

            try
            {
                var deleteCmd = connection.CreateCommand();
                deleteCmd.Transaction = transaction;
                deleteCmd.CommandText = "DELETE FROM AudioTrack";
                await deleteCmd.ExecuteNonQueryAsync();

                foreach (var track in tracks)
                {
                    var insertCmd = connection.CreateCommand();
                    insertCmd.Transaction = transaction;
                    insertCmd.CommandText = @"
                        INSERT INTO AudioTrack (
                            TrackKey,
                            Title,
                            Artist,
                            DurationMilliseconds,
                            SourceUri,
                            SourcePath,
                            MimeType,
                            ImportScope,
                            FolderTreeUri)
                        VALUES (
                            @TrackKey,
                            @Title,
                            @Artist,
                            @DurationMilliseconds,
                            @SourceUri,
                            @SourcePath,
                            @MimeType,
                            @ImportScope,
                            @FolderTreeUri);";

                    insertCmd.Parameters.AddWithValue("@TrackKey", track.TrackKey);
                    insertCmd.Parameters.AddWithValue("@Title", track.Title);
                    insertCmd.Parameters.AddWithValue("@Artist", track.Artist);
                    insertCmd.Parameters.AddWithValue("@DurationMilliseconds", track.DurationMilliseconds);
                    insertCmd.Parameters.AddWithValue("@SourceUri", track.SourceUri);
                    insertCmd.Parameters.AddWithValue("@SourcePath", track.SourcePath);
                    insertCmd.Parameters.AddWithValue("@MimeType", track.MimeType);
                    insertCmd.Parameters.AddWithValue("@ImportScope", track.ImportScope);
                    insertCmd.Parameters.AddWithValue("@FolderTreeUri", (object?)track.FolderTreeUri ?? DBNull.Value);

                    await insertCmd.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error replacing AudioTrack table");
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<int> DeleteByTrackKeyAsync(string trackKey)
        {
            await Init();
            await using var connection = new SqliteConnection(Constants.DatabasePath);
            await connection.OpenAsync();

            var deleteCmd = connection.CreateCommand();
            deleteCmd.CommandText = "DELETE FROM AudioTrack WHERE TrackKey = @TrackKey;";
            deleteCmd.Parameters.AddWithValue("@TrackKey", trackKey);

            return await deleteCmd.ExecuteNonQueryAsync();
        }

        private static async Task EnsureTrackKeyColumnAsync(SqliteConnection connection)
        {
            var checkColumnCmd = connection.CreateCommand();
            checkColumnCmd.CommandText = "PRAGMA table_info(AudioTrack);";

            var hasTrackKeyColumn = false;
            await using (var reader = await checkColumnCmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    if (string.Equals(reader.GetString(1), "TrackKey", StringComparison.OrdinalIgnoreCase))
                    {
                        hasTrackKeyColumn = true;
                        break;
                    }
                }
            }

            if (hasTrackKeyColumn)
            {
                return;
            }

            var alterCmd = connection.CreateCommand();
            alterCmd.CommandText = "ALTER TABLE AudioTrack ADD COLUMN TrackKey TEXT NULL;";
            await alterCmd.ExecuteNonQueryAsync();
        }
    }
}
