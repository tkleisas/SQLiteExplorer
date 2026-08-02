using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using SQLiteExplorer.Lib.Converters;
using SQLiteExplorer.Lib.ViewModels;

namespace SQLiteExplorer.Lib.Views;

/// <summary>
/// Report wizard: 1. details, 2. SQL query (optionally AI-generated), 3. preview → save.
/// Follows the code-built dialog idiom of the connection dialogs.
/// </summary>
public partial class ReportWizardDialog : Window
{
    private static readonly HasErrorToColorConverter ErrorColor = new();

    public ReportWizardDialog(ReportWizardViewModel vm)
    {
        DataContext = vm;
        InitializeComponent(vm);
    }

    private void InitializeComponent(ReportWizardViewModel vm)
    {
        Title = "Report Wizard";
        Width = 560;
        Height = 640;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var nameBox = new TextBox { Margin = new(0, 0, 0, 8) };
        nameBox.Bind(TextBox.TextProperty, new Binding(nameof(ReportWizardViewModel.Name)));

        var descriptionBox = new TextBox { Margin = new(0, 0, 0, 8) };
        descriptionBox.Bind(TextBox.TextProperty, new Binding(nameof(ReportWizardViewModel.Description)));

        var sqlBox = new TextBox
        {
            AcceptsReturn = true,
            MinHeight = 110,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            FontFamily = new Avalonia.Media.FontFamily("Consolas, Courier New"),
            Margin = new(0, 0, 0, 8)
        };
        sqlBox.Bind(TextBox.TextProperty, new Binding(nameof(ReportWizardViewModel.Sql)));

        var aiPromptBox = new TextBox
        {
            PlaceholderText = "Describe the report, e.g. \"monthly sales per category\"",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new(0, 0, 8, 0)
        };
        aiPromptBox.Bind(TextBox.TextProperty, new Binding(nameof(ReportWizardViewModel.AiPrompt)));

        var aiButton = new Button { Content = "✨ Generate with AI", Padding = new(12, 6) };
        aiButton.Bind(Button.CommandProperty, new Binding(nameof(ReportWizardViewModel.GenerateSqlWithAiCommand)));

        var aiPanel = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Margin = new(0, 0, 0, 8) };
        aiPanel.Children.Add(aiPromptBox);
        Grid.SetColumn(aiButton, 1);
        aiPanel.Children.Add(aiButton);
        aiPanel.Bind(Avalonia.Visual.IsVisibleProperty, new Binding(nameof(ReportWizardViewModel.IsAiAvailable)));

        var previewButton = new Button { Content = "Run preview", Padding = new(12, 6), Margin = new(0, 0, 0, 8), HorizontalAlignment = HorizontalAlignment.Left };
        previewButton.Bind(Button.CommandProperty, new Binding(nameof(ReportWizardViewModel.PreviewCommand)));

        var previewBox = new TextBox
        {
            IsReadOnly = true,
            MinHeight = 120,
            FontFamily = new Avalonia.Media.FontFamily("Consolas, Courier New"),
            FontSize = 11,
            TextWrapping = Avalonia.Media.TextWrapping.NoWrap,
            Margin = new(0, 0, 0, 8)
        };
        previewBox.Bind(TextBox.TextProperty, new Binding(nameof(ReportWizardViewModel.PreviewText)));

        var statusText = new TextBlock { TextWrapping = Avalonia.Media.TextWrapping.Wrap, Margin = new(0, 0, 0, 8) };
        statusText.Bind(TextBlock.TextProperty, new Binding(nameof(ReportWizardViewModel.StatusMessage)));
        statusText.Bind(TextBlock.ForegroundProperty, new Binding(nameof(ReportWizardViewModel.HasError)) { Converter = ErrorColor });

        var finishButton = new Button { Content = "Save Report", Padding = new(24, 8), HorizontalAlignment = HorizontalAlignment.Right };
        finishButton.Click += (_, _) =>
        {
            vm.FinishCommand.Execute(null);
            if (vm.SavedReport != null)
            {
                Close();
            }
        };

        var cancelButton = new Button { Content = "Cancel", Padding = new(24, 8), Margin = new(8, 0, 0, 0), HorizontalAlignment = HorizontalAlignment.Right };
        cancelButton.Click += (_, _) => Close();

        Content = new ScrollViewer
        {
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
                            SectionHeader("1. Details"),
                            Label("Name"),
                            nameBox,
                            Label("Description (optional)"),
                            descriptionBox,
                            SectionHeader("2. Query"),
                            sqlBox,
                            aiPanel,
                            SectionHeader("3. Preview"),
                            previewButton,
                            previewBox,
                            statusText,
                            new StackPanel
                            {
                                Orientation = Orientation.Horizontal,
                                HorizontalAlignment = HorizontalAlignment.Right,
                                Children = { finishButton, cancelButton }
                            }
                        }
                    }
                }
            }
        };
    }

    private static TextBlock Label(string text) =>
        new() { Text = text, FontWeight = Avalonia.Media.FontWeight.SemiBold, Margin = new(0, 0, 0, 4) };

    private static TextBlock SectionHeader(string text) =>
        new() { Text = text, FontWeight = Avalonia.Media.FontWeight.Bold, FontSize = 14, Margin = new(0, 4, 0, 8) };
}
