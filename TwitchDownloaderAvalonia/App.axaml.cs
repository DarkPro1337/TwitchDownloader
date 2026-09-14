using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using TwitchDownloaderAvalonia.ViewModels;
using TwitchDownloaderAvalonia.Views;

namespace TwitchDownloaderAvalonia
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
#if DEBUG
            this.AttachDeveloperTools();
#endif
        }

        public override void OnFrameworkInitializationCompleted()
        {
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);
            CoreLicensor.EnsureFilesExist(null);

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var settings = new SettingsService();
                LocalizationService.Current.SetCulture(settings.Current.GuiCulture);
                var themeStartup = ThemeService.Initialize(settings);
                var status = new AppStatus(settings);
                var ffmpeg = new FfmpegService();
                var files = new FileDialogService();
                var dialogs = new DialogService(settings, files);
                var collision = new FileCollisionService(settings, dialogs);
                var cacheCleaner = new AbandonedVideoCacheService(dialogs);
                var thumbnails = new ThumbnailService();
                var queue = new QueueService(settings, status, dialogs);
                var updates = new UpdateCheckService();

                var vm = new MainWindowViewModel(settings, status, ffmpeg, dialogs, files, collision, cacheCleaner, thumbnails, queue, updates);
                var mainWindow = new MainWindow { DataContext = vm };

                void OnOpened(object? sender, EventArgs e)
                {
                    mainWindow.Opened -= OnOpened;
                    _ = InitializeMainWindowAsync(vm, dialogs, themeStartup);
                }

                mainWindow.Opened += OnOpened;
                dialogs.SetOwner(mainWindow);
                files.SetOwner(mainWindow);
                desktop.MainWindow = mainWindow;
            }

            base.OnFrameworkInitializationCompleted();
        }

        private static async Task InitializeMainWindowAsync(
            MainWindowViewModel vm,
            DialogService dialogs,
            ThemeStartupResult themeStartup)
        {
            try
            {
                await ShowThemeStartupDialogsAsync(dialogs, themeStartup);
                await vm.InitializeAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[app] startup initialize failed: {ex}");
            }
        }

        private static async Task ShowThemeStartupDialogsAsync(DialogService dialogs, ThemeStartupResult themeStartup)
        {
            if (themeStartup.MissingPackFileName is { } fileName)
            {
                await dialogs.ShowMessageAsync(
                    Loc.Get("settings.theme_not_found"),
                    Loc.Get("settings.theme_not_found_message", fileName));
            }

            if (themeStartup.ThemesWriteFailed)
            {
                var message = Loc.Get("settings.themes_failed_to_write");
                await dialogs.ShowMessageAsync(message, message);
            }
        }
    }
}