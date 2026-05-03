using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Fonts;
using Microsoft.Maui.Dispatching;
using Tunvix.Data;
using Tunvix.Models;
using Tunvix.Services;

namespace Tunvix.PageModels
{
    public partial class MainPageModel : ObservableObject
    {
        private static readonly string[] AccentPalette =
        {
            "#D96A47",
            "#4D7CFE",
            "#2AA889",
            "#A56CFF",
            "#F2B94B",
            "#F06D8F"
        };

        private readonly ThemeService _themeService;
        private readonly PlaybackModeService _playbackModeService;
        private readonly IPlaylistService _playlistService;
        private readonly IAudioPlayerService _audioPlayerService;
        private readonly IPlaybackStateService _playbackStateService;
        private readonly IErrorHandler _errorHandler;
        private readonly Random _random = new();

        private bool _isInitialized;
        private bool _isUpdatingProgressFromPlayer;
        private bool _isHandlingPlaybackEnded;
        private bool _isRestoringPlaybackState;
        private bool _hasPreparedTrack;
        private long _currentDurationMilliseconds;
        private long _lastPersistedPositionMilliseconds = -1;
        private bool? _lastPersistedIsPlaying;
        private string? _lastPersistedSourceUri;

        [ObservableProperty]
        private ObservableCollection<MusicTrack> _playlist = new();

        [ObservableProperty]
        private MusicTrack? _selectedTrack;

        [ObservableProperty]
        private bool _isPlaying;

        [ObservableProperty]
        private double _playbackProgress;

        [ObservableProperty]
        private bool _isPlaylistDrawerOpen;

        [ObservableProperty]
        private PlaybackMode _playbackMode = PlaybackMode.Sequential;

        [ObservableProperty]
        private bool _isImportOptionsOpen;

        [ObservableProperty]
        private bool _isImportingLibrary;

        [ObservableProperty]
        private double _importProgressValue;

        [ObservableProperty]
        private string _importProgressLabel = "\u51c6\u5907\u5bfc\u5165\u97f3\u9891";

        public MainPageModel(
            ThemeService themeService,
            PlaybackModeService playbackModeService,
            IPlaylistService playlistService,
            IAudioPlayerService audioPlayerService,
            IPlaybackStateService playbackStateService,
            IErrorHandler errorHandler)
        {
            _themeService = themeService;
            _playbackModeService = playbackModeService;
            _playlistService = playlistService;
            _audioPlayerService = audioPlayerService;
            _playbackStateService = playbackStateService;
            _errorHandler = errorHandler;

            PlaybackMode = _playbackModeService.GetStoredMode();

            _audioPlayerService.PlaybackStateChanged += OnPlaybackStateChanged;
            _audioPlayerService.PlaybackEnded += OnPlaybackEnded;
        }

        public event Action<MusicTrack>? LocateCurrentTrackRequested;

        public event Func<PlaylistImportSource, Task<PlaylistLoadStrategy?>>? PlaylistLoadStrategyRequested;

        public event Func<string, Task>? FeedbackRequested;

        public string PlayPauseGlyph => IsPlaying
            ? FluentUI.pause_24_regular
            : FluentUI.play_24_regular;

        public string ThemeButtonDescription => IsDarkTheme
            ? "\u5207\u6362\u5230\u6d45\u8272\u4e3b\u9898"
            : "\u5207\u6362\u5230\u6df1\u8272\u4e3b\u9898";

        public string NowPlayingLabel => !HasTracks
            ? "\u7b49\u5f85\u5bfc\u5165"
            : IsPlaying
                ? "\u6b63\u5728\u64ad\u653e"
                : "\u5df2\u6682\u505c";

        public string PlaylistSummary => Playlist.Count == 0
            ? "\u8fd8\u6ca1\u6709\u5bfc\u5165\u6b4c\u66f2"
            : $"\u5171 {Playlist.Count} \u9996\u6b4c\u66f2";

        public bool IsDarkTheme => _themeService.IsDarkTheme;

        public bool HasTracks => Playlist.Count > 0;

        public bool IsPlaylistEmpty => Playlist.Count == 0;

