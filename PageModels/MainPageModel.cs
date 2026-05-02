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

        [ObservableProperty]
        private ObservableCollection<MusicTrack> _playlist = new();

        [ObservableProperty]
        private MusicTrack? _selectedTrack;

        [ObservableProperty]
        private bool _isPlaying = true;

        public string PlayPauseGlyph => IsPlaying
            ? FluentUI.pause_24_regular
            : FluentUI.play_24_regular;

        public string ThemeButtonDescription => IsDarkTheme
            ? "\u5207\u6362\u5230\u6d45\u8272\u4e3b\u9898"
            : "\u5207\u6362\u5230\u6df1\u8272\u4e3b\u9898";

        public string NowPlayingLabel => IsPlaying ? "\u6b63\u5728\u64ad\u653e" : "\u5df2\u6682\u505c";

        public string PlaylistSummary => $"\u5171 {Playlist.Count} \u9996\u6b4c\u66f2";

        public bool IsDarkTheme => _themeService.IsDarkTheme;

        public MainPageModel(ThemeService themeService)
        {
            _themeService = themeService;

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
        }

        partial void OnIsPlayingChanged(bool value)
        {
            OnPropertyChanged(nameof(PlayPauseGlyph));
            OnPropertyChanged(nameof(NowPlayingLabel));
        }

        public string CurrentTrackTitle => SelectedTrack?.Title ?? "\u672a\u9009\u62e9\u6b4c\u66f2";

        public string CurrentTrackArtist => SelectedTrack?.Artist ?? "\u7b49\u5f85\u64ad\u653e\u5217\u8868";

        [RelayCommand]
        private void TogglePlayback() =>
            IsPlaying = !IsPlaying;

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
    }
}
