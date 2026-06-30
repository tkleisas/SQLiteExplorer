using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using SQLiteExplorer.Lib.Models;

namespace SQLiteExplorer.Lib.Views;

public partial class OracleConnectionDialog : Window
{
    public OracleConnectionInfo? ConnectionInfo { get; private set; }

    public OracleConnectionDialog()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Title = "Connect to Oracle";
        Width = 420;
        Height = 520;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        // EZConnect fields
        var hostBox = new TextBox { Watermark = "localhost", Margin = new(0, 0, 0, 8) };
        var portBox = new NumericUpDown { Value = 1521, Minimum = 1, Maximum = 65535, Margin = new(0, 0, 0, 8) };
        var serviceBox = new TextBox { Watermark = "Service name (e.g. XEPDB1, FREEPDB1)", Margin = new(0, 0, 0, 8) };
        var usernameBox = new TextBox { Watermark = "Username", Margin = new(0, 0, 0, 8) };
        var passwordBox = new TextBox { Watermark = "Password", PasswordChar = '●', Margin = new(0, 0, 0, 8) };

        var ezPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Children =
            {
                new TextBlock { Text = "Host", FontWeight = FontWeight.SemiBold, Margin = new(0, 0, 0, 4) },
                hostBox,
                new TextBlock { Text = "Port", FontWeight = FontWeight.SemiBold, Margin = new(0, 0, 0, 4) },
                portBox,
                new TextBlock { Text = "Service Name", FontWeight = FontWeight.SemiBold, Margin = new(0, 0, 0, 4) },
                serviceBox,
                new TextBlock { Text = "Username", FontWeight = FontWeight.SemiBold, Margin = new(0, 0, 0, 4) },
                usernameBox,
                new TextBlock { Text = "Password", FontWeight = FontWeight.SemiBold, Margin = new(0, 0, 0, 4) },
                passwordBox
            }
        };

        // Raw connection-string field
        var rawBox = new TextBox
        {
            Watermark = "User Id=scott;Password=tiger;Data Source=host:1521/service\n(or Data Source=MY_TNS_ALIAS)",
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            Height = 120,
            Margin = new(0, 0, 0, 8)
        };

        var rawPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            IsVisible = false,
            Children =
            {
                new TextBlock { Text = "Connection String", FontWeight = FontWeight.SemiBold, Margin = new(0, 0, 0, 4) },
                rawBox
            }
        };

        var ezRadio = new RadioButton { Content = "Host / Port / Service (EZConnect)", GroupName = "mode", IsChecked = true, Margin = new(0, 0, 0, 4) };
        var rawRadio = new RadioButton { Content = "Raw connection string / TNS", GroupName = "mode", Margin = new(0, 0, 0, 8) };

        void UpdateMode()
        {
            var raw = rawRadio.IsChecked == true;
            ezPanel.IsVisible = !raw;
            rawPanel.IsVisible = raw;
        }

        ezRadio.IsCheckedChanged += (_, _) => UpdateMode();
        rawRadio.IsCheckedChanged += (_, _) => UpdateMode();
        UpdateMode();

        var connectButton = new Button { Content = "Connect", HorizontalAlignment = HorizontalAlignment.Right, Padding = new(24, 8) };
        connectButton.Click += (s, e) =>
        {
            if (rawRadio.IsChecked == true)
            {
                ConnectionInfo = new OracleConnectionInfo
                {
                    RawConnectionString = rawBox.Text ?? ""
                };
            }
            else
            {
                ConnectionInfo = new OracleConnectionInfo
                {
                    Host = hostBox.Text ?? "localhost",
                    Port = (int)portBox.Value,
                    ServiceName = serviceBox.Text ?? "",
                    Username = usernameBox.Text ?? "",
                    Password = passwordBox.Text ?? ""
                };
            }
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
                        new TextBlock { Text = "Connection Mode", FontWeight = FontWeight.SemiBold, Margin = new(0, 0, 0, 4) },
                        ezRadio,
                        rawRadio,
                        ezPanel,
                        rawPanel,
                        new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            HorizontalAlignment = HorizontalAlignment.Right,
                            Margin = new(0, 8, 0, 0),
                            Children = { connectButton, cancelButton }
                        }
                    }
                }
            }
        };
    }
}
