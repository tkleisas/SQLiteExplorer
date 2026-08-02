using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using SQLiteExplorer.Lib.Converters;
using SQLiteExplorer.Lib.Models;
using SQLiteExplorer.Lib.ViewModels;

namespace SQLiteExplorer.Lib.Views;

/// <summary>
/// Reports manager: lists saved report definitions with Generate/Edit/Delete actions.
/// Follows the code-built dialog idiom of the connection dialogs.
/// </summary>
public partial class ReportsDialog : Window
{
    private static readonly HasErrorToColorConverter ErrorColor = new();

    public ReportsDialog(ReportsViewModel vm)
    {
        DataContext = vm;
        InitializeComponent(vm);
    }

    private void InitializeComponent(ReportsViewModel vm)
    {
        Title = "Reports";
        Width = 560;
        Height = 480;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var list = new ListBox { Margin = new(0, 0, 0, 8) };
        list.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(ReportsViewModel.Reports)));
        list.Bind(ListBox.SelectedItemProperty, new Binding(nameof(ReportsViewModel.SelectedReport)));
        list.ItemTemplate = new FuncDataTemplate<ReportDefinition>((report, _) =>
        {
            var name = new TextBlock { FontWeight = Avalonia.Media.FontWeight.SemiBold };
            name.Bind(TextBlock.TextProperty, new Binding(nameof(ReportDefinition.Name)));

            var details = new TextBlock { FontSize = 11, Opacity = 0.7, TextWrapping = Avalonia.Media.TextWrapping.Wrap };
            details.Bind(TextBlock.TextProperty, new Binding(nameof(ReportDefinition.Description)));

            return new StackPanel
            {
                Margin = new(0, 2),
                Children = { name, details }
            };
        });

        var newButton = MakeButton("New", nameof(ReportsViewModel.NewCommand));
        var editButton = MakeButton("Edit", nameof(ReportsViewModel.EditCommand));
        var deleteButton = MakeButton("Delete", nameof(ReportsViewModel.DeleteCommand));

        var generateButton = new Button
        {
            Content = "Generate Excel…",
            Padding = new(16, 6),
            Margin = new(8, 0, 0, 0)
        };
        generateButton.Bind(Button.CommandProperty, new Binding(nameof(ReportsViewModel.GenerateCommand)));

        var closeButton = new Button { Content = "Close", Padding = new(16, 6), Margin = new(8, 0, 0, 0) };
        closeButton.Click += (_, _) => Close();

        var statusText = new TextBlock { TextWrapping = Avalonia.Media.TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center };
        statusText.Bind(TextBlock.TextProperty, new Binding(nameof(ReportsViewModel.StatusMessage)));
        statusText.Bind(TextBlock.ForegroundProperty, new Binding(nameof(ReportsViewModel.HasError)) { Converter = ErrorColor });

        Content = new Grid
        {
            Margin = new(16),
            RowDefinitions = new RowDefinitions("Auto,*,Auto,Auto"),
            Children =
            {
                Placed(new TextBlock
                {
                    Text = "Saved reports — select one to generate an Excel file, edit, or delete it.",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    Opacity = 0.7,
                    Margin = new(0, 0, 0, 8)
                }, 0),
                Placed(list, 1),
                Placed(statusText, 2),
                Placed(new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new(0, 8, 0, 0),
                    Children = { newButton, editButton, deleteButton, generateButton, closeButton }
                }, 3)
            }
        };
    }

    private static Button MakeButton(string content, string commandName)
    {
        var button = new Button { Content = content, Padding = new(16, 6), Margin = new(8, 0, 0, 0) };
        button.Bind(Button.CommandProperty, new Binding(commandName));
        return button;
    }

    private static Control Placed(Control control, int row)
    {
        Grid.SetRow(control, row);
        return control;
    }
}
