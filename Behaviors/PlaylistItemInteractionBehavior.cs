using System.Windows.Input;

#if ANDROID
using AView = Android.Views.View;
#endif

namespace Tunvix.Behaviors
{
    public class PlaylistItemInteractionBehavior : Behavior<View>
    {
        private View? _associatedObject;
        private double _defaultOpacity = 1;
        private double _defaultScale = 1;
        private TapGestureRecognizer? _tapGestureRecognizer;

#if ANDROID
        private AView? _platformView;
        private CancellationTokenSource? _longPressCancellationTokenSource;
        private bool _isPointerDown;
        private bool _didTriggerLongPress;
        private float _touchDownX;
        private float _touchDownY;
        private int _touchSlopPixels;
#endif

        public static readonly BindableProperty CommandProperty = BindableProperty.Create(
            nameof(Command),
            typeof(ICommand),
            typeof(PlaylistItemInteractionBehavior));

        public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create(
            nameof(CommandParameter),
            typeof(object),
            typeof(PlaylistItemInteractionBehavior));

        public static readonly BindableProperty LongPressCommandProperty = BindableProperty.Create(
            nameof(LongPressCommand),
            typeof(ICommand),
            typeof(PlaylistItemInteractionBehavior));

        public static readonly BindableProperty LongPressCommandParameterProperty = BindableProperty.Create(
            nameof(LongPressCommandParameter),
            typeof(object),
            typeof(PlaylistItemInteractionBehavior));

        public static readonly BindableProperty LongPressDurationProperty = BindableProperty.Create(
            nameof(LongPressDuration),
            typeof(int),
            typeof(PlaylistItemInteractionBehavior),
            650);

        public static readonly BindableProperty PressedOpacityProperty = BindableProperty.Create(
            nameof(PressedOpacity),
            typeof(double),
            typeof(PlaylistItemInteractionBehavior),
            0.96d);

        public static readonly BindableProperty PressedScaleProperty = BindableProperty.Create(
            nameof(PressedScale),
            typeof(double),
            typeof(PlaylistItemInteractionBehavior),
            0.99d);

