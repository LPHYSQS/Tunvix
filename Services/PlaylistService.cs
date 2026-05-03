using Tunvix.Data;
using Tunvix.Models;

namespace Tunvix.Services
{
    public sealed class PlaylistService : IPlaylistService
    {
        private readonly AudioTrackRepository _audioTrackRepository;
        private readonly RemovedSongRepository _removedSongRepository;
        private readonly IAudioLibraryImportService _audioLibraryImportService;

        public PlaylistService(
            AudioTrackRepository audioTrackRepository,
            RemovedSongRepository removedSongRepository,
            IAudioLibraryImportService audioLibraryImportService)
        {
            _audioTrackRepository = audioTrackRepository;
            _removedSongRepository = removedSongRepository;
            _audioLibraryImportService = audioLibraryImportService;
        }

        public async Task<IReadOnlyList<AudioTrackRecord>> ListAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var tracks = await _audioTrackRepository.ListAsync();
            return await NormalizeStoredTracksAsync(tracks, cancellationToken);
        }

        public Task<PlaylistLoadResult> LoadAllDeviceAudioAsync(
            PlaylistLoadStrategy strategy,
            IProgress<AudioImportProgress>? progress = null,
            CancellationToken cancellationToken = default) =>
            LoadAsync(
                strategy,
                (loadProgress, token) => _audioLibraryImportService.ImportAllDeviceAudioAsync(loadProgress, token),
                progress,
                cancellationToken);

        public Task<PlaylistLoadResult> LoadFolderAudioAsync(
            PlaylistLoadStrategy strategy,
            IProgress<AudioImportProgress>? progress = null,
            CancellationToken cancellationToken = default) =>
            LoadAsync(
                strategy,
                (loadProgress, token) => _audioLibraryImportService.ImportFolderAudioAsync(loadProgress, token),
                progress,
                cancellationToken);

        public async Task<bool> RemoveTrackAsync(
            string trackKey,
            string sourceUri,
            string sourcePath,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var normalizedTrackKey = AudioTrackKeyFactory.Ensure(
                trackKey,
                title: null,
                artist: null,
                durationMilliseconds: 0,
                sourcePath,
                sourceUri);

            await _removedSongRepository.AddOrUpdateAsync(normalizedTrackKey, sourceUri, sourcePath);

            var deletedRows = await _audioTrackRepository.DeleteByTrackKeyAsync(normalizedTrackKey);
            return deletedRows > 0;
        }

        private async Task<PlaylistLoadResult> LoadAsync(
            PlaylistLoadStrategy strategy,
            Func<IProgress<AudioImportProgress>?, CancellationToken, Task<AudioImportResult>> loadTracksAsync,
            IProgress<AudioImportProgress>? progress,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var existingTracks = await ListAsync(cancellationToken);
            var importResult = await loadTracksAsync(progress, cancellationToken);

            if (importResult.WasCancelled)
            {
                return new PlaylistLoadResult(
                    existingTracks,
                    addedCount: 0,
                    duplicateSkippedCount: 0,
                    removedSkippedCount: 0,
                    wasCancelled: true,
                    strategy);
            }

            var removedTrackKeys = await _removedSongRepository.ListTrackKeysAsync();
            var knownTrackKeys = strategy == PlaylistLoadStrategy.Incremental
                ? new HashSet<string>(existingTracks.Select(GetTrackKey), StringComparer.Ordinal)
                : new HashSet<string>(StringComparer.Ordinal);

            var deduplicatedTracks = new List<AudioTrackRecord>(importResult.Tracks.Count);
            var duplicateSkippedCount = 0;
            var removedSkippedCount = 0;

            foreach (var track in importResult.Tracks)
            {
                cancellationToken.ThrowIfCancellationRequested();

                track.TrackKey = GetTrackKey(track);
                if (removedTrackKeys.Contains(track.TrackKey))
                {
                    removedSkippedCount++;
                    continue;
                }

                if (!knownTrackKeys.Add(track.TrackKey))
                {
                    duplicateSkippedCount++;
                    continue;
                }

                deduplicatedTracks.Add(track);
            }

            if (strategy == PlaylistLoadStrategy.ReplaceAll)
            {
                await _audioTrackRepository.ReplaceAllAsync(deduplicatedTracks);
            }
            else
            {
                await _audioTrackRepository.InsertRangeAsync(deduplicatedTracks);
            }

            var normalizedFinalTracks = await ListAsync(cancellationToken);

            return new PlaylistLoadResult(
                normalizedFinalTracks,
                addedCount: deduplicatedTracks.Count,
                duplicateSkippedCount,
                removedSkippedCount,
                wasCancelled: false,
                strategy);
        }

        private async Task<IReadOnlyList<AudioTrackRecord>> NormalizeStoredTracksAsync(
            IReadOnlyList<AudioTrackRecord> tracks,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (tracks.Count == 0)
            {
                return tracks;
            }

            var removedTrackKeys = await _removedSongRepository.ListTrackKeysAsync();
            var seenTrackKeys = new HashSet<string>(StringComparer.Ordinal);
            var normalizedTracks = new List<AudioTrackRecord>(tracks.Count);
            var hasChanges = false;

            foreach (var track in tracks)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var trackKey = GetTrackKey(track);
                if (!string.Equals(track.TrackKey, trackKey, StringComparison.Ordinal))
                {
                    track.TrackKey = trackKey;
                    hasChanges = true;
                }

                if (removedTrackKeys.Contains(trackKey))
                {
                    hasChanges = true;
                    continue;
                }

                if (!seenTrackKeys.Add(trackKey))
                {
                    hasChanges = true;
                    continue;
                }

                normalizedTracks.Add(track);
            }

            if (hasChanges)
            {
                await _audioTrackRepository.ReplaceAllAsync(normalizedTracks);
                return await _audioTrackRepository.ListAsync();
            }

            return normalizedTracks;
        }

        private static string GetTrackKey(AudioTrackRecord track)
        {
            return AudioTrackKeyFactory.Ensure(
                track.TrackKey,
                track.Title,
                track.Artist,
                track.DurationMilliseconds,
                track.SourcePath,
                track.SourceUri);
        }
    }
}
