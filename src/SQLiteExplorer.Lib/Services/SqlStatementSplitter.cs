using System.Collections.Generic;
using System.Text;

namespace SQLiteExplorer.Lib.Services;

/// <summary>
/// Splits a SQL script into individual statements on top-level semicolons,
/// ignoring semicolons that appear inside string literals and line/block comments.
/// </summary>
internal static class SqlStatementSplitter
{
    /// <param name="sql">The script to split.</param>
    /// <param name="treatDoubleQuoteAsString">
    /// When true, double quotes also open/close a literal region (SQLite, where a
    /// double-quoted token may be a string). For PostgreSQL/SQL Server/Oracle this is
    /// false because double quotes delimit identifiers, not strings.
    /// </param>
    public static List<string> Split(string sql, bool treatDoubleQuoteAsString = false)
    {
        var statements = new List<string>();
        var current = new StringBuilder();
        var inString = false;
        var stringChar = '\0';

        for (var i = 0; i < sql.Length; i++)
        {
            var c = sql[i];

            if (!inString && (c == '\'' || (treatDoubleQuoteAsString && c == '"')))
            {
                inString = true;
                stringChar = c;
                current.Append(c);
            }
            else if (inString && c == stringChar)
            {
                if (i + 1 < sql.Length && sql[i + 1] == stringChar)
                {
                    current.Append(c);
                    current.Append(c);
                    i++;
                }
                else
                {
                    inString = false;
                    current.Append(c);
                }
            }
            else if (!inString && c == ';')
            {
                var stmt = current.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(stmt))
                {
                    statements.Add(stmt);
                }
                current.Clear();
            }
            else if (!inString && c == '-' && i + 1 < sql.Length && sql[i + 1] == '-')
            {
                while (i < sql.Length && sql[i] != '\n') i++;
            }
            else if (!inString && c == '/' && i + 1 < sql.Length && sql[i + 1] == '*')
            {
                i += 2;
                while (i < sql.Length - 1 && !(sql[i] == '*' && sql[i + 1] == '/')) i++;
                i++;
            }
            else
            {
                current.Append(c);
            }
        }

        var lastStmt = current.ToString().Trim();
        if (!string.IsNullOrWhiteSpace(lastStmt))
        {
            statements.Add(lastStmt);
        }

        return statements;
    }
}
