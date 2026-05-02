using Tunvix.PageModels;
using Tunvix.Services;

namespace Tunvix
{
    public partial class App : Application
    {
        private readonly MainPageModel _mainPageModel;

        public App(ThemeService themeService, MainPageModel mainPageModel)
        {
            InitializeComponent();
            themeService.ApplyStoredTheme();
            _mainPageModel = mainPageModel;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new AppShell());
            window.Stopped += OnWindowStopped;
            window.Destroying += OnWindowDestroying;

            return window;
        }

        private void OnWindowStopped(object? sender, EventArgs e) =>
            _mainPageModel.PersistPlaybackState(force: true);

        private void OnWindowDestroying(object? sender, EventArgs e) =>
            _mainPageModel.PersistPlaybackState(force: true);
    }
}