        public string EmptyPlaylistTitle => "\u8fd8\u6ca1\u6709\u97f3\u9891";

        public string EmptyPlaylistSubtitle => "\u70b9\u51fb\u53f3\u4e0a\u89d2\u52a0\u53f7\uff0c\u4ece\u672c\u673a\u6216\u6307\u5b9a\u6587\u4ef6\u5939\u5bfc\u5165";

        public bool CanLocateCurrentTrack => ResolveCurrentTrackInPlaylist() is not null;

        public string PlaybackModeLabel => PlaybackMode switch
        {
            PlaybackMode.Sequential => "\u987a\u5e8f\u64ad\u653e",
            PlaybackMode.SingleLoop => "\u5355\u66f2\u5faa\u73af",
            PlaybackMode.Shuffle => "\u968f\u673a\u64ad\u653e",
            _ => "\u987a\u5e8f\u64ad\u653e"
        };

        public string PlaybackModeGlyph => PlaybackMode switch
        {
            PlaybackMode.Sequential => FluentUI.arrow_sort_20_regular,
            PlaybackMode.SingleLoop => FluentUI.arrow_repeat_1_20_regular,
            PlaybackMode.Shuffle => FluentUI.arrow_shuffle_20_regular,
            _ => FluentUI.arrow_sort_20_regular
        };

        public string PlaybackModeDescription => $"{PlaybackModeLabel}\uff0c\u70b9\u51fb\u5207\u6362";

        public string CurrentTrackTitle => SelectedTrack?.Title ?? "\u6682\u65e0\u6b4c\u66f2";

        public string CurrentTrackArtist => SelectedTrack?.Artist ?? "\u8bf7\u5148\u5bfc\u5165\u97f3\u9891\u5230\u64ad\u653e\u5217\u8868";

        public string CurrentPlaybackTime =>
            FormatPlaybackTime((int)Math.Round(_audioPlayerService.Position.TotalSeconds));

        public string TotalPlaybackTime =>
            FormatPlaybackTime((int)Math.Round(TimeSpan.FromMilliseconds(_currentDurationMilliseconds).TotalSeconds));

        public async Task InitializeAsync()
        {
            if (_isInitialized)
            {
                return;
            }

            var records = await _playlistService.ListAsync();
            ApplyPlaylist(records, preserveCurrentSelection: false, preservePlaybackState: false);
            await RestorePlaybackStateAsync();
            _isInitialized = true;
        }

        public void PersistPlaybackState(bool force = false)
        {
            if (_isRestoringPlaybackState)
            {
                return;
            }

            if (!_isInitialized && !_hasPreparedTrack)
            {
                return;
            }

            if (!_hasPreparedTrack
                || SelectedTrack is null
                || string.IsNullOrWhiteSpace(SelectedTrack.SourceUri))
            {
                ClearPersistedPlaybackState();
                return;
            }

            var snapshot = new PlaybackStateSnapshot(
                SelectedTrack.SourceUri,
                SelectedTrack.SourcePath,
                GetCurrentPositionMilliseconds(),
                _audioPlayerService.IsPlaying);

            if (!force
                && string.Equals(snapshot.TrackSourceUri, _lastPersistedSourceUri, StringComparison.Ordinal)
                && snapshot.ShouldResumePlayback == _lastPersistedIsPlaying
                && Math.Abs(snapshot.PositionMilliseconds - _lastPersistedPositionMilliseconds) < 1000)
            {
                return;
            }

            _playbackStateService.Save(snapshot);
            _lastPersistedSourceUri = snapshot.TrackSourceUri;
            _lastPersistedPositionMilliseconds = snapshot.PositionMilliseconds;
            _lastPersistedIsPlaying = snapshot.ShouldResumePlayback;
        }

