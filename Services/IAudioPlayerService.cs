namespace Tunvix.Services
{
    public interface IAudioPlayerService
    {
        event EventHandler<AudioPlaybackStateChangedEventArgs>? PlaybackStateChanged;

        event EventHandler? PlaybackEnded;

        bool IsPlaying { get; }

        TimeSpan Position { get; }

        TimeSpan Duration { get; }

        Task PlayAsync(string sourceUri, CancellationToken cancellationToken = default);

        Task PauseAsync(CancellationToken cancellationToken = default);

        Task ResumeAsync(CancellationToken cancellationToken = default);

        Task SeekAsync(TimeSpan position, CancellationToken cancellationToken = default);

        Task StopAsync(CancellationToken cancellationToken = default);
    }
}