        public ICommand? Command
        {
            get => (ICommand?)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        public object? CommandParameter
        {
            get => GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }

        public ICommand? LongPressCommand
        {
            get => (ICommand?)GetValue(LongPressCommandProperty);
            set => SetValue(LongPressCommandProperty, value);
        }

        public object? LongPressCommandParameter
        {
            get => GetValue(LongPressCommandParameterProperty);
            set => SetValue(LongPressCommandParameterProperty, value);
        }

        public int LongPressDuration
        {
            get => (int)GetValue(LongPressDurationProperty);
            set => SetValue(LongPressDurationProperty, value);
        }

        public double PressedOpacity
        {
            get => (double)GetValue(PressedOpacityProperty);
            set => SetValue(PressedOpacityProperty, value);
        }

        public double PressedScale
        {
            get => (double)GetValue(PressedScaleProperty);
            set => SetValue(PressedScaleProperty, value);
        }

        protected override void OnAttachedTo(View bindable)
        {
            base.OnAttachedTo(bindable);

            _associatedObject = bindable;
            _defaultOpacity = bindable.Opacity;
            _defaultScale = bindable.Scale;

            bindable.HandlerChanged += OnHandlerChanged;
            bindable.HandlerChanging += OnHandlerChanging;

            _tapGestureRecognizer = new TapGestureRecognizer();
            _tapGestureRecognizer.Tapped += OnTapped;
            bindable.GestureRecognizers.Add(_tapGestureRecognizer);

#if ANDROID
            TryAttachPlatformView();
#endif
        }

        protected override void OnDetachingFrom(View bindable)
        {
            bindable.HandlerChanged -= OnHandlerChanged;
            bindable.HandlerChanging -= OnHandlerChanging;

            if (_tapGestureRecognizer is not null)
            {
                _tapGestureRecognizer.Tapped -= OnTapped;
                bindable.GestureRecognizers.Remove(_tapGestureRecognizer);
                _tapGestureRecognizer = null;
            }

#if ANDROID
            DetachPlatformView();
#endif

            ApplyPressedState(false);
            _associatedObject = null;

            base.OnDetachingFrom(bindable);
        }

        private void OnHandlerChanged(object? sender, EventArgs e)
        {
#if ANDROID
            TryAttachPlatformView();
#endif
        }

        private void OnHandlerChanging(object? sender, HandlerChangingEventArgs e)
        {
#if ANDROID
            DetachPlatformView();
#endif
        }

        private void OnTapped(object? sender, TappedEventArgs e)
        {
#if ANDROID
            if (_didTriggerLongPress)
            {
                _didTriggerLongPress = false;
                return;
            }
#endif

            ExecuteCommand(Command, CommandParameter);
        }

#if ANDROID
        private void TryAttachPlatformView()
        {
            if (_associatedObject?.Handler?.PlatformView is not AView platformView)
            {
                return;
            }

            if (ReferenceEquals(_platformView, platformView))
            {
                return;
            }

            DetachPlatformView();

            _platformView = platformView;
            var context = platformView.Context;
            _touchSlopPixels = context is not null
                ? Android.Views.ViewConfiguration.Get(context)?.ScaledTouchSlop ?? 16
                : 16;
            platformView.Touch += OnPlatformViewTouch;
        }

        private void DetachPlatformView()
        {
            if (_platformView is null)
            {
                return;
            }

            _platformView.Touch -= OnPlatformViewTouch;
            _platformView = null;
            CancelLongPressCountdown();
            _isPointerDown = false;
            _didTriggerLongPress = false;
        }

        private void OnPlatformViewTouch(object? sender, AView.TouchEventArgs e)
        {
            if (e.Event is null)
            {
                return;
            }

            switch (e.Event.ActionMasked)
            {
                case Android.Views.MotionEventActions.Down:
                    _isPointerDown = true;
                    _didTriggerLongPress = false;
                    _touchDownX = e.Event.GetX();
                    _touchDownY = e.Event.GetY();
                    StartLongPressCountdown();
                    ApplyPressedState(true);
                    break;
                case Android.Views.MotionEventActions.Move:
                    if (_isPointerDown && HasMovedBeyondTouchSlop(e.Event))
                    {
                        CancelLongPressCountdown();
                        _isPointerDown = false;
                        ApplyPressedState(false);
                    }
                    break;
                case Android.Views.MotionEventActions.Up:
                case Android.Views.MotionEventActions.Cancel:
                case Android.Views.MotionEventActions.Outside:
                    CancelLongPressCountdown();
                    _isPointerDown = false;
                    ApplyPressedState(false);
                    break;
            }
        }

        private bool HasMovedBeyondTouchSlop(Android.Views.MotionEvent motionEvent)
        {
            var deltaX = Math.Abs(motionEvent.GetX() - _touchDownX);
            var deltaY = Math.Abs(motionEvent.GetY() - _touchDownY);
            return deltaX > _touchSlopPixels || deltaY > _touchSlopPixels;
        }

        private void StartLongPressCountdown()
        {
            CancelLongPressCountdown();

            var duration = Math.Max(LongPressDuration, 1);
            var cancellationTokenSource = new CancellationTokenSource();
            _longPressCancellationTokenSource = cancellationTokenSource;
            _ = WaitForLongPressAsync(cancellationTokenSource.Token, duration);
        }

        private async Task WaitForLongPressAsync(CancellationToken cancellationToken, int durationMilliseconds)
        {
            try
            {
                await Task.Delay(durationMilliseconds, cancellationToken);

                if (cancellationToken.IsCancellationRequested || !_isPointerDown || _didTriggerLongPress)
                {
                    return;
                }

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (cancellationToken.IsCancellationRequested || !_isPointerDown || _didTriggerLongPress)
                    {
                        return;
                    }

                    _didTriggerLongPress = true;
                    ApplyPressedState(false);
                    ExecuteCommand(LongPressCommand, LongPressCommandParameter);
                });
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void CancelLongPressCountdown()
        {
            var cancellationTokenSource = _longPressCancellationTokenSource;
            if (cancellationTokenSource is null)
            {
                return;
            }

            _longPressCancellationTokenSource = null;
            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
        }
#endif

        private void ExecuteCommand(ICommand? command, object? parameter)
        {
            var resolvedParameter = parameter ?? _associatedObject?.BindingContext;
            if (command?.CanExecute(resolvedParameter) == true)
            {
                command.Execute(resolvedParameter);
            }
        }

        private void ApplyPressedState(bool isPressed)
        {
            if (_associatedObject is null)
            {
                return;
            }

            _associatedObject.Opacity = isPressed ? PressedOpacity : _defaultOpacity;
            _associatedObject.Scale = isPressed ? PressedScale : _defaultScale;
        }
    }
}
