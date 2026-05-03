using Microsoft.Extensions.Logging;
#if ANDROID
using Android.Graphics;
using Android.Media;
using AndroidUri = Android.Net.Uri;
#endif

namespace Tunvix.Services
{
    public sealed class AudioMetadataService : IAudioMetadataService
    {
        private const int MaxArtworkEdge = 512;

        private readonly ILogger<AudioMetadataService> _logger;
        private readonly Dictionary<int, ImageSource?> _artworkCache = new();
        private readonly Dictionary<int, Task<ImageSource?>> _pendingLoads = new();
        private readonly object _cacheLock = new();

#if ANDROID
        private readonly global::Android.Content.Context _context;
#endif

        public AudioMetadataService(ILogger<AudioMetadataService> logger)
        {
            _logger = logger;
#if ANDROID
            _context = global::Android.App.Application.Context;
#endif
        }

        public async Task<ImageSource?> GetArtworkAsync(
            int songId,
            string sourceUri,
            string sourcePath,
            string mimeType,
            CancellationToken cancellationToken = default)
        {
            if (songId <= 0)
            {
                return null;
            }

            Task<ImageSource?> loadTask;
            lock (_cacheLock)
            {
                if (_artworkCache.TryGetValue(songId, out var cachedArtwork))
                {
                    return cachedArtwork;
                }

                if (_pendingLoads.TryGetValue(songId, out var pendingLoad))
                {
                    loadTask = pendingLoad;
                }
                else
                {
                    loadTask = LoadAndCacheArtworkAsync(songId, sourceUri, sourcePath, mimeType);
                    _pendingLoads[songId] = loadTask;
                }
            }

            return cancellationToken.CanBeCanceled
                ? await loadTask.WaitAsync(cancellationToken)
                : await loadTask;
        }

        public void RemoveFromCache(int songId)
        {
            if (songId <= 0)
            {
                return;
            }

            lock (_cacheLock)
            {
                _artworkCache.Remove(songId);
                _pendingLoads.Remove(songId);
            }
        }

        public void TrimCache(IReadOnlyCollection<int> songIds)
        {
            var activeSongIds = songIds.Count == 0
                ? null
                : new HashSet<int>(songIds);

            lock (_cacheLock)
            {
                foreach (var songId in _artworkCache.Keys.ToArray())
                {
                    if (activeSongIds is null || !activeSongIds.Contains(songId))
                    {
                        _artworkCache.Remove(songId);
                    }
                }
            }
        }

        private async Task<ImageSource?> LoadAndCacheArtworkAsync(
            int songId,
            string sourceUri,
            string sourcePath,
            string mimeType)
        {
            ImageSource? artwork = null;

            try
            {
                artwork = await Task.Run(() => LoadArtworkCore(sourceUri, sourcePath, mimeType));
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to load artwork for song {SongId}.", songId);
            }
            finally
            {
                lock (_cacheLock)
                {
                    _artworkCache[songId] = artwork;
                    _pendingLoads.Remove(songId);
                }
            }

            return artwork;
        }

        private ImageSource? LoadArtworkCore(string sourceUri, string sourcePath, string mimeType)
        {
#if ANDROID
            try
            {
                using var metadataRetriever = new MediaMetadataRetriever();
                if (!TrySetDataSource(metadataRetriever, sourceUri, sourcePath))
                {
                    return null;
                }

                var embeddedPicture = metadataRetriever.GetEmbeddedPicture();
                if (embeddedPicture is null || embeddedPicture.Length == 0)
                {
                    return null;
                }

                var normalizedBytes = NormalizeArtworkBytes(embeddedPicture);
                return ImageSource.FromStream(() => new MemoryStream(normalizedBytes, writable: false));
            }
            catch (Exception ex)
            {
                _logger.LogDebug(
                    ex,
                    "Failed to parse embedded artwork for source '{SourceUri}' ({MimeType}).",
                    sourceUri,
                    mimeType);
                return null;
            }
#else
            return null;
#endif
        }

#if ANDROID
        private bool TrySetDataSource(MediaMetadataRetriever metadataRetriever, string sourceUri, string sourcePath)
        {
            if (Uri.TryCreate(sourceUri, UriKind.Absolute, out var parsedUri))
            {
                if (parsedUri.IsFile && !string.IsNullOrWhiteSpace(parsedUri.LocalPath))
                {
                    metadataRetriever.SetDataSource(parsedUri.LocalPath);
                    return true;
                }

                var androidUri = AndroidUri.Parse(sourceUri);
                if (androidUri is not null)
                {
                    metadataRetriever.SetDataSource(_context, androidUri);
                    return true;
                }
            }

            if (!string.IsNullOrWhiteSpace(sourcePath) && System.IO.Path.IsPathRooted(sourcePath))
            {
                metadataRetriever.SetDataSource(sourcePath);
                return true;
            }

            return false;
        }

        private static byte[] NormalizeArtworkBytes(byte[] embeddedPicture)
        {
            var boundsOptions = new BitmapFactory.Options
            {
                InJustDecodeBounds = true
            };

            BitmapFactory.DecodeByteArray(embeddedPicture, 0, embeddedPicture.Length, boundsOptions);

            var longestEdge = Math.Max(boundsOptions.OutWidth, boundsOptions.OutHeight);
            if (longestEdge <= 0 || longestEdge <= MaxArtworkEdge)
            {
                return embeddedPicture;
            }

            var decodeOptions = new BitmapFactory.Options
            {
                InSampleSize = CalculateInSampleSize(boundsOptions.OutWidth, boundsOptions.OutHeight, MaxArtworkEdge),
                InPreferredConfig = Bitmap.Config.Argb8888
            };

            using var bitmap = BitmapFactory.DecodeByteArray(embeddedPicture, 0, embeddedPicture.Length, decodeOptions);
            if (bitmap is null)
            {
                return embeddedPicture;
            }

            using var outputStream = new MemoryStream();
            Bitmap.CompressFormat format;
            int quality;
            if (bitmap.HasAlpha)
            {
                format = Bitmap.CompressFormat.Png!;
                quality = 100;
            }
            else
            {
                format = Bitmap.CompressFormat.Jpeg!;
                quality = 88;
            }

            bitmap.Compress(format, quality, outputStream);
            return outputStream.ToArray();
        }

        private static int CalculateInSampleSize(int width, int height, int targetEdge)
        {
            var inSampleSize = 1;
            while (width / inSampleSize > targetEdge || height / inSampleSize > targetEdge)
            {
                inSampleSize *= 2;
            }

            return Math.Max(inSampleSize, 1);
        }
#endif
    }
}
