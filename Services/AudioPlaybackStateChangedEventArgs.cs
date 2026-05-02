namespace Tunvix.Services
{
    public sealed class AudioPlaybackStateChangedEventArgs : EventArgs
    {
        public AudioPlaybackStateChangedEventArgs(
            bool isPlaying,
            TimeSpan position,
            TimeSpan duration)
        {
            IsPlaying = isPlaying;
            Position = position;
            Duration = duration;
        }

        public bool IsPlaying { get; }

        public TimeSpan Position { get; }

        public TimeSpan Duration { get; }
    }
}
