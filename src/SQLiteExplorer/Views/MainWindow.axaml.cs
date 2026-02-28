using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using SQLiteExplorer.ViewModels;

namespace SQLiteExplorer.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;

        if (e.KeyModifiers == KeyModifiers.Control)
        {
            switch (e.Key)
            {
                case Key.O:
                    vm.OpenSqliteDatabaseCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.N:
                    vm.NewSqliteDatabaseCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.T:
                    vm.AddNewQueryTabCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.Enter:
                    if (vm.SelectedTab?.ExecuteQueryCommand.CanExecute(null) == true)
                    {
                        vm.SelectedTab.ExecuteQueryCommand.Execute(null);
                        e.Handled = true;
                    }
                    break;
            }
        }
        else if (e.Key == Key.F5)
        {
            if (vm.SelectedTab?.ExecuteQueryCommand.CanExecute(null) == true)
            {
                vm.SelectedTab.ExecuteQueryCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}
