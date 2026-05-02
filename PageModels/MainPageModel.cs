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
        private readonly AudioTrackRepository _audioTrackRepository;
        private readonly IAudioLibraryImportService _audioLibraryImportService;
        private readonly IAudioPlayerService _audioPlayerService;
        private readonly IErrorHandler _errorHandler;
        private readonly Random _random = new();

        private bool _isInitialized;
        private bool _isUpdatingProgressFromPlayer;
        private bool _isHandlingPlaybackEnded;
        private long _currentDurationMilliseconds;

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
            AudioTrackRepository audioTrackRepository,
            IAudioLibraryImportService audioLibraryImportService,
            IAudioPlayerService audioPlayerService,
            IErrorHandler errorHandler)
        {
            _themeService = themeService;
            _playbackModeService = playbackModeService;
            _audioTrackRepository = audioTrackRepository;
            _audioLibraryImportService = audioLibraryImportService;
            _audioPlayerService = audioPlayerService;
            _errorHandler = errorHandler;

            PlaybackMode = _playbackModeService.GetStoredMode();

            _audioPlayerService.PlaybackStateChanged += OnPlaybackStateChanged;
            _audioPlayerService.PlaybackEnded += OnPlaybackEnded;
        }

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

            await LoadPlaylistAsync(preserveCurrentSelection: false);
            _isInitialized = true;
        }

        partial void OnSelectedTrackChanged(MusicTrack? oldValue, MusicTrack? newValue)
        {
            if (oldValue is not null)
            {
                oldValue.IsCurrent = false;
            }

            if (newValue is not null)
            {
                newValue.IsCurrent = true;
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
        }

        partial void OnIsPlayingChanged(bool value)
        {
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
            await ImportAudioAsync((progress, cancellationToken) =>
                _audioLibraryImportService.ImportAllDeviceAudioAsync(progress, cancellationToken));

        [RelayCommand]
        private async Task ImportFolderAudioAsync() =>
            await ImportAudioAsync((progress, cancellationToken) =>
                _audioLibraryImportService.ImportFolderAudioAsync(progress, cancellationToken));

        private async Task ImportAudioAsync(
            Func<IProgress<AudioImportProgress>, CancellationToken, Task<AudioImportResult>> importOperation)
        {
            if (IsImportingLibrary)
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

                var result = await importOperation(progress, CancellationToken.None);
                if (result.WasCancelled)
                {
                    return;
                }

                await _audioPlayerService.StopAsync();
                await _audioTrackRepository.ReplaceAllAsync(result.Tracks);
                await LoadPlaylistAsync(preserveCurrentSelection: false);

                ImportProgressValue = 1;
                ImportProgressLabel = result.Tracks.Count > 0
                    ? $"\u5df2\u5bfc\u5165 {result.Tracks.Count} \u9996\u6b4c\u66f2"
                    : "\u6ca1\u6709\u627e\u5230\u53ef\u5bfc\u5165\u7684\u97f3\u9891";

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

        private async Task LoadPlaylistAsync(bool preserveCurrentSelection)
        {
            var currentSourceUri = preserveCurrentSelection
                ? SelectedTrack?.SourceUri
                : null;

            var records = await _audioTrackRepository.ListAsync();
            var tracks = records
                .Select(CreateTrack)
                .ToList();

            Playlist.Clear();
            foreach (var track in tracks)
            {
                Playlist.Add(track);
            }

            var nextSelection = !string.IsNullOrWhiteSpace(currentSourceUri)
                ? Playlist.FirstOrDefault(track => track.SourceUri == currentSourceUri)
                : Playlist.FirstOrDefault();

            SelectedTrack = nextSelection;
            IsPlaying = false;
            _isUpdatingProgressFromPlayer = true;
            PlaybackProgress = 0;
            _isUpdatingProgressFromPlayer = false;

            OnPropertyChanged(nameof(PlaylistSummary));
            OnPropertyChanged(nameof(HasTracks));
            OnPropertyChanged(nameof(IsPlaylistEmpty));
            OnPropertyChanged(nameof(EmptyPlaylistTitle));
            OnPropertyChanged(nameof(EmptyPlaylistSubtitle));
            OnPropertyChanged(nameof(CurrentPlaybackTime));
            OnPropertyChanged(nameof(TotalPlaybackTime));
            OnPropertyChanged(nameof(NowPlayingLabel));
        }

        private MusicTrack CreateTrack(AudioTrackRecord record, int index)
        {
            return new MusicTrack
            {
                Id = record.ID,
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
            IsPlaying = true;

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

        private static string FormatPlaybackTime(int totalSeconds)
        {
            totalSeconds = Math.Max(totalSeconds, 0);
            return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
        }
    }
}
