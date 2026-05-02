namespace Tunvix.Services
{
    public interface IPlaybackStateService
    {
        PlaybackStateSnapshot? Load();

        void Save(PlaybackStateSnapshot snapshot);

        void Clear();

        bool IsTrackAvailable(string sourceUri, string sourcePath);
    }
}
