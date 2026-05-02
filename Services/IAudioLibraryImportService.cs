namespace Tunvix.Services
{
    public interface IAudioLibraryImportService
    {
        Task<AudioImportResult> ImportAllDeviceAudioAsync(
            IProgress<AudioImportProgress>? progress = null,
            CancellationToken cancellationToken = default);

        Task<AudioImportResult> ImportFolderAudioAsync(
            IProgress<AudioImportProgress>? progress = null,
            CancellationToken cancellationToken = default);
    }
}
