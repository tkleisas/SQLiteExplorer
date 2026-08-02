using System;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using SQLiteExplorer.Lib.Services;

namespace SQLiteExplorer.Lib.Views;

/// <summary>
/// Settings dialog for the built-in OpenAI-compatible LLM service (standalone app).
/// Hosts that inject their own <see cref="ILlmService"/> never show this dialog —
/// they handle <c>MainWindowViewModel.LlmSettingsRequested</c> instead.
/// </summary>
public partial class LlmSettingsDialog : Window
{
    private readonly LlmSettings _settings;

    public bool SettingsSaved { get; private set; }

    public LlmSettingsDialog()
    {
        _settings = LlmSettings.Load();
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Title = "AI Settings";
        Width = 460;
        Height = 500;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var enabledCheck = new CheckBox
        {
            Content = "Enable AI features",
            IsChecked = _settings.Enabled,
            Margin = new(0, 0, 0, 12)
        };
        var endpointBox = new TextBox { Text = _settings.Endpoint, PlaceholderText = "https://api.openai.com/v1", Margin = new(0, 0, 0, 8) };
        var apiKeyBox = new TextBox { Text = _settings.ApiKey, PlaceholderText = "API key (empty for local models)", PasswordChar = '●', Margin = new(0, 0, 0, 8) };
        var modelBox = new TextBox { Text = _settings.Model, PlaceholderText = "Model (e.g. gpt-4o-mini, deepseek-chat, codellama)", Margin = new(0, 0, 0, 8) };
        var temperatureBox = new NumericUpDown { Value = (decimal)_settings.Temperature, Minimum = 0, Maximum = 2, Increment = 0.1m, Margin = new(0, 0, 0, 12) };

        var testResultLabel = new TextBlock { Text = string.Empty, TextWrapping = TextWrapping.Wrap, Margin = new(0, 0, 0, 12) };

        var testButton = new Button { Content = "Test connection", HorizontalAlignment = HorizontalAlignment.Left, Padding = new(16, 6), Margin = new(0, 0, 0, 8) };
        testButton.Click += async (s, e) =>
        {
            testButton.IsEnabled = false;
            testResultLabel.Text = "Testing…";
            testResultLabel.Foreground = Brushes.Gray;
            try
            {
                var service = new OpenAiCompatibleLlmService(BuildSettings(enabledCheck, endpointBox, apiKeyBox, modelBox, temperatureBox));
                var reply = await service.ChatAsync(
                    "You are a connectivity probe. Reply with exactly: ok",
                    "ping");
                testResultLabel.Text = string.IsNullOrWhiteSpace(reply)
                    ? "Connected, but the model returned an empty reply."
                    : "Connection successful.";
                testResultLabel.Foreground = Brushes.Green;
            }
            catch (Exception ex)
            {
                testResultLabel.Text = ex.Message;
                testResultLabel.Foreground = Brushes.Red;
            }
            finally
            {
                testButton.IsEnabled = true;
            }
        };

        var saveButton = new Button { Content = "Save", HorizontalAlignment = HorizontalAlignment.Right, Padding = new(24, 8) };
        saveButton.Click += (s, e) =>
        {
            var updated = BuildSettings(enabledCheck, endpointBox, apiKeyBox, modelBox, temperatureBox);
            updated.Save();
            SettingsSaved = true;
            Close();
        };

        var cancelButton = new Button { Content = "Cancel", HorizontalAlignment = HorizontalAlignment.Right, Margin = new(8, 0, 0, 0), Padding = new(24, 8) };
        cancelButton.Click += (s, e) => Close();

        Content = new Panel
        {
            Margin = new(16),
            Children =
            {
                new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = "Works with any OpenAI-compatible endpoint: OpenAI, OpenRouter, DeepSeek, Ollama, LM Studio.",
                            TextWrapping = TextWrapping.Wrap,
                            Opacity = 0.7,
                            Margin = new(0, 0, 0, 12)
                        },
                        enabledCheck,
                        new TextBlock { Text = "Endpoint", FontWeight = FontWeight.SemiBold, Margin = new(0, 0, 0, 4) },
                        endpointBox,
                        new TextBlock { Text = "API Key", FontWeight = FontWeight.SemiBold, Margin = new(0, 0, 0, 4) },
                        apiKeyBox,
                        new TextBlock { Text = "Model", FontWeight = FontWeight.SemiBold, Margin = new(0, 0, 0, 4) },
                        modelBox,
                        new TextBlock { Text = "Temperature", FontWeight = FontWeight.SemiBold, Margin = new(0, 0, 0, 4) },
                        temperatureBox,
                        testButton,
                        testResultLabel,
                        new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            HorizontalAlignment = HorizontalAlignment.Right,
                            Children = { saveButton, cancelButton }
                        }
                    }
                }
            }
        };
    }

    private static LlmSettings BuildSettings(
        CheckBox enabledCheck,
        TextBox endpointBox,
        TextBox apiKeyBox,
        TextBox modelBox,
        NumericUpDown temperatureBox)
    {
        return new LlmSettings
        {
            Enabled = enabledCheck.IsChecked == true,
            Endpoint = endpointBox.Text?.Trim() ?? string.Empty,
            ApiKey = apiKeyBox.Text?.Trim() ?? string.Empty,
            Model = modelBox.Text?.Trim() ?? string.Empty,
            Temperature = (double)(temperatureBox.Value ?? 0.2m)
        };
    }
}