        partial void OnSelectedTrackChanged(MusicTrack? oldValue, MusicTrack? newValue)
        {
            if (oldValue is not null)
            {
                oldValue.IsCurrent = false;
                oldValue.IsPlayingCurrentTrack = false;
            }

            if (newValue is not null)
            {
                newValue.IsCurrent = true;
                newValue.IsPlayingCurrentTrack = IsPlaying;
                _currentDurationMilliseconds = newValue.DurationMilliseconds;
            }
            else
            {
                _currentDurationMilliseconds = 0;
            }

            OnPropertyChanged(nameof(CurrentTrackTitle));
            OnPropertyChanged(nameof(CurrentTrackArtist));
            OnPropertyChanged(nameof(CurrentPlaybackTime));
            OnPropertyChanged(nameof(TotalPlaybackTime));
            OnPropertyChanged(nameof(NowPlayingLabel));
            OnPropertyChanged(nameof(CanLocateCurrentTrack));
            LocateCurrentTrackCommand.NotifyCanExecuteChanged();
        }

        partial void OnIsPlayingChanged(bool value)
        {
            if (SelectedTrack is not null)
            {
                SelectedTrack.IsPlayingCurrentTrack = value;
            }

            OnPropertyChanged(nameof(PlayPauseGlyph));
            OnPropertyChanged(nameof(NowPlayingLabel));
        }

        partial void OnPlaybackProgressChanged(double value)
        {
            OnPropertyChanged(nameof(CurrentPlaybackTime));

            if (_isUpdatingProgressFromPlayer
                || SelectedTrack is null
                || _currentDurationMilliseconds <= 0)
            {
                return;
            }

            _ = SeekToProgressAsync(value);
        }

        partial void OnPlaybackModeChanged(PlaybackMode value)
        {
            _playbackModeService.SaveMode(value);
            OnPropertyChanged(nameof(PlaybackModeLabel));
            OnPropertyChanged(nameof(PlaybackModeGlyph));
            OnPropertyChanged(nameof(PlaybackModeDescription));
        }

        partial void OnIsImportingLibraryChanged(bool value)
        {
            if (!value)
            {
                ImportProgressValue = 0;
            }
        }

        [RelayCommand]
        private async Task TogglePlaybackAsync()
        {
            if (!HasTracks)
            {
                return;
            }

            if (SelectedTrack is null)
            {
                await PlayTrackAsync(Playlist[0]);
                return;
            }

            if (IsPlaying)
            {
                await _audioPlayerService.PauseAsync();
                IsPlaying = false;
                return;
            }

            if (_audioPlayerService.Duration > TimeSpan.Zero
                || _audioPlayerService.Position > TimeSpan.Zero)
            {
                await _audioPlayerService.ResumeAsync();
                IsPlaying = true;
                return;
            }

            await PlayTrackAsync(SelectedTrack);
        }

        [RelayCommand]
        private void OpenPlaylistDrawer() =>
            IsPlaylistDrawerOpen = true;

        [RelayCommand]
        private void ClosePlaylistDrawer()
        {
            IsPlaylistDrawerOpen = false;
            IsImportOptionsOpen = false;
        }

        [RelayCommand]
        private void ToggleTheme()
        {
            _themeService.ToggleTheme();
            OnPropertyChanged(nameof(IsDarkTheme));
            OnPropertyChanged(nameof(ThemeButtonDescription));
        }

        [RelayCommand]
        private async Task SelectTrackAsync(MusicTrack? track)
        {
            if (track is null)
            {
                return;
            }

            await PlayTrackAsync(track);
        }

        [RelayCommand]
        private async Task PreviousTrackAsync() =>
            await MoveSelectionAsync(-1);

        [RelayCommand]
        private async Task NextTrackAsync() =>
            await MoveSelectionAsync(1);

        [RelayCommand(CanExecute = nameof(CanLocateCurrentTrack))]
        private void LocateCurrentTrack()
        {
            var track = ResolveCurrentTrackInPlaylist();
            if (track is null)
            {
                return;
            }

            if (!ReferenceEquals(SelectedTrack, track))
            {
                SelectedTrack = track;
            }

            LocateCurrentTrackRequested?.Invoke(track);
        }

