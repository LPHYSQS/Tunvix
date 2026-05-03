using Tunvix.Models;

namespace Tunvix.Services
{
    public interface IPlaylistService
    {
        Task<IReadOnlyList<AudioTrackRecord>> ListAsync(CancellationToken cancellationToken = default);

        Task<PlaylistLoadResult> LoadAllDeviceAudioAsync(
            PlaylistLoadStrategy strategy,
            IProgress<AudioImportProgress>? progress = null,
            CancellationToken cancellationToken = default);

        Task<PlaylistLoadResult> LoadFolderAudioAsync(
            PlaylistLoadStrategy strategy,
            IProgress<AudioImportProgress>? progress = null,
            CancellationToken cancellationToken = default);

        Task<bool> RemoveTrackAsync(
            string trackKey,
            string sourceUri,
            string sourcePath,
            CancellationToken cancellationToken = default);
    }
}
