namespace Tunvix.Services
{
    public class FallbackAudioPlayerService : IAudioPlayerService
    {
        public event EventHandler<AudioPlaybackStateChangedEventArgs>? PlaybackStateChanged;

        public event EventHandler? PlaybackEnded;

        public bool IsPlaying { get; private set; }

        public TimeSpan Position { get; private set; }

        public TimeSpan Duration { get; private set; }

        public Task PlayAsync(string sourceUri, CancellationToken cancellationToken = default)
        {
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
            PlaybackStateChanged?.Invoke(
                this,
                new AudioPlaybackStateChangedEventArgs(IsPlaying, Position, Duration));
            return Task.CompletedTask;
        }
    }
}
