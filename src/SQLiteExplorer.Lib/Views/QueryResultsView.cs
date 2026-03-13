using System.Collections.Generic;
using System.Diagnostics;
using Avalonia.Controls;
using AvaloniaVirtualDataGrid.Columns;
using AvaloniaVirtualDataGrid.Controls;
using SQLiteExplorer.Lib.ViewModels;

namespace SQLiteExplorer.Lib.Views;

public partial class QueryResultsView : UserControl
{
    private readonly VirtualDataGrid _dataGrid;
    private ResultSetViewModel? _viewModel;

    public QueryResultsView()
    {
        _dataGrid = new VirtualDataGrid
        {
            RowHeight = 28
        };
        Content = _dataGrid;
    }

    protected override void OnDataContextChanged(System.EventArgs e)
    {
        base.OnDataContextChanged(e);
        
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }
        
        _viewModel = DataContext as ResultSetViewModel;
        
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            UpdateGrid();
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ResultSetViewModel.DataItems))
        {
            UpdateGrid();
        }
    }

    private void UpdateGrid()
    {
        if (_viewModel == null) return;
        
        Debug.WriteLine($"UpdateGrid: Columns={_viewModel.Columns?.Count ?? 0}, DataItems={_viewModel.DataItems?.Count ?? -1}");
        
        _dataGrid.Columns.Clear();
        
        if (_viewModel.Columns != null)
        {
            foreach (var col in _viewModel.Columns)
            {
                _dataGrid.Columns.Add(col);
                Debug.WriteLine($"Added column: {col.Header}");
            }
        }
        
        if (_viewModel.DataItems is System.Collections.IList list)
        {
            _dataGrid.ItemsSource = list;
            Debug.WriteLine($"Set ItemsSource with {list.Count} items");
        }
        else if (_viewModel.DataItems != null)
        {
            Debug.WriteLine($"DataItems is not IList, type: {_viewModel.DataItems.GetType().Name}");
        }
    }
}