        [RelayCommand]
        private async Task RemoveTrackAsync(MusicTrack? track)
        {
            if (track is null || IsImportingLibrary)
            {
                return;
            }

            try
            {
                var currentIndex = Playlist.IndexOf(track);
                var isRemovingCurrentTrack = SelectedTrack?.TrackKey == track.TrackKey;

                var removed = await _playlistService.RemoveTrackAsync(
                    track.TrackKey,
                    track.SourceUri,
                    track.SourcePath,
                    CancellationToken.None);

                if (!removed)
                {
                    return;
                }

                Playlist.Remove(track);
                var nextTrack = GetFallbackTrackAfterRemoval(currentIndex);
                ReindexPlaylistBadges();

                if (isRemovingCurrentTrack)
                {
                    await _audioPlayerService.StopAsync();
                    _hasPreparedTrack = false;
                    ClearPersistedPlaybackState();
                    _isUpdatingProgressFromPlayer = true;
                    PlaybackProgress = 0;
                    _isUpdatingProgressFromPlayer = false;
                    IsPlaying = false;
                    SelectedTrack = nextTrack;
                }
                else if (SelectedTrack is not null && !Playlist.Contains(SelectedTrack))
                {
                    SelectedTrack = nextTrack;
                }

                if (Playlist.Count == 0)
                {
                    SelectedTrack = null;
                }

                RefreshPlaylistMetadata();
                await RaiseFeedbackAsync($"\u5df2\u79fb\u9664\u300a{track.Title}\u300b");
            }
            catch (Exception ex)
            {
                _errorHandler.HandleError(ex);
            }
        }

        [RelayCommand]
        private void CyclePlaybackMode()
        {
            PlaybackMode = PlaybackMode switch
            {
                PlaybackMode.Sequential => PlaybackMode.SingleLoop,
                PlaybackMode.SingleLoop => PlaybackMode.Shuffle,
                _ => PlaybackMode.Sequential
            };
        }

        [RelayCommand]
        private void OpenImportOptions()
        {
            if (!IsImportingLibrary)
            {
                IsImportOptionsOpen = true;
            }
        }

        [RelayCommand]
        private void CloseImportOptions() =>
            IsImportOptionsOpen = false;

        [RelayCommand]
        private async Task ImportAllDeviceAudioAsync() =>
            await ImportAudioAsync(PlaylistImportSource.DeviceLibrary);

        [RelayCommand]
        private async Task ImportFolderAudioAsync() =>
            await ImportAudioAsync(PlaylistImportSource.Folder);

        private async Task ImportAudioAsync(PlaylistImportSource importSource)
        {
            if (IsImportingLibrary)
            {
                return;
            }

            var strategy = await ResolveLoadStrategyAsync(importSource);
            if (strategy is null)
            {
                return;
            }

            IsImportOptionsOpen = false;
            IsImportingLibrary = true;
            ImportProgressLabel = "\u6b63\u5728\u51c6\u5907\u5bfc\u5165...";

            try
            {
                var progress = new Progress<AudioImportProgress>(update =>
                {
                    ImportProgressValue = Math.Clamp(update.Progress, 0, 1);
                    ImportProgressLabel = update.Total > 0
                        ? $"{update.Status} {update.Processed}/{update.Total}"
                        : update.Status;
                });

                var result = importSource == PlaylistImportSource.DeviceLibrary
                    ? await _playlistService.LoadAllDeviceAudioAsync(strategy.Value, progress, CancellationToken.None)
                    : await _playlistService.LoadFolderAudioAsync(strategy.Value, progress, CancellationToken.None);

                if (result.WasCancelled)
                {
                    return;
                }

                await ApplyPlaylistChangeAsync(result.Tracks);

                ImportProgressValue = 1;
                ImportProgressLabel = BuildImportCompletedMessage(result);

                await Task.Delay(500);
            }
            catch (Exception ex)
            {
                _errorHandler.HandleError(ex);
            }
            finally
            {
                IsImportingLibrary = false;
            }
        }

