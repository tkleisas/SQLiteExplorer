using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using SQLiteExplorer.Lib.Models;

namespace SQLiteExplorer.Lib.Views;

public partial class SqlServerConnectionDialog : Window
{
    public SqlServerConnectionInfo? ConnectionInfo { get; private set; }

    public SqlServerConnectionDialog()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Title = "Connect to SQL Server";
        Width = 400;
        Height = 460;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var serverBox = new TextBox { Watermark = "localhost or .\\SQLEXPRESS", Margin = new(0, 0, 0, 8) };
        var databaseBox = new TextBox { Watermark = "Database name", Margin = new(0, 0, 0, 8) };

        var usernameBox = new TextBox { Watermark = "Username", Margin = new(0, 0, 0, 8) };
        var passwordBox = new TextBox { Watermark = "Password", PasswordChar = '●', Margin = new(0, 0, 0, 16) };

        var windowsAuthRadio = new RadioButton { Content = "Windows Authentication", GroupName = "auth", IsChecked = true, Margin = new(0, 0, 0, 4) };
        var sqlAuthRadio = new RadioButton { Content = "SQL Server Authentication", GroupName = "auth", Margin = new(0, 0, 0, 8) };

        void UpdateAuthEnabled()
        {
            var sql = sqlAuthRadio.IsChecked == true;
            usernameBox.IsEnabled = sql;
            passwordBox.IsEnabled = sql;
        }

        windowsAuthRadio.IsCheckedChanged += (_, _) => UpdateAuthEnabled();
        sqlAuthRadio.IsCheckedChanged += (_, _) => UpdateAuthEnabled();
        UpdateAuthEnabled();

        var connectButton = new Button { Content = "Connect", HorizontalAlignment = HorizontalAlignment.Right, Padding = new(24, 8) };
        connectButton.Click += (s, e) =>
        {
            ConnectionInfo = new SqlServerConnectionInfo
            {
                Server = serverBox.Text ?? "localhost",
                Database = databaseBox.Text ?? "",
                UseWindowsAuth = windowsAuthRadio.IsChecked == true,
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
                        new TextBlock { Text = "Server", FontWeight = FontWeight.SemiBold, Margin = new(0, 0, 0, 4) },
                        serverBox,
                        new TextBlock { Text = "Database", FontWeight = FontWeight.SemiBold, Margin = new(0, 0, 0, 4) },
                        databaseBox,
                        new TextBlock { Text = "Authentication", FontWeight = FontWeight.SemiBold, Margin = new(0, 0, 0, 4) },
                        windowsAuthRadio,
                        sqlAuthRadio,
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
