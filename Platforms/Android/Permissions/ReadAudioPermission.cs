using Android;
using Android.OS;
using Microsoft.Maui.ApplicationModel;

namespace Tunvix.Platforms.Android.Permissions
{
    public sealed class ReadAudioPermission : Microsoft.Maui.ApplicationModel.Permissions.BasePlatformPermission
    {
        public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
            Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu
                ? new[] { (Manifest.Permission.ReadMediaAudio, true) }
                : new[] { (Manifest.Permission.ReadExternalStorage, true) };
    }
}
