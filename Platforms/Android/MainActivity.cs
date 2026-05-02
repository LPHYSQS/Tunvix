using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Tunvix.Platforms.Android.Services;

namespace Tunvix
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
        {
            if (AndroidFolderPickerService.TryHandleActivityResult(requestCode, resultCode, data, this))
            {
                return;
            }

            base.OnActivityResult(requestCode, resultCode, data);
        }
    }
}
