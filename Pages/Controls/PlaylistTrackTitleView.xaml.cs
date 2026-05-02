using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls.Shapes;

namespace Tunvix.Pages.Controls
{
    public partial class PlaylistTrackTitleView : ContentView
    {
        private const double MarqueeGapWidth = 24;
        private const int MarqueeStartDelayMilliseconds = 850;
        private const int MarqueeRepeatDelayMilliseconds = 400;
        private const double MarqueeDurationPerPixel = 18;
        private const uint MinMarqueeDuration = 3600;
        private const uint MaxMarqueeDuration = 9000;

        private CancellationTokenSource? _marqueeLoopCancellationTokenSource;
        private int _pendingUpdateVersion;

        public static readonly BindableProperty TextProperty = BindableProperty.Create(
            nameof(Text),
            typeof(string),
            typeof(PlaylistTrackTitleView),
            string.Empty,
            propertyChanged: OnDisplayPropertyChanged);

        public static readonly BindableProperty IsMarqueeActiveProperty = BindableProperty.Create(
            nameof(IsMarqueeActive),
            typeof(bool),
            typeof(PlaylistTrackTitleView),
            false,
            propertyChanged: OnDisplayPropertyChanged);

        public static readonly BindableProperty TitleTextColorProperty = BindableProperty.Create(
            nameof(TitleTextColor),
            typeof(Color),
            typeof(PlaylistTrackTitleView),
            Colors.Black,
            propertyChanged: OnDisplayPropertyChanged);

        public static readonly BindableProperty TitleFontSizeProperty = BindableProperty.Create(
            nameof(TitleFontSize),
            typeof(double),
            typeof(PlaylistTrackTitleView),
            17d,
            propertyChanged: OnDisplayPropertyChanged);

        public static readonly BindableProperty TitleFontAttributesProperty = BindableProperty.Create(
            nameof(TitleFontAttributes),
            typeof(FontAttributes),
            typeof(PlaylistTrackTitleView),
            FontAttributes.None,
            propertyChanged: OnDisplayPropertyChanged);