        private void ApplyPlaylist(
            IReadOnlyList<AudioTrackRecord> records,
            bool preserveCurrentSelection,
            bool preservePlaybackState)
        {
            var currentTrackKey = preserveCurrentSelection ? SelectedTrack?.TrackKey : null;
            var currentSourceUri = preserveCurrentSelection ? SelectedTrack?.SourceUri : null;
            var currentPositionMilliseconds = preservePlaybackState ? GetCurrentPositionMilliseconds() : 0;
            var shouldKeepPreparedState = preservePlaybackState
                && _hasPreparedTrack
                && !string.IsNullOrWhiteSpace(currentTrackKey);

            var tracks = records
                .Select(CreateTrack)
                .ToList();

            Playlist.Clear();
            foreach (var track in tracks)
            {
                Playlist.Add(track);
            }

            MusicTrack? nextSelection = null;
            if (!string.IsNullOrWhiteSpace(currentTrackKey))
            {
                nextSelection = Playlist.FirstOrDefault(track =>
                    string.Equals(track.TrackKey, currentTrackKey, StringComparison.Ordinal));
            }

            if (nextSelection is null && !string.IsNullOrWhiteSpace(currentSourceUri))
            {
                nextSelection = Playlist.FirstOrDefault(track =>
                    string.Equals(track.SourceUri, currentSourceUri, StringComparison.Ordinal));
            }

            nextSelection ??= Playlist.FirstOrDefault();
            SelectedTrack = nextSelection;

            if (shouldKeepPreparedState
                && nextSelection is not null
                && string.Equals(nextSelection.TrackKey, currentTrackKey, StringComparison.Ordinal))
            {
                _hasPreparedTrack = true;
                _currentDurationMilliseconds = _audioPlayerService.Duration > TimeSpan.Zero
                    ? (long)_audioPlayerService.Duration.TotalMilliseconds
                    : nextSelection.DurationMilliseconds;

                var durationMilliseconds = Math.Max(_currentDurationMilliseconds, 0);
                var ratio = durationMilliseconds > 0
                    ? (double)Math.Min(currentPositionMilliseconds, durationMilliseconds) / durationMilliseconds
                    : 0;

                _isUpdatingProgressFromPlayer = true;
                PlaybackProgress = Math.Clamp(ratio, 0, 1);
                _isUpdatingProgressFromPlayer = false;
                IsPlaying = _audioPlayerService.IsPlaying;
            }
            else
            {
                IsPlaying = false;
                _hasPreparedTrack = false;
                ResetPersistedPlaybackStateCache();
                _isUpdatingProgressFromPlayer = true;
                PlaybackProgress = 0;
                _isUpdatingProgressFromPlayer = false;
            }

            RefreshPlaylistMetadata();
        }

        private async Task ApplyPlaylistChangeAsync(IReadOnlyList<AudioTrackRecord> records)
        {
            var currentTrackKey = SelectedTrack?.TrackKey;
            var currentSourceUri = SelectedTrack?.SourceUri;
            var currentSelectionStillExists = !string.IsNullOrWhiteSpace(currentTrackKey)
                && records.Any(track => string.Equals(track.TrackKey, currentTrackKey, StringComparison.Ordinal));
            var currentSourceStillExists = !string.IsNullOrWhiteSpace(currentSourceUri)
                && records.Any(track => string.Equals(track.SourceUri, currentSourceUri, StringComparison.Ordinal));

            if (!currentSourceStillExists && _hasPreparedTrack)
            {
                await _audioPlayerService.StopAsync();
                _hasPreparedTrack = false;
                ClearPersistedPlaybackState();
            }

            ApplyPlaylist(
                records,
                preserveCurrentSelection: currentSelectionStillExists,
                preservePlaybackState: currentSourceStillExists);

            if (currentSourceStillExists && SelectedTrack is not null)
            {
                PersistPlaybackState(force: true);
            }
        }

        private async Task<PlaylistLoadStrategy?> ResolveLoadStrategyAsync(PlaylistImportSource importSource)
        {
            if (!HasTracks)
            {
                return PlaylistLoadStrategy.ReplaceAll;
            }

            var handlers = PlaylistLoadStrategyRequested;
            if (handlers is null)
            {
                return PlaylistLoadStrategy.ReplaceAll;
            }

            foreach (var handler in handlers.GetInvocationList().Cast<Func<PlaylistImportSource, Task<PlaylistLoadStrategy?>>>() )
            {
                return await handler(importSource);
            }

            return null;
        }

