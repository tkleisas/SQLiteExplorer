using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Input;
using AvaloniaEdit;
using AvaloniaEdit.CodeCompletion;
using SQLiteExplorer.Completion;

namespace SQLiteExplorer.Behaviors;

public class SqlCompletionBehavior
{
    public static readonly AttachedProperty<SqlCompletionProvider?> ProviderProperty =
        AvaloniaProperty.RegisterAttached<SqlCompletionBehavior, TextEditor, SqlCompletionProvider?>("Provider");

    private static readonly AttachedProperty<SqlCompletionInstance?> InstanceProperty =
        AvaloniaProperty.RegisterAttached<SqlCompletionBehavior, TextEditor, SqlCompletionInstance?>("Instance");

    static SqlCompletionBehavior()
    {
        ProviderProperty.Changed.AddClassHandler<TextEditor>(OnProviderChanged);
    }

    public static void SetProvider(AvaloniaObject element, SqlCompletionProvider? value)
    {
        element.SetValue(ProviderProperty, value);
    }

    public static SqlCompletionProvider? GetProvider(AvaloniaObject element)
    {
        return element.GetValue(ProviderProperty);
    }

    private static void OnProviderChanged(TextEditor textEditor, AvaloniaPropertyChangedEventArgs args)
    {
        var oldInstance = textEditor.GetValue(InstanceProperty);
        oldInstance?.Dispose();

        var newProvider = args.NewValue as SqlCompletionProvider;
        if (newProvider != null)
        {
            var instance = new SqlCompletionInstance(textEditor, newProvider);
            textEditor.SetValue(InstanceProperty, instance);
        }
        else
        {
            textEditor.SetValue(InstanceProperty, null);
        }
    }

    private class SqlCompletionInstance : IDisposable
    {
        private readonly TextEditor _textEditor;
        private readonly SqlCompletionProvider _provider;
        private CompletionWindow? _completionWindow;

        public SqlCompletionInstance(TextEditor textEditor, SqlCompletionProvider provider)
        {
            _textEditor = textEditor;
            _provider = provider;

            _textEditor.TextArea.TextEntered += OnTextEntered;
            _textEditor.TextArea.KeyDown += OnKeyDown;
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space && e.KeyModifiers == KeyModifiers.Control)
            {
                e.Handled = true;
                ShowCompletion();
            }
        }

        private void OnTextEntered(object? sender, TextInputEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Text)) return;

            if (e.Text == " " || e.Text == ".")
            {
                return;
            }

            if (char.IsLetterOrDigit(e.Text[0]) || e.Text == "_")
            {
                ShowCompletion();
            }
        }

        private void ShowCompletion()
        {
            _completionWindow?.Close();
            _completionWindow = null;

            var textArea = _textEditor.TextArea;
            var caretOffset = textArea.Caret.Offset;
            
            var wordStart = FindWordStart(textArea.Document.GetText(0, caretOffset));
            var typedText = textArea.Document.GetText(wordStart, caretOffset - wordStart);
            
            var completion = _provider.GetCompletions(textArea.Document.GetText(0, caretOffset));

            var completionList = completion.ToList();
            if (completionList.Count == 0) return;

            _completionWindow = new CompletionWindow(textArea)
            {
                CloseWhenCaretAtBeginning = false
            };
            
            _completionWindow.StartOffset = wordStart;
            _completionWindow.EndOffset = caretOffset;

            var data = _completionWindow.CompletionList.CompletionData;
            foreach (var item in completionList)
            {
                data.Add(item);
            }

            _completionWindow.CompletionList.SelectItem(typedText);

            _completionWindow.Show();
            _completionWindow.Closed += (_, _) => _completionWindow = null;
        }

        private static int FindWordStart(string textBeforeCaret)
        {
            if (string.IsNullOrEmpty(textBeforeCaret)) return 0;
            
            for (var i = textBeforeCaret.Length - 1; i >= 0; i--)
            {
                var c = textBeforeCaret[i];
                if (!char.IsLetterOrDigit(c) && c != '_')
                {
                    return i + 1;
                }
            }
            
            return 0;
        }

        public void Dispose()
        {
            if (_textEditor?.TextArea != null)
            {
                _textEditor.TextArea.TextEntered -= OnTextEntered;
                _textEditor.TextArea.KeyDown -= OnKeyDown;
            }

            _completionWindow?.Close();
        }
    }
}
