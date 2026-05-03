namespace Tunvix.Pages.Controls
{
    public partial class NowPlayingIndicatorView : ContentView
    {
        private const string IndicatorAnimationName = nameof(NowPlayingIndicatorView);
        private bool _isAnimationRunning;
        private bool _isLoaded;

        public static readonly BindableProperty TrackKeyProperty = BindableProperty.Create(
            nameof(TrackKey),
            typeof(string),
            typeof(NowPlayingIndicatorView),
            string.Empty,
            propertyChanged: OnPlaybackIndicatorPropertyChanged);

        public static readonly BindableProperty CurrentTrackKeyProperty = BindableProperty.Create(
            nameof(CurrentTrackKey),
            typeof(string),
            typeof(NowPlayingIndicatorView),
            string.Empty,
            propertyChanged: OnPlaybackIndicatorPropertyChanged);

        public static readonly BindableProperty IsPlaybackActiveProperty = BindableProperty.Create(
            nameof(IsPlaybackActive),
            typeof(bool),
            typeof(NowPlayingIndicatorView),
            false,
            propertyChanged: OnPlaybackIndicatorPropertyChanged);

        public NowPlayingIndicatorView()
        {
            InitializeComponent();

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        public string TrackKey
        {
            get => (string)GetValue(TrackKeyProperty);
            set => SetValue(TrackKeyProperty, value);
        }

        public string CurrentTrackKey
        {
            get => (string)GetValue(CurrentTrackKeyProperty);
            set => SetValue(CurrentTrackKeyProperty, value);
        }

        public bool IsPlaybackActive
        {
            get => (bool)GetValue(IsPlaybackActiveProperty);
            set => SetValue(IsPlaybackActiveProperty, value);
        }

        private bool IsCurrentTrack =>
            !string.IsNullOrWhiteSpace(TrackKey)
            && string.Equals(TrackKey, CurrentTrackKey, StringComparison.Ordinal);

        private bool ShouldAnimate => _isLoaded && IsCurrentTrack && IsPlaybackActive;

        private static void OnPlaybackIndicatorPropertyChanged(
            BindableObject bindable,
            object oldValue,
            object newValue) =>
            ((NowPlayingIndicatorView)bindable).UpdateVisualState();

        private void OnLoaded(object? sender, EventArgs e)
        {
            _isLoaded = true;
            UpdateVisualState();
        }

        private void OnUnloaded(object? sender, EventArgs e)
        {
            _isLoaded = false;
            StopAnimation();
        }

        private void UpdateVisualState()
        {
            if (Dispatcher.IsDispatchRequired)
            {
                Dispatcher.Dispatch(UpdateVisualState);
                return;
            }

            if (!IsCurrentTrack)
            {
                StopAnimation();
                ApplyInactiveState();
                return;
            }

            if (ShouldAnimate)
            {
                ApplyAnimatedBaseState();
                StartAnimation();
                return;
            }

            StopAnimation();
            ApplyPausedState();
        }

        private void StartAnimation()
        {
            if (_isAnimationRunning)
            {
                return;
            }

            _isAnimationRunning = true;

            var animation = new Animation();
            animation.Add(0.00, 0.38, new Animation(v => BarOne.ScaleY = v, 0.42, 0.96, Easing.SinInOut));
            animation.Add(0.38, 0.76, new Animation(v => BarOne.ScaleY = v, 0.96, 0.48, Easing.SinInOut));
            animation.Add(0.76, 1.00, new Animation(v => BarOne.ScaleY = v, 0.48, 0.72, Easing.SinInOut));

            animation.Add(0.00, 0.26, new Animation(v => BarTwo.ScaleY = v, 0.74, 0.40, Easing.SinInOut));
            animation.Add(0.26, 0.64, new Animation(v => BarTwo.ScaleY = v, 0.40, 1.00, Easing.SinInOut));
            animation.Add(0.64, 1.00, new Animation(v => BarTwo.ScaleY = v, 1.00, 0.58, Easing.SinInOut));

            animation.Add(0.00, 0.34, new Animation(v => BarThree.ScaleY = v, 0.52, 0.88, Easing.SinInOut));
            animation.Add(0.34, 0.70, new Animation(v => BarThree.ScaleY = v, 0.88, 0.36, Easing.SinInOut));
            animation.Add(0.70, 1.00, new Animation(v => BarThree.ScaleY = v, 0.36, 0.64, Easing.SinInOut));

            animation.Commit(
                this,
                IndicatorAnimationName,
                rate: 16,
                length: 880,
                easing: Easing.Linear,
                finished: (_, _) =>
                {
                    if (!_isAnimationRunning)
                    {
                        ApplyPausedState();
                    }
                },
                repeat: () => ShouldAnimate);
        }

        private void StopAnimation()
        {
            if (!_isAnimationRunning)
            {
                this.AbortAnimation(IndicatorAnimationName);
                return;
            }

            _isAnimationRunning = false;
            this.AbortAnimation(IndicatorAnimationName);
        }

        private void ApplyInactiveState()
        {
            IndicatorRoot.Opacity = 0;
            ApplyBarScale(0.45, 0.72, 0.56);
        }

        private void ApplyPausedState()
        {
            IndicatorRoot.Opacity = 0.92;
            ApplyBarScale(0.48, 0.78, 0.60);
        }

        private void ApplyAnimatedBaseState()
        {
            IndicatorRoot.Opacity = 1;
            ApplyBarScale(0.42, 0.74, 0.52);
        }

        private void ApplyBarScale(double first, double second, double third)
        {
            BarOne.ScaleY = first;
            BarTwo.ScaleY = second;
            BarThree.ScaleY = third;
        }
    }
}
