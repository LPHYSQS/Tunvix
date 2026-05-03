using CommunityToolkit.Mvvm.ComponentModel;

namespace Tunvix.Models
{
    public partial class MusicTrack : ObservableObject
    {
        [ObservableProperty]
        private int _id;

        [ObservableProperty]
        private string _trackKey = string.Empty;

        [ObservableProperty]
        private string _title = string.Empty;

        [ObservableProperty]
        private string _artist = string.Empty;

        [ObservableProperty]
        private string _duration = string.Empty;

        [ObservableProperty]
        private string _badge = string.Empty;

        [ObservableProperty]
        private string _sourceUri = string.Empty;

        [ObservableProperty]
        private string _sourcePath = string.Empty;

        [ObservableProperty]
        private string _mimeType = string.Empty;

        [ObservableProperty]
        private long _durationMilliseconds;

        [ObservableProperty]
        private Color _accentColor = Colors.Transparent;

        [ObservableProperty]
        private bool _isCurrent;

        [ObservableProperty]
        private bool _isPlayingCurrentTrack;
    }
}
