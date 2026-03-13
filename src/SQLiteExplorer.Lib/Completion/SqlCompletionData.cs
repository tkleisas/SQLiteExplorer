using System;
using Avalonia.Media;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;

namespace SQLiteExplorer.Lib.Completion;

public class SqlCompletionData : ICompletionData
{
    public SqlCompletionData(string text, string description = "", CompletionCategory category = CompletionCategory.Keyword)
    {
        Text = text;
        DescriptionText = description;
        Category = category;
    }

    public enum CompletionCategory
    {
        Keyword,
        Table,
        Column,
        Function
    }

    public string Text { get; }
    public string DescriptionText { get; }
    public CompletionCategory Category { get; }

    public object Content => $"{GetCategoryIcon()} {Text}";

    public object Description => DescriptionText;

    public double Priority => Category switch
    {
        CompletionCategory.Table => 2,
        CompletionCategory.Column => 1,
        CompletionCategory.Keyword => 0,
        CompletionCategory.Function => 0,
        _ => 0
    };

    public IImage? Image => null;

    private string GetCategoryIcon() => Category switch
    {
        CompletionCategory.Table => "📋",
        CompletionCategory.Column => "📝",
        CompletionCategory.Function => "⚡",
        _ => "🔑"
    };

    public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
    {
        textArea.Document.Replace(completionSegment, Text);
    }
}