        private string BuildImportCompletedMessage(PlaylistLoadResult result)
        {
            if (result.AddedCount == 0)
            {
                return result.DuplicateSkippedCount > 0 || result.RemovedSkippedCount > 0
                    ? $"\u672a\u65b0\u589e\u6b4c\u66f2\uff0c\u8df3\u8fc7 {result.DuplicateSkippedCount} \u9996\u91cd\u590d\uff0c\u8fc7\u6ee4 {result.RemovedSkippedCount} \u9996\u5df2\u79fb\u9664"
                    : "\u6ca1\u6709\u627e\u5230\u53ef\u5bfc\u5165\u7684\u97f3\u9891";
            }

            return result.Strategy == PlaylistLoadStrategy.Incremental
                ? $"\u65b0\u589e {result.AddedCount} \u9996\uff0c\u8df3\u8fc7 {result.DuplicateSkippedCount} \u9996\u91cd\u590d\uff0c\u8fc7\u6ee4 {result.RemovedSkippedCount} \u9996\u5df2\u79fb\u9664"
                : result.DuplicateSkippedCount > 0 || result.RemovedSkippedCount > 0
                    ? $"\u5df2\u91cd\u65b0\u52a0\u8f7d {result.AddedCount} \u9996\uff0c\u8df3\u8fc7 {result.DuplicateSkippedCount} \u9996\u91cd\u590d\uff0c\u8fc7\u6ee4 {result.RemovedSkippedCount} \u9996\u5df2\u79fb\u9664"
                    : $"\u5df2\u91cd\u65b0\u52a0\u8f7d {result.AddedCount} \u9996\u6b4c\u66f2";
        }

        private void RefreshPlaylistMetadata()
        {
            OnPropertyChanged(nameof(PlaylistSummary));
            OnPropertyChanged(nameof(HasTracks));
            OnPropertyChanged(nameof(IsPlaylistEmpty));
            OnPropertyChanged(nameof(EmptyPlaylistTitle));
            OnPropertyChanged(nameof(EmptyPlaylistSubtitle));
            OnPropertyChanged(nameof(CurrentPlaybackTime));
            OnPropertyChanged(nameof(TotalPlaybackTime));
            OnPropertyChanged(nameof(NowPlayingLabel));
            OnPropertyChanged(nameof(CanLocateCurrentTrack));
            LocateCurrentTrackCommand.NotifyCanExecuteChanged();
        }

        private void ReindexPlaylistBadges()
        {
            for (var index = 0; index < Playlist.Count; index++)
            {
                Playlist[index].Badge = $"{index + 1:00}";
            }
        }

        private MusicTrack? GetFallbackTrackAfterRemoval(int removedIndex)
        {
            if (Playlist.Count == 0)
            {
                return null;
            }

            var nextIndex = removedIndex >= 0 && removedIndex + 1 < Playlist.Count
                ? removedIndex + 1
                : removedIndex - 1;

            return nextIndex >= 0 && nextIndex < Playlist.Count
                ? Playlist[nextIndex]
                : Playlist.FirstOrDefault();
        }

        private async Task RaiseFeedbackAsync(string message)
        {
            var handlers = FeedbackRequested;
            if (handlers is null)
            {
                return;
            }

            foreach (var handler in handlers.GetInvocationList().Cast<Func<string, Task>>())
            {
                await handler(message);
            }
        }

        private MusicTrack CreateTrack(AudioTrackRecord record, int index)
        {
            return new MusicTrack
            {
                Id = record.ID,
                TrackKey = record.TrackKey,
                Title = record.Title,
                Artist = record.Artist,
                DurationMilliseconds = record.DurationMilliseconds,
                Duration = FormatPlaybackTime((int)Math.Round(TimeSpan.FromMilliseconds(record.DurationMilliseconds).TotalSeconds)),
                Badge = $"{index + 1:00}",
                AccentColor = Color.FromArgb(AccentPalette[index % AccentPalette.Length]),
                SourceUri = record.SourceUri,
                SourcePath = record.SourcePath,
                MimeType = record.MimeType
            };
        }

