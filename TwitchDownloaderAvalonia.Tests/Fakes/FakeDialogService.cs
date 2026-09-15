using TwitchDownloaderAvalonia.Abstractions;
using TwitchDownloaderAvalonia.Models;
using TwitchDownloaderAvalonia.Services;

namespace TwitchDownloaderAvalonia.Tests.Fakes
{
    internal sealed class FakeDialogService : IDialogService
    {
        public List<(string Title, string Message)> Errors { get; } = [];
        public List<(string Title, string Message)> Messages { get; } = [];
        public List<(string Title, string Message)> Confirms { get; } = [];
        public List<string> Copied { get; } = [];

        public bool ConfirmResult { get; init; }

        public Task ShowErrorAsync(string title, string message)
        {
            Errors.Add((title, message));
            return Task.CompletedTask;
        }

        public Task ShowMessageAsync(string title, string message)
        {
            Messages.Add((title, message));
            return Task.CompletedTask;
        }

        public Task<bool> ShowConfirmAsync(string title, string message)
        {
            Confirms.Add((title, message));
            return Task.FromResult(ConfirmResult);
        }

        public Task CopyTextAsync(string text)
        {
            Copied.Add(text);
            return Task.CompletedTask;
        }

        public Task<EnqueueOptions?> ShowEnqueueOptionsAsync(bool hasVods, bool hasRecordingVods)
        {
            return Task.FromResult<EnqueueOptions?>(null);
        }

        public Task ShowUrlListAsync(ThumbnailService thumbnails, QueueEnqueueService enqueue) => Task.CompletedTask;

        public CollisionPromptResult PromptCollision(string fileName, string fullPath)
        {
            return new CollisionPromptResult(CollisionChoice.Cancel, false);
        }

        public DirectoryInfo[] PromptAbandonedVideoCaches(DirectoryInfo[] directories) => [];
    }

    internal sealed class FakeFileDialogService : IFileDialogService
    {
        public Task<string?> SaveFileAsync(string suggestedFileName, string filterName, string extension)
        {
            return Task.FromResult<string?>(null);
        }

        public Task<string?> OpenFileAsync(string title, string filterName, IReadOnlyList<string> patterns)
        {
            return Task.FromResult<string?>(null);
        }

        public Task<string?> PickFolderAsync(string title, string? startPath = null)
        {
            return Task.FromResult<string?>(null);
        }
    }
}
