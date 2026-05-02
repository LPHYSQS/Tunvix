using CommunityToolkit.Mvvm.ComponentModel;

namespace Tunvix.Models
{
    public partial class MusicTrack : ObservableObject
    {
        [ObservableProperty]
        private string _title = string.Empty;

        [ObservableProperty]
        private string _artist = string.Empty;

        [ObservableProperty]
        private string _duration = string.Empty;

        [ObservableProperty]
        private string _badge = string.Empty;

        [ObservableProperty]
        private Color _accentColor = Colors.Transparent;

        [ObservableProperty]
        private bool _isCurrent;
    }
}
