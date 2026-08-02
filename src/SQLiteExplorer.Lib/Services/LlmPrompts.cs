using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SQLiteExplorer.Lib.Models;

namespace SQLiteExplorer.Lib.Services;

/// <summary>
/// Pure prompt builders for the AI assistant. Every prompt is dialect-aware
/// (database type) and schema-aware (tables and columns embedded).
/// </summary>
public static class LlmPrompts
{
    public const int MaxAnalysisRows = 50;
    public const int MaxAnalysisColumns = 20;

    public static string BuildSchemaDescription(string databaseType, IEnumerable<TableInfo> tables)
    {
        var sb = new StringBuilder();
        sb.Append("Database type: ").AppendLine(databaseType);
        sb.AppendLine("Tables and views:");

        foreach (var table in tables)
        {
            var qualified = string.IsNullOrEmpty(table.Schema)
                ? table.Name
                : $"{table.Schema}.{table.Name}";
            var kind = table.Type == "view" ? "view" : "table";
            var columns = string.Join(", ", table.Columns.Select(c => $"{c.Name} {c.Type}"));
            sb.Append("- ").Append(qualified).Append(" (").Append(kind).Append("): ").AppendLine(columns);
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>Continue the SQL statement at the caret. The reply must be SQL only.</summary>
    public static (string System, string User) BuildCompletion(string schemaDescription, string sqlBeforeCaret)
    {
        var system = """
            You are a SQL completion engine embedded in a database explorer.
            Continue the user's SQL exactly at the caret position.
            Reply with ONLY the SQL completion text — no explanations, no markdown fences.
            Keep the completion short: finish the current statement, at most a few lines.
            """;

        var user = $"""
            {schemaDescription}

            SQL up to the caret:
            {sqlBeforeCaret}
            """;

        return (system, user);
    }

    /// <summary>Translate a natural-language question into a SQL query.</summary>
    public static (string System, string User) BuildGenerateSql(string schemaDescription, string question)
    {
        var system = """
            You are a SQL expert embedded in a database explorer.
            Translate the user's request into a single SQL query matching the database dialect.
            Reply with ONLY the SQL in a single ```sql fenced block, plus at most one sentence
            of explanation after the block.
            """;

        var user = $"""
            {schemaDescription}

            Request: {question}
            """;

        return (system, user);
    }

    public static (string System, string User) BuildExplain(string schemaDescription, string sql)
    {
        var system = """
            You are a SQL expert embedded in a database explorer.
            Explain what the query does in plain English: purpose, tables and joins used,
            filtering, and the shape of the result. Be concise — a short paragraph or a few bullets.
            """;

        var user = $"""
            {schemaDescription}

            SQL:
            {sql}
            """;

        return (system, user);
    }

    public static (string System, string User) BuildOptimize(string schemaDescription, string sql)
    {
        var system = """
            You are a SQL performance expert embedded in a database explorer.
            Review the query for the given dialect. Suggest a rewritten query and/or index
            recommendations when they would help. Reply with the improved SQL in a ```sql
            fenced block when you have one, followed by a brief rationale. If the query is
            already fine, say so in one or two sentences.
            """;

        var user = $"""
            {schemaDescription}

            SQL:
            {sql}
            """;

        return (system, user);
    }

    public static (string System, string User) BuildAnalyzeResults(
        string schemaDescription,
        string sql,
        IReadOnlyList<string> columns,
        IReadOnlyList<Dictionary<string, object?>> rows)
    {
        var system = """
            You are a data analyst embedded in a database explorer.
            Summarize the query result: what the rows represent, notable patterns, outliers,
            and anything worth investigating. Be concise — a short paragraph or a few bullets.
            """;

        var sb = new StringBuilder();
        sb.AppendLine(schemaDescription).AppendLine();
        sb.Append("SQL:\n").AppendLine(sql).AppendLine();
        sb.Append("Total rows returned: ").Append(rows.Count).AppendLine(". Sample:");

        var shownColumns = columns.Take(MaxAnalysisColumns).ToList();
        sb.AppendLine(string.Join(", ", shownColumns));

        foreach (var row in rows.Take(MaxAnalysisRows))
        {
            var values = shownColumns.Select(c =>
                row.TryGetValue(c, out var v) ? FormatValue(v) : string.Empty);
            sb.AppendLine(string.Join(", ", values));
        }

        return (system, sb.ToString());
    }

    /// <summary>
    /// Extracts SQL from an LLM reply: the first fenced code block when present,
    /// otherwise the whole trimmed text.
    /// </summary>
    public static string ExtractSql(string llmText)
    {
        if (string.IsNullOrWhiteSpace(llmText))
        {
            return string.Empty;
        }

        var text = llmText;
        var fenceStart = text.IndexOf("```", StringComparison.Ordinal);
        if (fenceStart >= 0)
        {
            var contentStart = text.IndexOf('\n', fenceStart);
            if (contentStart >= 0)
            {
                var fenceEnd = text.IndexOf("```", contentStart, StringComparison.Ordinal);
                if (fenceEnd > contentStart)
                {
                    return text[(contentStart + 1)..fenceEnd].Trim();
                }
            }
        }

        return text.Trim();
    }

    private static string FormatValue(object? value)
    {
        var s = value?.ToString() ?? "NULL";
        s = s.Replace('\n', ' ').Replace('\r', ' ');
        return s.Length > 100 ? s[..100] + "…" : s;
    }
}
