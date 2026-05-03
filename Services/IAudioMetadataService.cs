namespace Tunvix.Services
{
    public interface IAudioMetadataService
    {
        Task<ImageSource?> GetArtworkAsync(
            int songId,
            string sourceUri,
            string sourcePath,
            string mimeType,
            CancellationToken cancellationToken = default);

        void RemoveFromCache(int songId);

        void TrimCache(IReadOnlyCollection<int> songIds);
    }
}
