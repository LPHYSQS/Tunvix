using Tunvix.Models;

namespace Tunvix.Services
{
    public sealed class AudioImportResult
    {
        public AudioImportResult(
            IReadOnlyList<AudioTrackRecord> tracks,
            bool wasCancelled = false)
        {
            Tracks = tracks;
            WasCancelled = wasCancelled;
        }

        public IReadOnlyList<AudioTrackRecord> Tracks { get; }

        public bool WasCancelled { get; }
    }
}
