using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Fonts;
using Tunvix.Models;
using Tunvix.Services;

namespace Tunvix.PageModels
{
    public partial class MainPageModel : ObservableObject
    {
        private readonly ThemeService _themeService;
        private readonly PlaybackModeService _playbackModeService;

        [ObservableProperty]
        private ObservableCollection<MusicTrack> _playlist = new();

        [ObservableProperty]
        private MusicTrack? _selectedTrack;

        [ObservableProperty]
        private bool _isPlaying = true;

        [ObservableProperty]
        private double _playbackProgress = 0.36;

        [ObservableProperty]
        private bool _isPlaylistDrawerOpen;

        [ObservableProperty]
        private PlaybackMode _playbackMode = PlaybackMode.Sequential;

        public string PlayPauseGlyph => IsPlaying
            ? FluentUI.pause_24_regular
            : FluentUI.play_24_regular;

        public string ThemeButtonDescription => IsDarkTheme
            ? "\u5207\u6362\u5230\u6d45\u8272\u4e3b\u9898"
            : "\u5207\u6362\u5230\u6df1\u8272\u4e3b\u9898";

        public string NowPlayingLabel => IsPlaying ? "\u6b63\u5728\u64ad\u653e" : "\u5df2\u6682\u505c";

        public string PlaylistSummary => $"\u5171 {Playlist.Count} \u9996\u6b4c\u66f2";

        public bool IsDarkTheme => _themeService.IsDarkTheme;

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

        public MainPageModel(
            ThemeService themeService,
            PlaybackModeService playbackModeService)
        {
            _themeService = themeService;
            _playbackModeService = playbackModeService;

            Playlist = new ObservableCollection<MusicTrack>
            {
                new MusicTrack
                {
                    Title = "\u4e91\u6f6e\u5e8f\u66f2",
                    Artist = "\u6d41\u98ce\u5de5\u4f5c\u5ba4",
                    Duration = "03:42",
                    Badge = "01",
                    AccentColor = Color.FromArgb("#D96A47")
                },
                new MusicTrack
                {
                    Title = "\u591c\u822a\u9891\u7387",
                    Artist = "\u6781\u5730\u56de\u58f0",
                    Duration = "04:18",
                    Badge = "02",
                    AccentColor = Color.FromArgb("#4D7CFE")
                },
                new MusicTrack
                {
                    Title = "\u6d77\u98ce\u843d\u9488",
                    Artist = "\u84dd\u6e2f\u4e50\u961f",
                    Duration = "03:56",
                    Badge = "03",
                    AccentColor = Color.FromArgb("#2AA889")
                },
                new MusicTrack
                {
                    Title = "\u6781\u663c\u56de\u54cd",
                    Artist = "\u5317\u5883\u78c1\u5e26",
                    Duration = "05:07",
                    Badge = "04",
                    AccentColor = Color.FromArgb("#A56CFF")
                },
                new MusicTrack
                {
                    Title = "\u5c71\u8c37\u6162\u677f",
                    Artist = "\u82d4\u75d5\u7406\u8bba",
                    Duration = "02:51",
                    Badge = "05",
                    AccentColor = Color.FromArgb("#F2B94B")
                },
                new MusicTrack
                {
                    Title = "\u957f\u591c\u661f\u56fe",
                    Artist = "\u6d41\u660e\u8f68\u8ff9",
                    Duration = "04:40",
                    Badge = "06",
                    AccentColor = Color.FromArgb("#F06D8F")
                }
            };

            SelectedTrack = Playlist.FirstOrDefault();
            PlaybackMode = _playbackModeService.GetStoredMode();
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
                IsPlaying = true;
            }

            OnPropertyChanged(nameof(CurrentTrackTitle));
            OnPropertyChanged(nameof(CurrentTrackArtist));
            OnPropertyChanged(nameof(CurrentPlaybackTime));
            OnPropertyChanged(nameof(TotalPlaybackTime));
        }

        partial void OnIsPlayingChanged(bool value)
        {
            OnPropertyChanged(nameof(PlayPauseGlyph));
            OnPropertyChanged(nameof(NowPlayingLabel));
        }

        partial void OnPlaybackProgressChanged(double value)
        {
            OnPropertyChanged(nameof(CurrentPlaybackTime));
        }

        partial void OnPlaybackModeChanged(PlaybackMode value)
        {
            _playbackModeService.SaveMode(value);
            OnPropertyChanged(nameof(PlaybackModeLabel));
            OnPropertyChanged(nameof(PlaybackModeGlyph));
            OnPropertyChanged(nameof(PlaybackModeDescription));
        }

        public string CurrentTrackTitle => SelectedTrack?.Title ?? "\u672a\u9009\u62e9\u6b4c\u66f2";

        public string CurrentTrackArtist => SelectedTrack?.Artist ?? "\u7b49\u5f85\u64ad\u653e\u5217\u8868";

        public string CurrentPlaybackTime => FormatPlaybackTime(GetCurrentPlaybackSeconds());

        public string TotalPlaybackTime => SelectedTrack?.Duration ?? "00:00";

        [RelayCommand]
        private void TogglePlayback() =>
            IsPlaying = !IsPlaying;

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
        private void OpenPlaylistDrawer() =>
            IsPlaylistDrawerOpen = true;

        [RelayCommand]
        private void ClosePlaylistDrawer() =>
            IsPlaylistDrawerOpen = false;

        [RelayCommand]
        private void ToggleTheme()
        {
            _themeService.ToggleTheme();
            OnPropertyChanged(nameof(IsDarkTheme));
            OnPropertyChanged(nameof(ThemeButtonDescription));
        }

        [RelayCommand]
        private void SelectTrack(MusicTrack? track)
        {
            if (track is null)
            {
                return;
            }

            SelectedTrack = track;
        }

        [RelayCommand]
        private void PreviousTrack() =>
            MoveSelection(-1);

        [RelayCommand]
        private void NextTrack() =>
            MoveSelection(1);

        private void MoveSelection(int offset)
        {
            if (Playlist.Count == 0)
            {
                return;
            }

            if (SelectedTrack is null)
            {
                SelectedTrack = Playlist[0];
                return;
            }

            var currentIndex = Playlist.IndexOf(SelectedTrack);
            if (currentIndex < 0)
            {
                SelectedTrack = Playlist[0];
                return;
            }

            var nextIndex = (currentIndex + offset + Playlist.Count) % Playlist.Count;
            SelectedTrack = Playlist[nextIndex];
        }

        private int GetCurrentPlaybackSeconds()
        {
            var totalSeconds = GetTrackDurationSeconds();
            var currentSeconds = (int)Math.Round(totalSeconds * PlaybackProgress);
            return Math.Clamp(currentSeconds, 0, totalSeconds);
        }

        private int GetTrackDurationSeconds()
        {
            if (SelectedTrack?.Duration is null)
            {
                return 0;
            }

            var parts = SelectedTrack.Duration.Split(':');
            if (parts.Length != 2
                || !int.TryParse(parts[0], out var minutes)
                || !int.TryParse(parts[1], out var seconds))
            {
                return 0;
            }

            return (minutes * 60) + seconds;
        }

        private static string FormatPlaybackTime(int totalSeconds) =>
            $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
    }
}
