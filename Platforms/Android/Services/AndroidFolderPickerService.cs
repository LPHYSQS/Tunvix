using Android.App;
using Android.Content;
using AndroidX.DocumentFile.Provider;
using Microsoft.Maui.ApplicationModel;

namespace Tunvix.Platforms.Android.Services
{
    public sealed record FolderSelectionResult(string TreeUri, string DisplayName);

    public interface IAndroidFolderPickerService
    {
        Task<FolderSelectionResult?> PickFolderAsync(CancellationToken cancellationToken = default);
    }

    public sealed class AndroidFolderPickerService : IAndroidFolderPickerService
    {
        private const int PickFolderRequestCode = 47281;
        private static TaskCompletionSource<FolderSelectionResult?>? _pendingRequest;

        public Task<FolderSelectionResult?> PickFolderAsync(CancellationToken cancellationToken = default)
        {
            var activity = Platform.CurrentActivity
                ?? throw new InvalidOperationException("当前没有可用的 Android 窗口，无法选择文件夹。");

            if (_pendingRequest is not null)
            {
                throw new InvalidOperationException("已有文件夹选择请求正在进行中。");
            }

            _pendingRequest = new TaskCompletionSource<FolderSelectionResult?>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            if (cancellationToken.CanBeCanceled)
            {
                cancellationToken.Register(() =>
                {
                    _pendingRequest?.TrySetCanceled(cancellationToken);
                    _pendingRequest = null;
                });
            }

            var intent = new Intent(Intent.ActionOpenDocumentTree);
            intent.AddFlags(
                ActivityFlags.GrantReadUriPermission
                | ActivityFlags.GrantPersistableUriPermission
                | ActivityFlags.GrantPrefixUriPermission);

            activity.StartActivityForResult(intent, PickFolderRequestCode);
            return _pendingRequest.Task;
        }

        internal static bool TryHandleActivityResult(
            int requestCode,
            Result resultCode,
            Intent? data,
            Activity activity)
        {
            if (requestCode != PickFolderRequestCode || _pendingRequest is null)
            {
                return false;
            }

            var taskCompletionSource = _pendingRequest;
            _pendingRequest = null;

            if (resultCode != Result.Ok || data?.Data is null)
            {
                taskCompletionSource.TrySetResult(null);
                return true;
            }

            var treeUri = data.Data;
            var persistFlags = data.Flags & (ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission);

            activity.ContentResolver?.TakePersistableUriPermission(
                treeUri,
                persistFlags | ActivityFlags.GrantReadUriPermission);

            var document = DocumentFile.FromTreeUri(activity, treeUri);
            var displayName = document?.Name ?? "已选择文件夹";

            var treeUriText = treeUri.ToString();
            if (string.IsNullOrWhiteSpace(treeUriText))
            {
                taskCompletionSource.TrySetResult(null);
                return true;
            }

            taskCompletionSource.TrySetResult(new FolderSelectionResult(treeUriText, displayName));
            return true;
        }
    }
}
