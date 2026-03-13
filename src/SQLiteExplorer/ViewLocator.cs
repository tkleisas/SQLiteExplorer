using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using SQLiteExplorer.Lib.ViewModels;

namespace SQLiteExplorer;

/// <summary>
/// Given a view model, returns the corresponding view if possible.
/// Searches both the app assembly and the SQLiteExplorer.Lib assembly for views.
/// </summary>
[RequiresUnreferencedCode(
    "Default implementation of ViewLocator involves reflection which may be trimmed away.",
    Url = "https://docs.avaloniaui.net/docs/concepts/view-locator")]
public class ViewLocator : IDataTemplate
{
    private static readonly Assembly LibAssembly = typeof(ViewModelBase).Assembly;
    private static readonly Assembly AppAssembly = typeof(ViewLocator).Assembly;

    public Control? Build(object? param)
    {
        if (param is null)
            return null;
        
        var name = param.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);

        // Try app assembly first, then Lib assembly
        var type = AppAssembly.GetType(name) ?? LibAssembly.GetType(name);

        if (type != null)
        {
            return (Control)Activator.CreateInstance(type)!;
        }
        
        return new TextBlock { Text = "Not Found: " + name };
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}
