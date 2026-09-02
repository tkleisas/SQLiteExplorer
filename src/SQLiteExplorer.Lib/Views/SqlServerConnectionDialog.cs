using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Microsoft.Data.SqlClient;
using SQLiteExplorer.Lib.Models;

namespace SQLiteExplorer.Lib.Views;

public partial class SqlServerConnectionDialog : Window
{
    private sealed record EncryptMode(string Label, SqlConnectionEncryptOption Value)
    {
        public override string ToString() => Label;
    }

    public SqlServerConnectionInfo? ConnectionInfo { get; private set; }

    public SqlServerConnectionDialog()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Title = "Connect to SQL Server";
        Width = 440;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var serverBox = new TextBox { Watermark = "localhost, .\\SQLEXPRESS or host,port", Margin = new(0, 0, 0, 8) };
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

        var encryptCombo = new ComboBox
        {
            SelectedIndex = 0,
            ItemsSource = new[]
            {
                new EncryptMode("Required (default)", SqlConnectionEncryptOption.Mandatory),
                new EncryptMode("Not encrypted", SqlConnectionEncryptOption.Optional),
                new EncryptMode("Strict (TDS 8.0)", SqlConnectionEncryptOption.Strict)
            },
            Margin = new(0, 0, 0, 8)
        };

        var trustCertCheck = new CheckBox { Content = "Trust server certificate", IsChecked = true, Margin = new(0, 0, 0, 8) };

        // Strict always validates the certificate; the checkbox does not apply.
        encryptCombo.SelectionChanged += (_, _) =>
        {
            var mode = ((EncryptMode?)encryptCombo.SelectedItem)?.Value;
            trustCertCheck.IsEnabled = mode != SqlConnectionEncryptOption.Strict;
        };

        var timeoutBox = new NumericUpDown { Value = 15, Minimum = 0, Maximum = 300, Margin = new(0, 0, 0, 8) };
        var applicationBox = new TextBox { Watermark = "Application name (optional)", Margin = new(0, 0, 0, 8) };
        var marsCheck = new CheckBox { Content = "Multiple active result sets (MARS)", IsChecked = false, Margin = new(0, 0, 0, 8) };
        var additionalBox = new TextBox { Watermark = "Key=Value; extra options override the above" };

        var advancedExpander = new Expander
        {
            Header = "Advanced options",
            IsExpanded = false,
            Margin = new(0, 0, 0, 16),
            Content = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new(0, 8, 0, 0),
                Children =
                {
                    new TextBlock { Text = "Connection timeout (seconds, 0 = infinite)", FontWeight = FontWeight.SemiBold, Margin = new(0, 0, 0, 4) },
                    timeoutBox,
                    new TextBlock { Text = "Application name", FontWeight = FontWeight.SemiBold, Margin = new(0, 0, 0, 4) },
                    applicationBox,
                    marsCheck,
                    new TextBlock { Text = "Additional connection options", FontWeight = FontWeight.SemiBold, Margin = new(0, 8, 0, 4) },
                    additionalBox
                }
            }
        };

        var connectButton = new Button { Content = "Connect", HorizontalAlignment = HorizontalAlignment.Right, Padding = new(24, 8) };
        connectButton.Click += (s, e) =>
        {
            ConnectionInfo = new SqlServerConnectionInfo
            {
                Server = serverBox.Text ?? "localhost",
                Database = databaseBox.Text ?? "",
                UseWindowsAuth = windowsAuthRadio.IsChecked == true,
                Username = usernameBox.Text ?? "",
                Password = passwordBox.Text ?? "",
                Encrypt = ((EncryptMode)encryptCombo.SelectedItem!).Value,
                TrustServerCertificate = trustCertCheck.IsChecked == true,
                ConnectionTimeout = (int)timeoutBox.Value,
                ApplicationName = applicationBox.Text ?? "",
                MultipleActiveResultSets = marsCheck.IsChecked == true,
                AdditionalOptions = string.IsNullOrWhiteSpace(additionalBox.Text) ? null : additionalBox.Text!.Trim()
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
                        new TextBlock { Text = "Encryption", FontWeight = FontWeight.SemiBold, Margin = new(0, 0, 0, 4) },
                        encryptCombo,
                        trustCertCheck,
                        advancedExpander,
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
