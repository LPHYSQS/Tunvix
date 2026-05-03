using System.ComponentModel;
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

        private async Task<PlaylistLoadStrategy?> OnPlaylistLoadStrategyRequestedAsync(PlaylistImportSource importSource)
        {
            var action = await DisplayActionSheetAsync(
                importSource == PlaylistImportSource.DeviceLibrary
                    ? "当前播放列表已有歌曲，选择设备音频加载方式"
                    : "当前播放列表已有歌曲，选择文件夹音频加载方式",
                "取消",
                null,
                "增量加载",
                "完全覆盖");

            return action switch
            {
                "增量加载" => PlaylistLoadStrategy.Incremental,
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
    }
}
