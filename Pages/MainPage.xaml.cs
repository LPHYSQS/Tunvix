using System.ComponentModel;
using Tunvix.PageModels;

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
    }
}
