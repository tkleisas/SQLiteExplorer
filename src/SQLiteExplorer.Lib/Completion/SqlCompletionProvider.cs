using System;
using System.Collections.Generic;
using System.Linq;
using AvaloniaEdit.CodeCompletion;

namespace SQLiteExplorer.Lib.Completion;

public class SqlCompletionProvider
{
    private static readonly string[] SqlKeywords = new[]
    {
        "SELECT", "FROM", "WHERE", "AND", "OR", "NOT", "IN", "LIKE", "BETWEEN",
        "INSERT", "INTO", "VALUES", "UPDATE", "SET", "DELETE", "CREATE", "TABLE",
        "DROP", "ALTER", "ADD", "COLUMN", "INDEX", "VIEW", "JOIN", "LEFT", "RIGHT",
        "INNER", "OUTER", "ON", "AS", "ORDER", "BY", "ASC", "DESC", "GROUP", "HAVING",
        "DISTINCT", "LIMIT", "OFFSET", "UNION", "ALL", "NULL", "IS", "PRIMARY", "KEY",
        "FOREIGN", "REFERENCES", "UNIQUE", "CHECK", "DEFAULT", "AUTOINCREMENT",
        "INTEGER", "TEXT", "REAL", "BLOB", "NUMERIC", "BOOLEAN", "DATE", "DATETIME",
        "COUNT", "SUM", "AVG", "MIN", "MAX", "COALESCE", "NULLIF", "IFNULL",
        "CASE", "WHEN", "THEN", "ELSE", "END", "CAST", "EXISTS", "WITH", "RECURSIVE"
    };

    private static readonly string[] SqlFunctions = new[]
    {
        "ABS", "CHANGES", "CHAR", "COALESCE", "GLOB", "HEX", "IFNULL", "INSTR",
        "LAST_INSERT_ROWID", "LENGTH", "LIKE", "LIKELIHOOD", "LIKELY", "LOAD_EXTENSION",
        "LOWER", "LTRIM", "MAX", "MIN", "NULLIF", "PRINTF", "QUOTE", "RANDOM",
        "RANDOMBLOB", "REPLACE", "ROUND", "RTRIM", "SOUNDEX", "SQLITE_COMPILEOPTION_GET",
        "SQLITE_COMPILEOPTION_USED", "SQLITE_OFFSET", "SQLITE_SOURCE_ID", "SQLITE_VERSION",
        "SUBSTR", "SUBSTRING", "TOTAL_CHANGES", "TRIM", "TYPEOF", "UNICODE", "UNLIKELY",
        "UPPER", "ZEROBLOB", "DATE", "TIME", "DATETIME", "JULIANDAY", "STRFTIME",
        "CURRENT_DATE", "CURRENT_TIME", "CURRENT_TIMESTAMP"
    };

    private List<string> _tableNames = new();
    private Dictionary<string, List<string>> _tableColumns = new();

    public void UpdateSchema(List<string> tableNames, Dictionary<string, List<string>> tableColumns)
    {
        _tableNames = tableNames ?? new List<string>();
        _tableColumns = tableColumns ?? new Dictionary<string, List<string>>();
    }

    public IEnumerable<ICompletionData> GetCompletions(string textBeforeCaret)
    {
        var completions = new List<ICompletionData>();

        var wordBeforeCaret = GetWordBeforeCaret(textBeforeCaret);

        if (wordBeforeCaret.Equals("FROM", StringComparison.OrdinalIgnoreCase) ||
            wordBeforeCaret.Equals("JOIN", StringComparison.OrdinalIgnoreCase) ||
            wordBeforeCaret.Equals("INTO", StringComparison.OrdinalIgnoreCase) ||
            wordBeforeCaret.Equals("TABLE", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var table in _tableNames)
            {
                completions.Add(new SqlCompletionData(table, "Table", SqlCompletionData.CompletionCategory.Table));
            }
        }
        else if (wordBeforeCaret.Equals("SELECT", StringComparison.OrdinalIgnoreCase) ||
                 wordBeforeCaret.Equals("WHERE", StringComparison.OrdinalIgnoreCase) ||
                 wordBeforeCaret.Equals("AND", StringComparison.OrdinalIgnoreCase) ||
                 wordBeforeCaret.Equals("OR", StringComparison.OrdinalIgnoreCase) ||
                 wordBeforeCaret.Equals("SET", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var table in _tableNames)
            {
                if (_tableColumns.TryGetValue(table, out var columns))
                {
                    foreach (var column in columns)
                    {
                        completions.Add(new SqlCompletionData(column, $"Column in {table}", SqlCompletionData.CompletionCategory.Column));
                    }
                }
            }
        }

        foreach (var keyword in SqlKeywords)
        {
            completions.Add(new SqlCompletionData(keyword, "SQL Keyword", SqlCompletionData.CompletionCategory.Keyword));
        }

        foreach (var func in SqlFunctions)
        {
            completions.Add(new SqlCompletionData(func + "()", "SQL Function", SqlCompletionData.CompletionCategory.Function));
        }

        foreach (var table in _tableNames)
        {
            if (!completions.Any(c => c.Text.Equals(table, StringComparison.OrdinalIgnoreCase)))
            {
                completions.Add(new SqlCompletionData(table, "Table", SqlCompletionData.CompletionCategory.Table));
            }
        }

        return completions;
    }

    private static string GetWordBeforeCaret(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        
        var trimmed = text.TrimEnd();
        var lastSpace = trimmed.LastIndexOfAny(new[] { ' ', '\t', '\n', '\r', '(', ')', ',', ';' });
        
        if (lastSpace < 0) return trimmed.ToUpperInvariant();
        
        var word = trimmed.Substring(lastSpace + 1).Trim().ToUpperInvariant();
        return word;
    }
}