        private async Task PlayTrackAsync(MusicTrack track)
        {
            if (string.IsNullOrWhiteSpace(track.SourceUri))
            {
                return;
            }

            SelectedTrack = track;
            _currentDurationMilliseconds = track.DurationMilliseconds;
            _isUpdatingProgressFromPlayer = true;
            PlaybackProgress = 0;
            _isUpdatingProgressFromPlayer = false;

            await _audioPlayerService.PlayAsync(track.SourceUri);
            _hasPreparedTrack = true;
            IsPlaying = true;
            PersistPlaybackState(force: true);

            OnPropertyChanged(nameof(CurrentPlaybackTime));
            OnPropertyChanged(nameof(TotalPlaybackTime));
        }

        private async Task MoveSelectionAsync(int offset)
        {
            if (Playlist.Count == 0)
            {
                return;
            }

            if (SelectedTrack is null)
            {
                await PlayTrackAsync(Playlist[0]);
                return;
            }

            var currentIndex = Playlist.IndexOf(SelectedTrack);
            if (currentIndex < 0)
            {
                await PlayTrackAsync(Playlist[0]);
                return;
            }

            var nextIndex = (currentIndex + offset + Playlist.Count) % Playlist.Count;
            await PlayTrackAsync(Playlist[nextIndex]);
        }

        private async Task SeekToProgressAsync(double progress)
        {
            try
            {
                var target = TimeSpan.FromMilliseconds(_currentDurationMilliseconds * progress);
                await _audioPlayerService.SeekAsync(target);
            }
            catch (Exception ex)
            {
                _errorHandler.HandleError(ex);
            }
        }

