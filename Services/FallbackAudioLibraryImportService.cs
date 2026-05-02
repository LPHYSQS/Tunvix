using Tunvix.Models;

namespace Tunvix.Services
{
    public class FallbackAudioLibraryImportService : IAudioLibraryImportService
    {
        public Task<AudioImportResult> ImportAllDeviceAudioAsync(
            IProgress<AudioImportProgress>? progress = null,
            CancellationToken cancellationToken = default) =>
            throw new PlatformNotSupportedException("当前平台暂不支持设备音频导入。");

        public Task<AudioImportResult> ImportFolderAudioAsync(
            IProgress<AudioImportProgress>? progress = null,
            CancellationToken cancellationToken = default) =>
            throw new PlatformNotSupportedException("当前平台暂不支持文件夹音频导入。");
    }
}
