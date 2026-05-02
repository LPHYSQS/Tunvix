using Tunvix.Services;

namespace Tunvix
{
    public partial class App : Application
    {
        public App(ThemeService themeService)
        {
            InitializeComponent();
            themeService.ApplyStoredTheme();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}
