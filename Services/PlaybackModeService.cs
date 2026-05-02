using Tunvix.Models;

namespace Tunvix.Services
{
    public class PlaybackModeService
    {
        private const string PlaybackModePreferenceKey = "playback_mode";

        public PlaybackMode GetStoredMode()
        {
            var storedValue = Preferences.Default.Get(
                PlaybackModePreferenceKey,
                PlaybackMode.Sequential.ToString());

            return Enum.TryParse<PlaybackMode>(storedValue, out var mode)
                ? mode
                : PlaybackMode.Sequential;
        }

        public void SaveMode(PlaybackMode mode) =>
            Preferences.Default.Set(PlaybackModePreferenceKey, mode.ToString());
    }
}
