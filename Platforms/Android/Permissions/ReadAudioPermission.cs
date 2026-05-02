using Android;
using Microsoft.Maui.ApplicationModel;
using System.Runtime.Versioning;

namespace Tunvix.Platforms.Android.Permissions
{
    public sealed class ReadAudioPermission : Microsoft.Maui.ApplicationModel.Permissions.BasePlatformPermission
    {
        public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
            OperatingSystem.IsAndroidVersionAtLeast(33)
                ? GetAndroid13Permissions()
                : GetLegacyPermissions();

        [SupportedOSPlatform("android33.0")]
        private static (string androidPermission, bool isRuntime)[] GetAndroid13Permissions() =>
            new[] { (Manifest.Permission.ReadMediaAudio, true) };

        private static (string androidPermission, bool isRuntime)[] GetLegacyPermissions() =>
            new[] { (Manifest.Permission.ReadExternalStorage, true) };
    }
}
