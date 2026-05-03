namespace Tunvix.Services
{
    public interface IAudioPlayerService
    {
        event EventHandler<AudioPlaybackStateChangedEventArgs>? PlaybackStateChanged;

        event EventHandler? PlaybackEnded;

        string? CurrentTrackKey { get; }

        bool IsPlaying { get; }

        TimeSpan Position { get; }

        TimeSpan Duration { get; }

        Task LoadAsync(string trackKey, string sourceUri, CancellationToken cancellationToken = default);

        Task PlayAsync(string trackKey, string sourceUri, CancellationToken cancellationToken = default);

        Task PauseAsync(CancellationToken cancellationToken = default);

        Task ResumeAsync(CancellationToken cancellationToken = default);

        Task SeekAsync(TimeSpan position, CancellationToken cancellationToken = default);

        Task StopAsync(CancellationToken cancellationToken = default);
    }
}
