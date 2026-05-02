namespace Tunvix.Services
{
    public sealed class PlaybackStateSnapshot
    {
        public PlaybackStateSnapshot(
            string trackSourceUri,
            string trackSourcePath,
            long positionMilliseconds,
            bool shouldResumePlayback)
        {
            TrackSourceUri = trackSourceUri;
            TrackSourcePath = trackSourcePath;
            PositionMilliseconds = positionMilliseconds;
            ShouldResumePlayback = shouldResumePlayback;
        }

        public string TrackSourceUri { get; }

        public string TrackSourcePath { get; }

        public long PositionMilliseconds { get; }

        public bool ShouldResumePlayback { get; }
    }
}
