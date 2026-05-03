using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace Tunvix.Data
{
    public class RemovedSongRepository
    {
        private bool _hasBeenInitialized;
        private readonly ILogger _logger;

        public RemovedSongRepository(ILogger<RemovedSongRepository> logger)
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
                CREATE TABLE IF NOT EXISTS RemovedSong (
                    TrackKey TEXT PRIMARY KEY,
                    SourceUri TEXT NOT NULL,
                    SourcePath TEXT NOT NULL,
                    RemovedAtUtc TEXT NOT NULL
                );";
                await createTableCmd.ExecuteNonQueryAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error creating RemovedSong table");
                throw;
            }

            _hasBeenInitialized = true;
        }

        public async Task<HashSet<string>> ListTrackKeysAsync()
        {
            await Init();
            await using var connection = new SqliteConnection(Constants.DatabasePath);
            await connection.OpenAsync();

            var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = "SELECT TrackKey FROM RemovedSong;";

            var trackKeys = new HashSet<string>(StringComparer.Ordinal);
            await using var reader = await selectCmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                if (!reader.IsDBNull(0))
                {
                    trackKeys.Add(reader.GetString(0));
                }
            }

            return trackKeys;
        }

        public async Task AddOrUpdateAsync(string trackKey, string sourceUri, string sourcePath)
        {
            await Init();
            await using var connection = new SqliteConnection(Constants.DatabasePath);
            await connection.OpenAsync();

            var saveCmd = connection.CreateCommand();
            saveCmd.CommandText = @"
                INSERT INTO RemovedSong (
                    TrackKey,
                    SourceUri,
                    SourcePath,
                    RemovedAtUtc)
                VALUES (
                    @TrackKey,
                    @SourceUri,
                    @SourcePath,
                    @RemovedAtUtc)
                ON CONFLICT(TrackKey) DO UPDATE SET
                    SourceUri = excluded.SourceUri,
                    SourcePath = excluded.SourcePath,
                    RemovedAtUtc = excluded.RemovedAtUtc;";

            saveCmd.Parameters.AddWithValue("@TrackKey", trackKey);
            saveCmd.Parameters.AddWithValue("@SourceUri", sourceUri ?? string.Empty);
            saveCmd.Parameters.AddWithValue("@SourcePath", sourcePath ?? string.Empty);
            saveCmd.Parameters.AddWithValue("@RemovedAtUtc", DateTime.UtcNow.ToString("O"));

            await saveCmd.ExecuteNonQueryAsync();
        }
    }
}
