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
                SELECT ID, Title, Artist, DurationMilliseconds, SourceUri, SourcePath, MimeType, ImportScope, FolderTreeUri
                FROM AudioTrack
                ORDER BY Title COLLATE NOCASE, Artist COLLATE NOCASE;";

            var tracks = new List<AudioTrackRecord>();
            await using var reader = await selectCmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                tracks.Add(new AudioTrackRecord
                {
                    ID = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Artist = reader.GetString(2),
                    DurationMilliseconds = reader.GetInt64(3),
                    SourceUri = reader.GetString(4),
                    SourcePath = reader.GetString(5),
                    MimeType = reader.GetString(6),
                    ImportScope = reader.GetString(7),
                    FolderTreeUri = reader.IsDBNull(8) ? null : reader.GetString(8)
                });
            }

            return tracks;
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
                            Title,
                            Artist,
                            DurationMilliseconds,
                            SourceUri,
                            SourcePath,
                            MimeType,
                            ImportScope,
                            FolderTreeUri)
                        VALUES (
                            @Title,
                            @Artist,
                            @DurationMilliseconds,
                            @SourceUri,
                            @SourcePath,
                            @MimeType,
                            @ImportScope,
                            @FolderTreeUri);";

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
    }
}