        public PlaylistTrackTitleView()
        {
            InitializeComponent();

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            SizeChanged += OnSizeChanged;
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public bool IsMarqueeActive
        {
            get => (bool)GetValue(IsMarqueeActiveProperty);
            set => SetValue(IsMarqueeActiveProperty, value);
        }

        public Color TitleTextColor
        {
            get => (Color)GetValue(TitleTextColorProperty);
            set => SetValue(TitleTextColorProperty, value);
        }

        public double TitleFontSize
        {
            get => (double)GetValue(TitleFontSizeProperty);
            set => SetValue(TitleFontSizeProperty, value);
        }

        public FontAttributes TitleFontAttributes
        {
            get => (FontAttributes)GetValue(TitleFontAttributesProperty);
            set => SetValue(TitleFontAttributesProperty, value);
        }

        private static void OnDisplayPropertyChanged(BindableObject bindable, object oldValue, object newValue) =>
            ((PlaylistTrackTitleView)bindable).ScheduleUpdate();

        private void OnLoaded(object? sender, EventArgs e) =>
            ScheduleUpdate();

        private void OnUnloaded(object? sender, EventArgs e) =>
            StopMarquee();

        private void OnSizeChanged(object? sender, EventArgs e)
        {
            UpdateViewportClip();
            ScheduleUpdate();
        }

        private void ScheduleUpdate()
        {
            var updateVersion = Interlocked.Increment(ref _pendingUpdateVersion);
            _ = RefreshPresentationAsync(updateVersion);
        }

        private async Task RefreshPresentationAsync(int updateVersion)
        {
            await Task.Delay(60);

            if (updateVersion != _pendingUpdateVersion)
            {
                return;
            }

            await MainThread.InvokeOnMainThreadAsync(UpdatePresentation);
        }

        private void UpdatePresentation()
        {
            ApplyLabelStyle(StaticTitleLabel, LineBreakMode.TailTruncation);
            ApplyLabelStyle(MarqueePrimaryLabel, LineBreakMode.NoWrap);
            ApplyLabelStyle(MarqueeSecondaryLabel, LineBreakMode.NoWrap);

            var text = Text?.Trim() ?? string.Empty;
            StaticTitleLabel.Text = text;
            MarqueePrimaryLabel.Text = text;
            MarqueeSecondaryLabel.Text = text;

            UpdateViewportClip();
            StopMarquee();

            if (string.IsNullOrWhiteSpace(text))
            {
                ShowStaticTitle();
                return;
            }

            if (!IsMarqueeActive)
            {
                ShowStaticTitle();
                return;
            }

            var viewportWidth = Width > 0
                ? Width
                : StaticTitleLabel.Width;

            if (viewportWidth <= 0)
            {
                ShowStaticTitle();
                return;
            }

            var textWidth = MarqueePrimaryLabel.Measure(double.PositiveInfinity, double.PositiveInfinity).Width;
            if (textWidth <= viewportWidth + 1)
            {
                ShowStaticTitle();
                return;
            }

            ShowMarqueeTitle();
            StartMarqueeLoop(textWidth + MarqueeGapWidth);
        }

        private void ApplyLabelStyle(Label label, LineBreakMode lineBreakMode)
        {
            label.TextColor = TitleTextColor;
            label.FontSize = TitleFontSize;
            label.FontAttributes = TitleFontAttributes;
            label.LineBreakMode = lineBreakMode;
            label.MaxLines = 1;
        }

        private void UpdateViewportClip()
        {
            var clipWidth = Width > 0
                ? Width
                : StaticTitleLabel.Width;

            var clipHeight = Height > 0
                ? Height
                : StaticTitleLabel.Height;

            if (clipWidth <= 0 || clipHeight <= 0)
            {
                return;
            }

            MarqueeViewport.Clip = new RectangleGeometry
            {
                Rect = new Rect(0, 0, clipWidth, clipHeight)
            };
        }

        private void ShowStaticTitle()
        {
            StaticTitleLabel.IsVisible = true;
            MarqueeViewport.IsVisible = false;
            MarqueeTrack.TranslationX = 0;
        }

        private void ShowMarqueeTitle()
        {
            StaticTitleLabel.IsVisible = false;
            MarqueeViewport.IsVisible = true;
            MarqueeTrack.TranslationX = 0;
        }

        private void StartMarqueeLoop(double scrollDistance)
        {
            var cancellationTokenSource = new CancellationTokenSource();
            _marqueeLoopCancellationTokenSource = cancellationTokenSource;
            _ = RunMarqueeLoopAsync(cancellationTokenSource, scrollDistance);
        }

        private async Task RunMarqueeLoopAsync(
            CancellationTokenSource cancellationTokenSource,
            double scrollDistance)
        {
            try
            {
                while (!cancellationTokenSource.Token.IsCancellationRequested)
                {
                    await Task.Delay(MarqueeStartDelayMilliseconds, cancellationTokenSource.Token);

                    var duration = (uint)Math.Clamp(
                        scrollDistance * MarqueeDurationPerPixel,
                        MinMarqueeDuration,
                        MaxMarqueeDuration);

                    await MainThread.InvokeOnMainThreadAsync(() =>
                        MarqueeTrack.TranslateToAsync(-scrollDistance, 0, duration, Easing.Linear));

                    if (cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        break;
                    }

                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        MarqueeTrack.CancelAnimations();
                        MarqueeTrack.TranslationX = 0;
                    });

                    await Task.Delay(MarqueeRepeatDelayMilliseconds, cancellationTokenSource.Token);
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                cancellationTokenSource.Dispose();

                if (ReferenceEquals(_marqueeLoopCancellationTokenSource, cancellationTokenSource))
                {
                    _marqueeLoopCancellationTokenSource = null;
                }
            }
        }

        private void StopMarquee()
        {
            var cancellationTokenSource = _marqueeLoopCancellationTokenSource;
            if (cancellationTokenSource is null)
            {
                MarqueeTrack.CancelAnimations();
                MarqueeTrack.TranslationX = 0;
                return;
            }

            _marqueeLoopCancellationTokenSource = null;
            cancellationTokenSource.Cancel();

            MarqueeTrack.CancelAnimations();
            MarqueeTrack.TranslationX = 0;
        }
    }
}
