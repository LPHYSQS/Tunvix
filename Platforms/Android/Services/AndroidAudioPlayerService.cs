using Android.Net;
using AndroidX.Media3.Common;
using AndroidX.Media3.ExoPlayer;
using Microsoft.Maui.ApplicationModel;
using Tunvix.Services;
using AndroidUri = Android.Net.Uri;

namespace Tunvix.Platforms.Android.Services
{
    public sealed class AndroidAudioPlayerService : IAudioPlayerService, IDisposable
    {
        private const int PlaybackStateEnded = 4;
        private readonly IExoPlayer _player;
        private CancellationTokenSource? _monitorCancellationTokenSource;
        private bool _playbackEndedRaised;

        public AndroidAudioPlayerService()
        {
            var context = global::Android.App.Application.Context;
            _player = new ExoPlayerBuilder(context).Build()
                ?? throw new InvalidOperationException("Unable to create the Android audio player.");
        }

        public event EventHandler<AudioPlaybackStateChangedEventArgs>? PlaybackStateChanged;

        public event EventHandler? PlaybackEnded;

        public bool IsPlaying => _player.IsPlaying;

        public TimeSpan Position => TimeSpan.FromMilliseconds(Math.Max(_player.CurrentPosition, 0));

        public TimeSpan Duration => _player.Duration > 0
            ? TimeSpan.FromMilliseconds(_player.Duration)
            : TimeSpan.Zero;

        public async Task LoadAsync(string sourceUri, CancellationToken cancellationToken = default)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                _player.SetMediaItem(MediaItem.FromUri(AndroidUri.Parse(sourceUri)));
                _player.Prepare();
                _player.Pause();
                _playbackEndedRaised = false;
                RaisePlaybackStateChanged();
            });
        }

        public async Task PlayAsync(string sourceUri, CancellationToken cancellationToken = default)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                _player.SetMediaItem(MediaItem.FromUri(AndroidUri.Parse(sourceUri)));
                _player.Prepare();
                _player.Play();
                _playbackEndedRaised = false;
                EnsureMonitorLoop();
                RaisePlaybackStateChanged();
            });
        }

        public async Task PauseAsync(CancellationToken cancellationToken = default)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                _player.Pause();
                RaisePlaybackStateChanged();
            });
        }

        public async Task ResumeAsync(CancellationToken cancellationToken = default)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                _player.Play();
                _playbackEndedRaised = false;
                EnsureMonitorLoop();
                RaisePlaybackStateChanged();
            });
        }

        public async Task SeekAsync(TimeSpan position, CancellationToken cancellationToken = default)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                _player.SeekTo((long)Math.Max(position.TotalMilliseconds, 0));
                RaisePlaybackStateChanged();
            });
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                _player.Stop();
                _playbackEndedRaised = false;
                RaisePlaybackStateChanged();
            });
        }

        public void Dispose()
        {
            _monitorCancellationTokenSource?.Cancel();
            _monitorCancellationTokenSource?.Dispose();
            _player.Release();
        }

        private void EnsureMonitorLoop()
        {
            if (_monitorCancellationTokenSource is not null)
            {
                return;
            }

            _monitorCancellationTokenSource = new CancellationTokenSource();
            _ = MonitorPlaybackAsync(_monitorCancellationTokenSource.Token);
        }

        private async Task MonitorPlaybackAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(350, cancellationToken);

                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        RaisePlaybackStateChanged();

                        if (_player.PlaybackState == PlaybackStateEnded && !_playbackEndedRaised)
                        {
                            _playbackEndedRaised = true;
                            PlaybackEnded?.Invoke(this, EventArgs.Empty);
                        }
                    });
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void RaisePlaybackStateChanged() =>
            PlaybackStateChanged?.Invoke(
                this,
                new AudioPlaybackStateChangedEventArgs(IsPlaying, Position, Duration));
    }
}
