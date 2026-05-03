namespace Tunvix.Services
{
    public class FallbackAudioPlayerService : IAudioPlayerService
    {
        public event EventHandler<AudioPlaybackStateChangedEventArgs>? PlaybackStateChanged;

        public event EventHandler? PlaybackEnded
        {
            add { }
            remove { }
        }

        public bool IsPlaying { get; private set; }

        public string? CurrentTrackKey { get; private set; }

        public TimeSpan Position { get; private set; }

        public TimeSpan Duration { get; private set; }

        public Task LoadAsync(string trackKey, string sourceUri, CancellationToken cancellationToken = default)
        {
            CurrentTrackKey = NormalizeTrackKey(trackKey);
            IsPlaying = false;
            Position = TimeSpan.Zero;
            PlaybackStateChanged?.Invoke(
                this,
                new AudioPlaybackStateChangedEventArgs(IsPlaying, Position, Duration));
            return Task.CompletedTask;
        }

        public Task PlayAsync(string trackKey, string sourceUri, CancellationToken cancellationToken = default)
        {
            CurrentTrackKey = NormalizeTrackKey(trackKey);
            IsPlaying = true;
            PlaybackStateChanged?.Invoke(
                this,
                new AudioPlaybackStateChangedEventArgs(IsPlaying, Position, Duration));
            return Task.CompletedTask;
        }

        public Task PauseAsync(CancellationToken cancellationToken = default)
        {
            IsPlaying = false;
            PlaybackStateChanged?.Invoke(
                this,
                new AudioPlaybackStateChangedEventArgs(IsPlaying, Position, Duration));
            return Task.CompletedTask;
        }

        public Task ResumeAsync(CancellationToken cancellationToken = default)
        {
            IsPlaying = true;
            PlaybackStateChanged?.Invoke(
                this,
                new AudioPlaybackStateChangedEventArgs(IsPlaying, Position, Duration));
            return Task.CompletedTask;
        }

        public Task SeekAsync(TimeSpan position, CancellationToken cancellationToken = default)
        {
            Position = position;
            PlaybackStateChanged?.Invoke(
                this,
                new AudioPlaybackStateChangedEventArgs(IsPlaying, Position, Duration));
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken = default)
        {
            IsPlaying = false;
            Position = TimeSpan.Zero;
            CurrentTrackKey = null;
            PlaybackStateChanged?.Invoke(
                this,
                new AudioPlaybackStateChangedEventArgs(IsPlaying, Position, Duration));
            return Task.CompletedTask;
        }

        private static string? NormalizeTrackKey(string trackKey) =>
            string.IsNullOrWhiteSpace(trackKey)
                ? null
                : trackKey;
    }
}
