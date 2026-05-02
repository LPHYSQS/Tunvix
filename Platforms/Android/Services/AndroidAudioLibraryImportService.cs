using Android.Content;
using Android.Database;
using Android.Media;
using Android.Net;
using Android.Provider;
using AndroidX.DocumentFile.Provider;
using Microsoft.Maui.ApplicationModel;
using Tunvix.Models;
using Tunvix.Platforms.Android.Permissions;
using Tunvix.Services;
using AndroidUri = Android.Net.Uri;

namespace Tunvix.Platforms.Android.Services
{
    public sealed class AndroidAudioLibraryImportService : IAudioLibraryImportService
    {
        private static readonly HashSet<string> SupportedAudioExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".aac",
            ".adts",
            ".amr",
            ".awb",
            ".flac",
            ".imy",
            ".m4a",
            ".m4b",
            ".mid",
            ".midi",
            ".mka",
            ".mp3",
            ".mp4",
            ".mxmf",
            ".oga",
            ".ogg",
            ".opus",
            ".ota",
            ".rtttl",
            ".rtx",
            ".wav",
            ".webm",
            ".xmf",
            ".3gp"
        };

        private readonly Context _context;
        private readonly IAndroidFolderPickerService _folderPickerService;

        public AndroidAudioLibraryImportService(IAndroidFolderPickerService folderPickerService)
        {
            _folderPickerService = folderPickerService;
            _context = global::Android.App.Application.Context;
        }

        public async Task<AudioImportResult> ImportAllDeviceAudioAsync(
            IProgress<AudioImportProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            await EnsureAudioReadPermissionAsync();

            var resolver = _context.ContentResolver
                ?? throw new InvalidOperationException("无法访问 Android 媒体内容提供器。");

            const string columnId = "_id";
            const string columnTitle = "title";
            const string columnArtist = "artist";
            const string columnDuration = "duration";
            const string columnMimeType = "mime_type";
            const string columnDisplayName = "_display_name";
            const string columnRelativePath = "relative_path";

            var projection = new[]
            {
                columnId,
                columnTitle,
                columnArtist,
                columnDuration,
                columnMimeType,
                columnDisplayName,
                columnRelativePath
            };

            using var cursor = resolver.Query(
                MediaStore.Audio.Media.ExternalContentUri,
                projection,
                "is_music != 0",
                null,
                "title COLLATE NOCASE ASC");

            if (cursor is null)
            {
                return new AudioImportResult(Array.Empty<AudioTrackRecord>());
            }

            var tracks = new List<AudioTrackRecord>(Math.Max(cursor.Count, 0));
            var total = Math.Max(cursor.Count, 0);
            var processed = 0;
            progress?.Report(new AudioImportProgress(0, "正在扫描设备音频...", 0, total));

            while (cursor.MoveToNext())
            {
                cancellationToken.ThrowIfCancellationRequested();
                processed++;

                var track = CreateMediaStoreTrack(cursor, columnId, columnTitle, columnArtist, columnDuration, columnMimeType, columnDisplayName, columnRelativePath);
                if (track is not null)
                {
                    tracks.Add(track);
                }

                progress?.Report(new AudioImportProgress(
                    total == 0 ? 0 : (double)processed / total,
                    "正在载入设备音频",
                    processed,
                    total));
            }

            progress?.Report(new AudioImportProgress(1, "设备音频载入完成", total, total));
            return new AudioImportResult(tracks);
        }

        public async Task<AudioImportResult> ImportFolderAudioAsync(
            IProgress<AudioImportProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var folder = await _folderPickerService.PickFolderAsync(cancellationToken);
            if (folder is null)
            {
                return new AudioImportResult(Array.Empty<AudioTrackRecord>(), wasCancelled: true);
            }

            var treeUri = AndroidUri.Parse(folder.TreeUri)
                ?? throw new InvalidOperationException("所选文件夹无效。");

            var root = DocumentFile.FromTreeUri(_context, treeUri)
                ?? throw new InvalidOperationException("无法读取所选文件夹。");

            progress?.Report(new AudioImportProgress(0, $"正在扫描“{folder.DisplayName}”...", 0, 0));

            var files = new List<(DocumentFile file, string relativePath)>();
            CollectAudioFiles(root, string.Empty, files, cancellationToken);

            if (files.Count == 0)
            {
                progress?.Report(new AudioImportProgress(1, "所选文件夹中没有可导入音频", 0, 0));
                return new AudioImportResult(Array.Empty<AudioTrackRecord>());
            }

            var tracks = new List<AudioTrackRecord>(files.Count);
            for (var index = 0; index < files.Count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var current = files[index];
                var track = CreateDocumentTrack(current.file, current.relativePath, folder.TreeUri);
                if (track is not null)
                {
                    tracks.Add(track);
                }

                progress?.Report(new AudioImportProgress(
                    (double)(index + 1) / files.Count,
                    "正在载入文件夹音频",
                    index + 1,
                    files.Count));
            }

            progress?.Report(new AudioImportProgress(1, "文件夹音频载入完成", files.Count, files.Count));
            return new AudioImportResult(tracks);
        }

