namespace Tunvix.Pages.Controls
{
    public partial class RotatingAlbumArtView : ContentView
    {
        private const string RotationAnimationName = nameof(RotatingAlbumArtView);
        private const uint RotationDuration = 18000;

        private bool _isLoaded;
        private bool _isAnimating;
        private double _rotationBase;

        public static readonly BindableProperty ArtworkSourceProperty = BindableProperty.Create(
            nameof(ArtworkSource),
            typeof(ImageSource),
            typeof(RotatingAlbumArtView),
            default(ImageSource),
            propertyChanged: OnPresentationPropertyChanged);

        public static readonly BindableProperty IsPlayingProperty = BindableProperty.Create(
            nameof(IsPlaying),
            typeof(bool),
            typeof(RotatingAlbumArtView),
            false,
            propertyChanged: OnPresentationPropertyChanged);

        public RotatingAlbumArtView()
        {
            InitializeComponent();

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        public ImageSource? ArtworkSource
        {
            get => (ImageSource?)GetValue(ArtworkSourceProperty);
            set => SetValue(ArtworkSourceProperty, value);
        }

        public bool IsPlaying
        {
            get => (bool)GetValue(IsPlayingProperty);
            set => SetValue(IsPlayingProperty, value);
        }

        private bool ShouldAnimate =>
            _isLoaded
            && IsVisible
            && IsPlaying
            && ArtworkSource is not null;

        protected override void OnPropertyChanged(string? propertyName = null)
        {
            base.OnPropertyChanged(propertyName);

            if (propertyName == nameof(IsVisible))
            {
                UpdatePresentation();
            }
        }

        private static void OnPresentationPropertyChanged(
            BindableObject bindable,
            object oldValue,
            object newValue) =>
            ((RotatingAlbumArtView)bindable).UpdatePresentation();

        private void OnLoaded(object? sender, EventArgs e)
        {
            _isLoaded = true;
            UpdatePresentation();
        }

        private void OnUnloaded(object? sender, EventArgs e)
        {
            _isLoaded = false;
            StopRotation();
        }

        private void UpdatePresentation()
        {
            var dispatcher = Dispatcher;
            if (dispatcher?.IsDispatchRequired == true)
            {
                dispatcher.Dispatch(UpdatePresentation);
                return;
            }

            ArtworkImage.Source = ArtworkSource;

            if (ShouldAnimate)
            {
                StartRotation();
                return;
            }

            StopRotation();
        }

        private void StartRotation()
        {
            if (_isAnimating)
            {
                return;
            }

            _isAnimating = true;
            _rotationBase = NormalizeRotation(ArtworkBorder.Rotation);

            var animation = new Animation(
                callback: progress => ArtworkBorder.Rotation = _rotationBase + (progress * 360d),
                start: 0d,
                end: 1d,
                easing: Easing.Linear);

            animation.Commit(
                this,
                RotationAnimationName,
                rate: 16,
                length: RotationDuration,
                easing: Easing.Linear,
                finished: (_, _) =>
                {
                    ArtworkBorder.Rotation = NormalizeRotation(ArtworkBorder.Rotation);
                    _rotationBase = ArtworkBorder.Rotation;
                },
                repeat: () =>
                {
                    if (!_isAnimating || !ShouldAnimate)
                    {
                        return false;
                    }

                    ArtworkBorder.Rotation = NormalizeRotation(ArtworkBorder.Rotation);
                    _rotationBase = ArtworkBorder.Rotation;
                    return true;
                });
        }

        private void StopRotation()
        {
            if (!_isAnimating)
            {
                this.AbortAnimation(RotationAnimationName);
                ArtworkBorder.Rotation = NormalizeRotation(ArtworkBorder.Rotation);
                return;
            }

            _isAnimating = false;
            this.AbortAnimation(RotationAnimationName);
            ArtworkBorder.Rotation = NormalizeRotation(ArtworkBorder.Rotation);
        }

        private static double NormalizeRotation(double rotation)
        {
            var normalized = rotation % 360d;
            return normalized < 0 ? normalized + 360d : normalized;
        }
    }
}
