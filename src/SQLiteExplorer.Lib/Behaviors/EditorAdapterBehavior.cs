using Avalonia;
using Avalonia.Controls;
using AvaloniaEdit;
using SQLiteExplorer.Lib.Completion;
using SQLiteExplorer.Lib.ViewModels;

namespace SQLiteExplorer.Lib.Behaviors;

/// <summary>
/// Attached property that registers a <see cref="SqlEditorAdapter"/> on the
/// bound <see cref="QueryTabViewModel"/> so the view model can insert text at
/// the caret (used by AI completion and "Insert SQL into editor").
/// </summary>
public class EditorAdapterBehavior
{
    public static readonly AttachedProperty<QueryTabViewModel?> TabProperty =
        AvaloniaProperty.RegisterAttached<EditorAdapterBehavior, TextEditor, QueryTabViewModel?>("Tab");

    static EditorAdapterBehavior()
    {
        TabProperty.Changed.AddClassHandler<TextEditor>(OnTabChanged);
    }

    public static void SetTab(AvaloniaObject element, QueryTabViewModel? value)
    {
        element.SetValue(TabProperty, value);
    }

    public static QueryTabViewModel? GetTab(AvaloniaObject element)
    {
        return element.GetValue(TabProperty);
    }

    private static void OnTabChanged(TextEditor editor, AvaloniaPropertyChangedEventArgs args)
    {
        if (args.OldValue is QueryTabViewModel oldTab && oldTab.Editor?.TextEditor == editor)
        {
            oldTab.Editor = null;
        }

        if (args.NewValue is QueryTabViewModel newTab)
        {
            newTab.Editor = new SqlEditorAdapter(editor);
            editor.DetachedFromVisualTree += OnEditorDetached;
        }
    }

    private static void OnEditorDetached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is TextEditor editor)
        {
            editor.DetachedFromVisualTree -= OnEditorDetached;
            var tab = GetTab(editor);
            if (tab?.Editor?.TextEditor == editor)
            {
                tab.Editor = null;
            }
        }
    }
}