        private static AudioTrackRecord? CreateMediaStoreTrack(
            ICursor cursor,
            string idColumn,
            string titleColumn,
            string artistColumn,
            string durationColumn,
            string mimeTypeColumn,
            string displayNameColumn,
            string relativePathColumn)
        {
            var id = GetLong(cursor, idColumn);
            if (id <= 0)
            {
                return null;
            }

            var contentUri = ContentUris.WithAppendedId(MediaStore.Audio.Media.ExternalContentUri, id);
            var displayName = GetString(cursor, displayNameColumn);
            var title = NormalizeTitle(GetString(cursor, titleColumn), displayName);
            var artist = NormalizeArtist(GetString(cursor, artistColumn));
            var durationMilliseconds = Math.Max(GetLong(cursor, durationColumn), 0);
            var mimeType = NormalizeMimeType(GetString(cursor, mimeTypeColumn), displayName);
            var relativePath = GetString(cursor, relativePathColumn);

            return new AudioTrackRecord
            {
                Title = title,
                Artist = artist,
                DurationMilliseconds = durationMilliseconds,
                SourceUri = contentUri.ToString(),
                SourcePath = BuildDisplayPath(relativePath, displayName),
                MimeType = mimeType,
                ImportScope = "device"
            };
        }

        private AudioTrackRecord? CreateDocumentTrack(DocumentFile file, string relativePath, string treeUri)
        {
            try
            {
                using var metadataRetriever = new MediaMetadataRetriever();
                metadataRetriever.SetDataSource(_context, file.Uri);

                var title = NormalizeTitle(
                    metadataRetriever.ExtractMetadata(MetadataKey.Title),
                    file.Name);
                var artist = NormalizeArtist(metadataRetriever.ExtractMetadata(MetadataKey.Artist));
                var durationMilliseconds = ParseDuration(metadataRetriever.ExtractMetadata(MetadataKey.Duration));
                var mimeType = NormalizeMimeType(file.Type, file.Name);

                return new AudioTrackRecord
                {
                    Title = title,
                    Artist = artist,
                    DurationMilliseconds = durationMilliseconds,
                    SourceUri = file.Uri.ToString(),
                    SourcePath = relativePath,
                    MimeType = mimeType,
                    ImportScope = "folder",
                    FolderTreeUri = treeUri
                };
            }
            catch
            {
                return null;
            }
        }

        private void CollectAudioFiles(
            DocumentFile directory,
            string currentPath,
            ICollection<(DocumentFile file, string relativePath)> results,
            CancellationToken cancellationToken)
        {
            foreach (var child in directory.ListFiles())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (child.IsDirectory)
                {
                    var nextDirectoryPath = string.IsNullOrWhiteSpace(currentPath)
                        ? child.Name ?? string.Empty
                        : $"{currentPath}/{child.Name}";

                    CollectAudioFiles(child, nextDirectoryPath, results, cancellationToken);
                    continue;
                }

                if (!child.IsFile || !IsSupportedAudioFile(child))
                {
                    continue;
                }

                var relativePath = string.IsNullOrWhiteSpace(currentPath)
                    ? child.Name ?? "未知文件"
                    : $"{currentPath}/{child.Name}";

                results.Add((child, relativePath));
            }
        }

        private static bool IsSupportedAudioFile(DocumentFile file)
        {
            if (!string.IsNullOrWhiteSpace(file.Type)
                && file.Type.StartsWith("audio/", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var extension = Path.GetExtension(file.Name ?? string.Empty);
            return SupportedAudioExtensions.Contains(extension);
        }

        private static string NormalizeTitle(string? title, string? fallbackName)
        {
            if (!string.IsNullOrWhiteSpace(title))
            {
                return title.Trim();
            }

            var fallback = fallbackName ?? "未知音频";
            return Path.GetFileNameWithoutExtension(fallback);
        }

        private static string NormalizeArtist(string? artist)
        {
            if (string.IsNullOrWhiteSpace(artist)
                || artist.Equals("<unknown>", StringComparison.OrdinalIgnoreCase))
            {
                return "未知艺术家";
            }

            return artist.Trim();
        }

        private static string NormalizeMimeType(string? mimeType, string? fileName)
        {
            if (!string.IsNullOrWhiteSpace(mimeType))
            {
                return mimeType;
            }

            var extension = Path.GetExtension(fileName ?? string.Empty);
            return string.IsNullOrWhiteSpace(extension)
                ? "audio/*"
                : $"audio/{extension.TrimStart('.').ToLowerInvariant()}";
        }

        private static string BuildDisplayPath(string? relativePath, string? displayName)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return displayName ?? string.Empty;
            }

            return $"{relativePath}{displayName}";
        }

        private static long ParseDuration(string? duration) =>
            long.TryParse(duration, out var value) ? Math.Max(value, 0) : 0;

        private static string? GetString(ICursor cursor, string columnName)
        {
            var index = cursor.GetColumnIndex(columnName);
            return index >= 0 && !cursor.IsNull(index)
                ? cursor.GetString(index)
                : null;
        }

        private static long GetLong(ICursor cursor, string columnName)
        {
            var index = cursor.GetColumnIndex(columnName);
            return index >= 0 && !cursor.IsNull(index)
                ? cursor.GetLong(index)
                : 0;
        }

        private static async Task EnsureAudioReadPermissionAsync()
        {
            var status = await Microsoft.Maui.ApplicationModel.Permissions.CheckStatusAsync<ReadAudioPermission>();
            if (status == PermissionStatus.Granted)
            {
                return;
            }

            status = await Microsoft.Maui.ApplicationModel.Permissions.RequestAsync<ReadAudioPermission>();
            if (status != PermissionStatus.Granted)
            {
                throw new PermissionException("未获得读取设备音频的权限，无法加载本机全部音频。");
            }
        }
    }
}
