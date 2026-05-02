namespace Tunvix.Services
{
    public class ThemeService
    {
        private const string ThemePreferenceKey = "app_theme";
        private const string DarkThemeValue = "dark";
        private const string LightThemeValue = "light";

        public AppTheme CurrentTheme { get; private set; } = AppTheme.Dark;

        public bool IsDarkTheme => CurrentTheme == AppTheme.Dark;

        public void ApplyStoredTheme()
        {
            var storedTheme = Preferences.Default.Get(ThemePreferenceKey, DarkThemeValue);
            var theme = storedTheme == LightThemeValue ? AppTheme.Light : AppTheme.Dark;
            ApplyTheme(theme, persist: false);
        }

        public AppTheme ToggleTheme()
        {
            var nextTheme = IsDarkTheme ? AppTheme.Light : AppTheme.Dark;
            return ApplyTheme(nextTheme, persist: true);
        }

        private AppTheme ApplyTheme(AppTheme theme, bool persist)
        {
            CurrentTheme = theme;

            if (persist)
            {
                Preferences.Default.Set(
                    ThemePreferenceKey,
                    theme == AppTheme.Light ? LightThemeValue : DarkThemeValue);
            }

            if (Application.Current is not null)
            {
                Application.Current.UserAppTheme = theme;
            }

            return theme;
        }
    }
}