        private void OnPlaybackStateChanged(object? sender, AudioPlaybackStateChangedEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                IsPlaying = e.IsPlaying;

                if (e.Duration > TimeSpan.Zero)
                {
                    _currentDurationMilliseconds = (long)e.Duration.TotalMilliseconds;

                    if (SelectedTrack is not null
                        && SelectedTrack.DurationMilliseconds != _currentDurationMilliseconds)
                    {
                        SelectedTrack.DurationMilliseconds = _currentDurationMilliseconds;
                        SelectedTrack.Duration = FormatPlaybackTime((int)Math.Round(e.Duration.TotalSeconds));
                    }
                }

                var ratio = e.Duration > TimeSpan.Zero
                    ? e.Position.TotalMilliseconds / e.Duration.TotalMilliseconds
                    : 0;

                _isUpdatingProgressFromPlayer = true;
                PlaybackProgress = Math.Clamp(ratio, 0, 1);
                _isUpdatingProgressFromPlayer = false;

                OnPropertyChanged(nameof(CurrentPlaybackTime));
                OnPropertyChanged(nameof(TotalPlaybackTime));

                PersistPlaybackState();
            });
        }

        private void OnPlaybackEnded(object? sender, EventArgs e)
        {
            _ = MainThread.InvokeOnMainThreadAsync(HandlePlaybackEndedAsync);
        }

        private async Task HandlePlaybackEndedAsync()
        {
            if (_isHandlingPlaybackEnded
                || Playlist.Count == 0
                || SelectedTrack is null)
            {
                return;
            }

            _isHandlingPlaybackEnded = true;

            try
            {
                switch (PlaybackMode)
                {
                    case PlaybackMode.SingleLoop:
                        await PlayTrackAsync(SelectedTrack);
                        break;
                    case PlaybackMode.Shuffle:
                        var nextTrack = GetRandomTrack();
                        if (nextTrack is not null)
                        {
                            await PlayTrackAsync(nextTrack);
                        }
                        break;
                    default:
                        await MoveSelectionAsync(1);
                        break;
                }
            }
            finally
            {
                _isHandlingPlaybackEnded = false;
            }
        }

        private MusicTrack? GetRandomTrack()
        {
            if (Playlist.Count == 0)
            {
                return null;
            }

            if (Playlist.Count == 1)
            {
                return Playlist[0];
            }

            var candidates = Playlist
                .Where(track => track != SelectedTrack)
                .ToList();

            return candidates[_random.Next(candidates.Count)];
        }

        private MusicTrack? ResolveCurrentTrackInPlaylist()
        {
            if (Playlist.Count == 0
                || SelectedTrack is null)
            {
                return null;
            }

            if (Playlist.Contains(SelectedTrack))
            {
                return SelectedTrack;
            }

            if (!string.IsNullOrWhiteSpace(SelectedTrack.TrackKey))
            {
                var trackByKey = Playlist.FirstOrDefault(track =>
                    string.Equals(track.TrackKey, SelectedTrack.TrackKey, StringComparison.Ordinal));

                if (trackByKey is not null)
                {
                    return trackByKey;
                }
            }

            if (string.IsNullOrWhiteSpace(SelectedTrack.SourceUri))
            {
                return null;
            }

            return Playlist.FirstOrDefault(track =>
                string.Equals(track.SourceUri, SelectedTrack.SourceUri, StringComparison.Ordinal));
        }

        private async Task RestorePlaybackStateAsync()
        {
            if (Playlist.Count == 0)
            {
                ClearPersistedPlaybackState();
                return;
            }

            var snapshot = _playbackStateService.Load();
            if (snapshot is null)
            {
                return;
            }

            var track = Playlist.FirstOrDefault(candidate =>
                string.Equals(candidate.SourceUri, snapshot.TrackSourceUri, StringComparison.Ordinal));

            if (track is null
                || !_playbackStateService.IsTrackAvailable(track.SourceUri, track.SourcePath))
            {
                ClearPersistedPlaybackState();
                return;
            }

            _isRestoringPlaybackState = true;

            try
            {
                await RestoreTrackAsync(track, snapshot);
            }
            catch
            {
                _hasPreparedTrack = false;
                ClearPersistedPlaybackState();
            }
            finally
            {
                _isRestoringPlaybackState = false;
            }
        }

        private async Task RestoreTrackAsync(MusicTrack track, PlaybackStateSnapshot snapshot)
        {
            var targetPosition = snapshot.PositionMilliseconds;
            if (track.DurationMilliseconds > 0)
            {
                targetPosition = Math.Min(targetPosition, track.DurationMilliseconds);
            }

            SelectedTrack = track;
            _currentDurationMilliseconds = track.DurationMilliseconds;
            _isUpdatingProgressFromPlayer = true;
            PlaybackProgress = track.DurationMilliseconds > 0
                ? Math.Clamp((double)targetPosition / track.DurationMilliseconds, 0, 1)
                : 0;
            _isUpdatingProgressFromPlayer = false;

            await _audioPlayerService.LoadAsync(track.SourceUri);

            if (targetPosition > 0)
            {
                await _audioPlayerService.SeekAsync(TimeSpan.FromMilliseconds(targetPosition));
            }

            if (snapshot.ShouldResumePlayback)
            {
                await _audioPlayerService.ResumeAsync();
            }

            _hasPreparedTrack = true;
            IsPlaying = snapshot.ShouldResumePlayback;

            PersistPlaybackState(force: true);

            OnPropertyChanged(nameof(CurrentPlaybackTime));
            OnPropertyChanged(nameof(TotalPlaybackTime));
        }

        private long GetCurrentPositionMilliseconds()
        {
            var positionMilliseconds = (long)Math.Max(_audioPlayerService.Position.TotalMilliseconds, 0);
            var durationMilliseconds = _audioPlayerService.Duration > TimeSpan.Zero
                ? (long)_audioPlayerService.Duration.TotalMilliseconds
                : _currentDurationMilliseconds;

            return durationMilliseconds > 0
                ? Math.Min(positionMilliseconds, durationMilliseconds)
                : positionMilliseconds;
        }

        private void ClearPersistedPlaybackState()
        {
            _playbackStateService.Clear();
            ResetPersistedPlaybackStateCache();
        }

        private void ResetPersistedPlaybackStateCache()
        {
            _lastPersistedSourceUri = null;
            _lastPersistedPositionMilliseconds = -1;
            _lastPersistedIsPlaying = null;
        }

        private static string FormatPlaybackTime(int totalSeconds)
        {
            totalSeconds = Math.Max(totalSeconds, 0);
            return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
        }
    }
}
