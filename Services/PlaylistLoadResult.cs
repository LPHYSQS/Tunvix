using Tunvix.Models;

namespace Tunvix.Services
{
    public sealed class PlaylistLoadResult
    {
        public PlaylistLoadResult(
            IReadOnlyList<AudioTrackRecord> tracks,
            int addedCount,
            int duplicateSkippedCount,
            int removedSkippedCount,
            bool wasCancelled,
            PlaylistLoadStrategy strategy)
        {
            Tracks = tracks;
            AddedCount = addedCount;
            DuplicateSkippedCount = duplicateSkippedCount;
            RemovedSkippedCount = removedSkippedCount;
            WasCancelled = wasCancelled;
            Strategy = strategy;
        }

        public IReadOnlyList<AudioTrackRecord> Tracks { get; }

        public int AddedCount { get; }

        public int DuplicateSkippedCount { get; }

        public int RemovedSkippedCount { get; }

        public bool WasCancelled { get; }

        public PlaylistLoadStrategy Strategy { get; }
    }
}
