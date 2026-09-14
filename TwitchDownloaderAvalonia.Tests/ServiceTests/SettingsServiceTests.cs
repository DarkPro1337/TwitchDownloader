using System.Globalization;
using Avalonia.Data;
using TwitchDownloaderAvalonia.Converters;
using TwitchDownloaderAvalonia.Services;

namespace TwitchDownloaderAvalonia.Tests.ServiceTests
{
    public class SettingsServiceTests
    {
        [Fact]
        public void SaveReplacesFileAtomically()
        {
            var directory = Path.Combine(Path.GetTempPath(), "TwitchDownloaderTests", Guid.NewGuid().ToString("N"));
            var path = Path.Combine(directory, "avalonia-settings.json");
            var service = new SettingsService(path);
            service.Current.General.OAuth = "token-one";
            service.Save();
            service.Current.General.OAuth = "token-two";
            service.Save();

            var json = File.ReadAllText(path);
            Assert.Contains("token-two", json);
            Assert.DoesNotContain("token-one", json);
            Assert.False(File.Exists(path + ".tmp"));
        }

        [Fact]
        public void SaveWritesCategorizedSettings()
        {
            var directory = Path.Combine(Path.GetTempPath(), "TwitchDownloaderTests", Guid.NewGuid().ToString("N"));
            var path = Path.Combine(directory, "avalonia-settings.json");
            var service = new SettingsService(path);
            service.Current.Render.Width = 800;
            service.Current.General.OAuth = "abc";
            service.Save();

            var json = File.ReadAllText(path);
            Assert.Contains("\"Render\"", json);
            Assert.Contains("\"Width\": 800", json);
            Assert.Contains("\"General\"", json);
        }

        [Fact]
        public void ResetToDefaultsRaisesEvent()
        {
            var directory = Path.Combine(Path.GetTempPath(), "TwitchDownloaderTests", Guid.NewGuid().ToString("N"));
            var path = Path.Combine(directory, "avalonia-settings.json");
            var service = new SettingsService(path);
            service.Current.Render.Width = 1234;
            var raised = false;
            service.DefaultsRestored += OnDefaultsRestored;
            service.ResetToDefaults();

            Assert.True(raised);
            Assert.Equal(700, service.Current.Render.Width);

            void OnDefaultsRestored(object? sender, EventArgs args)
            {
                raised = sender == service && args == EventArgs.Empty;
            }
        }
    }

    public class DecimalConverterTests
    {
        [Fact]
        public void IntConverterDoesNotWriteNullBackAsZero()
        {
            var result = IntDecimalConverter.Instance.ConvertBack(null, typeof(int), null, CultureInfo.InvariantCulture);
            Assert.Equal(BindingOperations.DoNothing, result);
        }

        [Fact]
        public void DoubleConverterDoesNotWriteNullBackAsZero()
        {
            var result = DoubleDecimalConverter.Instance.ConvertBack(null, typeof(double), null, CultureInfo.InvariantCulture);
            Assert.Equal(BindingOperations.DoNothing, result);
        }
    }
}
