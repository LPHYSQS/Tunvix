using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Tunvix.Models;

namespace Tunvix.Services
{
    public static class AudioTrackKeyFactory
    {
        public static string Create(AudioTrackRecord track)
        {
            ArgumentNullException.ThrowIfNull(track);

            return Create(
                track.Title,
                track.Artist,
                track.DurationMilliseconds,
                track.SourcePath,
                track.SourceUri);
        }

        public static string Create(
            string? title,
            string? artist,
            long durationMilliseconds,
            string? sourcePath,
            string? sourceUri)
        {
            var fileName = ResolveFileName(sourcePath, sourceUri);
            var keyMaterial = string.Join(
                "|",
                NormalizeText(title),
                NormalizeText(artist),
                Math.Max(durationMilliseconds, 0).ToString(CultureInfo.InvariantCulture),
                NormalizeText(fileName));

            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(keyMaterial));
            return Convert.ToHexString(hash);
        }

        public static string Ensure(string? trackKey, string? title, string? artist, long durationMilliseconds, string? sourcePath, string? sourceUri)
        {
            return string.IsNullOrWhiteSpace(trackKey)
                ? Create(title, artist, durationMilliseconds, sourcePath, sourceUri)
                : trackKey.Trim().ToUpperInvariant();
        }

        private static string ResolveFileName(string? sourcePath, string? sourceUri)
        {
            var candidate = sourcePath;
            if (string.IsNullOrWhiteSpace(candidate)
                && Uri.TryCreate(sourceUri, UriKind.Absolute, out var uri))
            {
                candidate = uri.Segments.LastOrDefault();
            }

            if (string.IsNullOrWhiteSpace(candidate))
            {
                return string.Empty;
            }

            var normalized = candidate.Replace('\\', '/').Trim();
            var fileName = Path.GetFileName(normalized);

            return fileName.Trim().ToLowerInvariant();
        }

        private static string NormalizeText(string? value) =>
            string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().ToLowerInvariant();
    }
}
