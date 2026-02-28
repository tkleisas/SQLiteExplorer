using System.Reflection;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SQLiteExplorer.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        LoadVersionInfo();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void LoadVersionInfo()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";
        var runtime = RuntimeInformation.FrameworkDescription;
        var os = RuntimeInformation.OSDescription;
        var arch = RuntimeInformation.ProcessArchitecture.ToString();
        var platform = $"{os} ({arch})";

        var versionText = this.FindControl<TextBlock>("VersionText");
        var platformText = this.FindControl<TextBlock>("PlatformText");
        var runtimeText = this.FindControl<TextBlock>("RuntimeText");

        if (versionText != null)
            versionText.Text = $"Version {version}";

        if (platformText != null)
            platformText.Text = platform;

        if (runtimeText != null)
            runtimeText.Text = runtime;
    }

    private void OnCloseClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }
}
