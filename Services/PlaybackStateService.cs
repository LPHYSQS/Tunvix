using Microsoft.Maui.Storage;
#if ANDROID
using AndroidUri = Android.Net.Uri;
#endif

namespace Tunvix.Services
{
    public sealed class PlaybackStateService : IPlaybackStateService
    {
        private const string TrackSourceUriKey = "playback_state_track_source_uri";
        private const string TrackSourcePathKey = "playback_state_track_source_path";
        private const string PositionMillisecondsKey = "playback_state_position_milliseconds";
        private const string ShouldResumePlaybackKey = "playback_state_should_resume_playback";

        public PlaybackStateSnapshot? Load()
        {
            var sourceUri = Preferences.Default.Get(TrackSourceUriKey, string.Empty);
            if (string.IsNullOrWhiteSpace(sourceUri))
            {
                return null;
            }

            return new PlaybackStateSnapshot(
                sourceUri,
                Preferences.Default.Get(TrackSourcePathKey, string.Empty),
                Math.Max(Preferences.Default.Get(PositionMillisecondsKey, 0L), 0L),
                Preferences.Default.Get(ShouldResumePlaybackKey, false));
        }

        public void Save(PlaybackStateSnapshot snapshot)
        {
            Preferences.Default.Set(TrackSourceUriKey, snapshot.TrackSourceUri);
            Preferences.Default.Set(TrackSourcePathKey, snapshot.TrackSourcePath);
            Preferences.Default.Set(PositionMillisecondsKey, Math.Max(snapshot.PositionMilliseconds, 0L));
            Preferences.Default.Set(ShouldResumePlaybackKey, snapshot.ShouldResumePlayback);
        }

        public void Clear()
        {
            Preferences.Default.Remove(TrackSourceUriKey);
            Preferences.Default.Remove(TrackSourcePathKey);
            Preferences.Default.Remove(PositionMillisecondsKey);
            Preferences.Default.Remove(ShouldResumePlaybackKey);
        }

        public bool IsTrackAvailable(string sourceUri, string sourcePath)
        {
            if (TryGetFilePath(sourceUri, sourcePath, out var filePath))
            {
                return File.Exists(filePath);
            }

#if ANDROID
            if (Uri.TryCreate(sourceUri, UriKind.Absolute, out var parsedUri)
                && string.Equals(parsedUri.Scheme, "content", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var androidUri = AndroidUri.Parse(sourceUri);
                    if (androidUri is null)
                    {
                        return false;
                    }

                    var context = global::Android.App.Application.Context;
                    var descriptor = context.ContentResolver?.OpenAssetFileDescriptor(
                        androidUri,
                        "r");

                    using (descriptor)
                    {
                        return descriptor is not null;
                    }
                }
                catch
                {
                    return false;
                }
            }
#endif

            return true;
        }

        private static bool TryGetFilePath(string sourceUri, string sourcePath, out string filePath)
        {
            if (Uri.TryCreate(sourceUri, UriKind.Absolute, out var parsedUri)
                && parsedUri.IsFile
                && !string.IsNullOrWhiteSpace(parsedUri.LocalPath))
            {
                filePath = parsedUri.LocalPath;
                return true;
            }

            if (Path.IsPathRooted(sourcePath))
            {
                filePath = sourcePath;
                return true;
            }

            filePath = string.Empty;
            return false;
        }
    }
}
