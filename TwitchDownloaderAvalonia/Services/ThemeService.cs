using Avalonia.Styling;

namespace TwitchDownloaderAvalonia.Services
{
    public static class ThemeService
    {
        public const string SYSTEM = "System";
        public const string LIGHT = "Light";
        public const string DARK = "Dark";

        internal static ThemePackCatalog Catalog { get; } = new(Path.Combine(AppContext.BaseDirectory, "Themes"));

        public static IReadOnlyList<ThemePackInfo> ScanPackInfos() => Catalog.ScanPackInfos();

        public static IReadOnlyList<string> GetLightThemeOptions()
        {
            var names = new List<string> { LIGHT };
            foreach (var pack in ScanPackInfos())
            {
                if (!pack.IsDark)
                    names.Add(pack.Name);
            }

            return names;
        }

        public static IReadOnlyList<string> GetDarkThemeOptions()
        {
            var names = new List<string> { DARK };
            foreach (var pack in ScanPackInfos())
            {
                if (pack.IsDark)
                    names.Add(pack.Name);
            }

            return names;
        }

        public static ThemeStartupResult Initialize(SettingsService settings)
        {
            bool writeOk;
            try
            {
                writeOk = Catalog.EnsureIncludedPacks();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[themes] failed to write included packs: {ex.Message}");
                writeOk = false;
            }

            var current = settings.Current;
            var ui = current.Ui;
            var prefs = Normalize(ui.Theme, ui.LightTheme, ui.DarkTheme, ScanPackInfos());
            if (prefs.MissingPackFiles.Count > 0 || prefs.MigratedFromPack)
            {
                ui.Theme = prefs.Mode;
                ui.LightTheme = prefs.LightTheme;
                ui.DarkTheme = prefs.DarkTheme;
                settings.Save();
            }

            Apply(prefs);
            var missing = prefs.MissingPackFiles.Count != 0
                ? string.Join(", ", prefs.MissingPackFiles)
                : null;

            return new ThemeStartupResult(!writeOk, missing);
        }

        public static ThemePreferences Normalize(
            string? mode,
            string? lightTheme,
            string? darkTheme,
            IReadOnlyList<ThemePackInfo>? packs = null)
        {
            packs ??= ScanPackInfos();
            var missing = new List<string>();
            var migrated = false;
            var lightPacks = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var darkPacks = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var pack in packs)
            {
                if (pack.IsDark)
                    darkPacks[pack.Name] = pack.Name;
                else
                    lightPacks[pack.Name] = pack.Name;
            }

            if (TryMatchPack(mode, lightPacks, darkPacks, out var legacyPack, out var legacyIsDark))
            {
                migrated = true;
                if (legacyIsDark)
                {
                    mode = DARK;
                    darkTheme = legacyPack;
                }
                else
                {
                    mode = LIGHT;
                    lightTheme = legacyPack;
                }
            }
            else if (EqualsMode(mode, LIGHT))
            {
                mode = LIGHT;
            }
            else if (EqualsMode(mode, DARK))
            {
                mode = DARK;
            }
            else if (string.IsNullOrWhiteSpace(mode) || EqualsMode(mode, SYSTEM))
            {
                mode = SYSTEM;
            }
            else
            {
                missing.Add($"{mode}.json");
                mode = SYSTEM;
            }

            lightTheme = ResolveNamedTheme(lightTheme, LIGHT, lightPacks, missing);
            darkTheme = ResolveNamedTheme(darkTheme, DARK, darkPacks, missing);
            return new ThemePreferences(mode, lightTheme, darkTheme, missing, migrated);
        }

        public static ThemeVariant GetRequestedVariant(string? mode)
        {
            return EqualsMode(mode, LIGHT)
                ? ThemeVariant.Light
                : EqualsMode(mode, DARK)
                    ? ThemeVariant.Dark
                    : ThemeVariant.Default;
        }

        public static void Apply(AppSettings settings)
        {
            Apply(Normalize(settings.Ui.Theme, settings.Ui.LightTheme, settings.Ui.DarkTheme));
        }

        public static void Apply(string? mode, string? lightTheme = null, string? darkTheme = null)
        {
            Apply(Normalize(mode, lightTheme, darkTheme));
        }

        public static void Apply(ThemePreferences prefs)
        {
            if (Application.Current is null)
                return;

            // Overlay Light/Dark dictionaries so RequestedThemeVariant.Default still follows the OS.
            SetOverlay(ThemeVariant.Light, prefs.LightTheme, LIGHT);
            SetOverlay(ThemeVariant.Dark, prefs.DarkTheme, DARK);

            Application.Current.RequestedThemeVariant = GetRequestedVariant(prefs.Mode);
        }

        private static void SetOverlay(ThemeVariant variant, string themeName, string builtinName)
        {
            var app = Application.Current!;
            app.Resources.ThemeDictionaries.Remove(variant);
            if (themeName.Equals(builtinName, StringComparison.OrdinalIgnoreCase))
                return;

            if (Catalog.FindPackPath(themeName) is not { } path || !ThemePackLoader.TryLoadFile(path, themeName, out var pack) || pack is null)
                return;

            app.Resources.ThemeDictionaries[variant] = pack.ToResourceDictionary();
        }

        private static string ResolveNamedTheme(
            string? theme,
            string builtin,
            Dictionary<string, string> packs,
            List<string> missing)
        {
            if (string.IsNullOrWhiteSpace(theme) || theme.Equals(builtin, StringComparison.OrdinalIgnoreCase))
                return builtin;

            if (packs.TryGetValue(theme, out var canonical))
                return canonical;

            missing.Add($"{theme}.json");
            return builtin;
        }

        private static bool TryMatchPack(
            string? name,
            Dictionary<string, string> lightPacks,
            Dictionary<string, string> darkPacks,
            out string packName,
            out bool isDark)
        {
            packName = string.Empty;
            isDark = false;
            if (string.IsNullOrWhiteSpace(name) || EqualsMode(name, SYSTEM) || EqualsMode(name, LIGHT) || EqualsMode(name, DARK))
                return false;

            if (lightPacks.TryGetValue(name, out var light))
            {
                packName = light;
                return true;
            }

            if (darkPacks.TryGetValue(name, out var dark))
            {
                packName = dark;
                isDark = true;
                return true;
            }

            return false;
        }

        private static bool EqualsMode(string? value, string mode)
        {
            return value is not null && value.Equals(mode, StringComparison.OrdinalIgnoreCase);
        }
    }

    public readonly record struct ThemePackInfo(string Name, bool IsDark);

    public readonly record struct ThemePreferences(
        string Mode,
        string LightTheme,
        string DarkTheme,
        IReadOnlyList<string> MissingPackFiles,
        bool MigratedFromPack);

    public readonly record struct ThemeStartupResult(bool ThemesWriteFailed, string? MissingPackFileName);
}
