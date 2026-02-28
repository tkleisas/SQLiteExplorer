using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using SQLiteExplorer.Models;

namespace SQLiteExplorer.Views;

public partial class PostgresConnectionDialog : Window
{
    public PostgresConnectionInfo? ConnectionInfo { get; private set; }

    public PostgresConnectionDialog()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Title = "Connect to PostgreSQL";
        Width = 400;
        Height = 350;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var hostBox = new TextBox { Watermark = "localhost", Margin = new(0, 0, 0, 8) };
        var portBox = new NumericUpDown { Value = 5432, Minimum = 1, Maximum = 65535, Margin = new(0, 0, 0, 8) };
        var databaseBox = new TextBox { Watermark = "Database name", Margin = new(0, 0, 0, 8) };
        var usernameBox = new TextBox { Watermark = "Username", Margin = new(0, 0, 0, 8) };
        var passwordBox = new TextBox { Watermark = "Password", PasswordChar = '●', Margin = new(0, 0, 0, 16) };

        var connectButton = new Button { Content = "Connect", HorizontalAlignment = HorizontalAlignment.Right, Padding = new(24, 8) };
        connectButton.Click += (s, e) =>
        {
            ConnectionInfo = new PostgresConnectionInfo
            {
                Host = hostBox.Text ?? "localhost",
                Port = (int)portBox.Value,
                Database = databaseBox.Text ?? "",
                Username = usernameBox.Text ?? "",
                Password = passwordBox.Text ?? ""
            };
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
                        new TextBlock { Text = "Host", FontWeight = FontWeight.SemiBold, Margin = new(0, 0, 0, 4) },
                        hostBox,
                        new TextBlock { Text = "Port", FontWeight = FontWeight.SemiBold, Margin = new(0, 0, 0, 4) },
                        portBox,
                        new TextBlock { Text = "Database", FontWeight = FontWeight.SemiBold, Margin = new(0, 0, 0, 4) },
                        databaseBox,
                        new TextBlock { Text = "Username", FontWeight = FontWeight.SemiBold, Margin = new(0, 0, 0, 4) },
                        usernameBox,
                        new TextBlock { Text = "Password", FontWeight = FontWeight.SemiBold, Margin = new(0, 0, 0, 4) },
                        passwordBox,
                        new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            HorizontalAlignment = HorizontalAlignment.Right,
                            Children = { connectButton, cancelButton }
                        }
                    }
                }
            }
        };
    }
}
