namespace TwitchDownloaderAvalonia.ViewModels
{
    public interface IBusyPage
    {
        bool IsBusy { get; }
        AppStatus AppStatus { get; }
    }
}
