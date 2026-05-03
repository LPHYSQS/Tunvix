using System.ComponentModel;
using Microsoft.Maui.Controls;
using Tunvix.Models;
using Tunvix.PageModels;
using Tunvix.Services;

namespace Tunvix.Pages
{
    public partial class MainPage : ContentPage
    {
        private readonly MainPageModel _model;
        private bool _isAnimatingDrawer;

        public MainPage(MainPageModel model)
        {
            InitializeComponent();
            _model = model;
            BindingContext = model;

            Loaded += OnPageLoaded;
            SizeChanged += OnPageSizeChanged;
            _model.PropertyChanged += OnModelPropertyChanged;
            _model.LocateCurrentTrackRequested += OnLocateCurrentTrackRequested;
            _model.PlaylistLoadStrategyRequested += OnPlaylistLoadStrategyRequestedAsync;
            _model.TrackRemovalConfirmationRequested += OnTrackRemovalConfirmationRequestedAsync;
            _model.FeedbackRequested += OnFeedbackRequestedAsync;
        }

        private async void OnPageLoaded(object? sender, EventArgs e)
        {
            await _model.InitializeAsync();
            UpdateDrawerMetrics();
            InitializeDrawerVisualState();
        }

        private void OnPageSizeChanged(object? sender, EventArgs e)
        {
            UpdateDrawerMetrics();
        }

        private async void OnModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainPageModel.IsPlaylistDrawerOpen))
            {
                await AnimatePlaylistDrawerAsync(_model.IsPlaylistDrawerOpen);
            }
        }

        private void UpdateDrawerMetrics()
        {
            if (Width <= 0)
            {
                return;
            }

            var drawerWidth = Math.Min(Width * 0.82, 380);
            PlaylistDrawer.WidthRequest = drawerWidth;

            if (!_model.IsPlaylistDrawerOpen && !_isAnimatingDrawer)
            {
                PlaylistDrawer.TranslationX = -GetDrawerHiddenOffset();
            }
        }

        private void InitializeDrawerVisualState()
        {
            PlaylistOverlay.IsVisible = false;
            PlaylistOverlay.InputTransparent = true;
            PlaylistScrim.Opacity = 0;
            PlaylistDrawer.TranslationX = -GetDrawerHiddenOffset();
        }

        private double GetDrawerHiddenOffset()
        {
            var drawerWidth = PlaylistDrawer.WidthRequest > 0
                ? PlaylistDrawer.WidthRequest
                : 320;

            return drawerWidth + 48;
        }

        private async Task AnimatePlaylistDrawerAsync(bool shouldOpen)
        {
            if (_isAnimatingDrawer)
            {
                return;
            }

            _isAnimatingDrawer = true;

            try
            {
                UpdateDrawerMetrics();

                if (shouldOpen)
                {
                    PlaylistOverlay.IsVisible = true;
                    PlaylistOverlay.InputTransparent = false;
                    PlaylistDrawer.TranslationX = -GetDrawerHiddenOffset();

                    await Task.WhenAll(
                        PlaylistScrim.FadeToAsync(1, 280, Easing.CubicOut),
                        PlaylistDrawer.TranslateToAsync(0, 0, 320, Easing.CubicOut));
                }
                else
                {
                    await Task.WhenAll(
                        PlaylistScrim.FadeToAsync(0, 220, Easing.CubicIn),
                        PlaylistDrawer.TranslateToAsync(-GetDrawerHiddenOffset(), 0, 260, Easing.CubicIn));

                    PlaylistOverlay.InputTransparent = true;
                    PlaylistOverlay.IsVisible = false;
                }
            }
            finally
            {
                _isAnimatingDrawer = false;
            }
        }

        private void OnLocateCurrentTrackRequested(MusicTrack track)
        {
            if (PlaylistCollectionView.ItemsSource is null)
            {
                return;
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                PlaylistCollectionView.ScrollTo(
                    track,
                    position: ScrollToPosition.Center,
                    animate: false);
            });
        }

        private async void OnPlaylistSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (e.CurrentSelection.FirstOrDefault() is not MusicTrack track)
                {
                    return;
                }

                await _model.SelectTrackCommand.ExecuteAsync(track);
            }
            finally
            {
                if (sender is CollectionView collectionView)
                {
                    collectionView.SelectedItem = null;
                }
            }
        }

        private async Task<PlaylistLoadStrategy?> OnPlaylistLoadStrategyRequestedAsync(PlaylistImportSource importSource)
        {
            var action = await DisplayActionSheetAsync(
                importSource == PlaylistImportSource.DeviceLibrary
                    ? "当前播放列表已有歌曲，请选择导入本机音频的方式"
                    : "当前播放列表已有歌曲，请选择导入文件夹音频的方式",
                "取消",
                null,
                "增量导入",
                "完全覆盖");

            return action switch
            {
                "增量导入" => PlaylistLoadStrategy.Incremental,
                "完全覆盖" => PlaylistLoadStrategy.ReplaceAll,
                _ => null
            };
        }

        private async Task OnFeedbackRequestedAsync(string message)
        {
            if (OperatingSystem.IsWindows())
            {
                await AppShell.DisplaySnackbarAsync(message);
                return;
            }

            await AppShell.DisplayToastAsync(message);
        }

        private Task<bool> OnTrackRemovalConfirmationRequestedAsync(MusicTrack track)
        {
            var isCurrentTrack = !string.IsNullOrWhiteSpace(_model.CurrentPlaybackTrackKey)
                && string.Equals(track.TrackKey, _model.CurrentPlaybackTrackKey, StringComparison.Ordinal);

            if (!isCurrentTrack && _model.SelectedTrack is not null)
            {
                isCurrentTrack = string.Equals(track.TrackKey, _model.SelectedTrack.TrackKey, StringComparison.Ordinal)
                    || (!string.IsNullOrWhiteSpace(track.SourceUri)
                        && string.Equals(track.SourceUri, _model.SelectedTrack.SourceUri, StringComparison.Ordinal));
            }

            var message = isCurrentTrack
                ? "是否从播放列表中移除当前歌曲？"
                : string.IsNullOrWhiteSpace(track.Title)
                    ? "是否从播放列表中移除这首歌曲？"
                    : $"是否从播放列表中移除《{track.Title}》？";

            return DisplayAlertAsync(
                "移除歌曲",
                message,
                "确认",
                "取消");
        }
    }
}
