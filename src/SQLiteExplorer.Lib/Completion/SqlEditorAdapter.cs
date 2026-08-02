using AvaloniaEdit;

namespace SQLiteExplorer.Lib.Completion;

/// <summary>
/// Thin adapter over a <see cref="TextEditor"/> exposing caret-aware text
/// operations to view models without leaking the editor into them.
/// </summary>
public class SqlEditorAdapter
{
    private readonly TextEditor _editor;

    public SqlEditorAdapter(TextEditor editor)
    {
        _editor = editor;
    }

    public TextEditor TextEditor => _editor;

    /// <summary>Inserts text at the caret, replacing any active selection.</summary>
    public void InsertAtCaret(string text)
    {
        var offset = _editor.SelectionLength > 0
            ? _editor.SelectionStart
            : _editor.TextArea.Caret.Offset;

        _editor.Document.Replace(offset, _editor.SelectionLength, text);
        _editor.TextArea.Caret.Offset = offset + text.Length;
        _editor.TextArea.Focus();
    }

    public string GetTextBeforeCaret()
    {
        var caretOffset = _editor.TextArea.Caret.Offset;
        return caretOffset <= 0
            ? string.Empty
            : _editor.Document.GetText(0, caretOffset);
    }
}
