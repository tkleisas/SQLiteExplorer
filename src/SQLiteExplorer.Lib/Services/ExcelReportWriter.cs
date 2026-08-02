using System;
using System.Globalization;
using ClosedXML.Excel;
using SQLiteExplorer.Lib.Models;

namespace SQLiteExplorer.Lib.Services;

/// <summary>
/// Writes a <see cref="QueryResult"/> to an Excel workbook using ClosedXML:
/// title block, bold frozen header row, typed data cells, auto-fitted columns.
/// </summary>
public static class ExcelReportWriter
{
    public static void Write(QueryResult result, ReportDefinition report, string path)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Report");

        ws.Cell(1, 1).Value = report.Name;
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;

        ws.Cell(2, 1).Value = $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}  ·  {result.RowCount} row(s)";
        ws.Cell(2, 1).Style.Font.FontColor = XLColor.Gray;

        if (!string.IsNullOrWhiteSpace(report.Description))
        {
            ws.Cell(3, 1).Value = report.Description;
            ws.Cell(3, 1).Style.Font.FontColor = XLColor.Gray;
        }

        const int headerRow = 5;
        for (var i = 0; i < result.ColumnNames.Count; i++)
        {
            var cell = ws.Cell(headerRow, i + 1);
            cell.Value = result.ColumnNames[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#DDEBF7");
            cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        }

        for (var r = 0; r < result.Rows.Count; r++)
        {
            var row = result.Rows[r];
            for (var c = 0; c < result.ColumnNames.Count; c++)
            {
                row.TryGetValue(result.ColumnNames[c], out var value);
                SetCellValue(ws.Cell(headerRow + 1 + r, c + 1), value);
            }
        }

        if (result.Rows.Count > 0 || result.ColumnNames.Count > 0)
        {
            ws.SheetView.FreezeRows(headerRow);
        }

        ws.Columns().AdjustToContents();
        workbook.SaveAs(path);
    }

    private static void SetCellValue(IXLCell cell, object? value)
    {
        switch (value)
        {
            case null:
                cell.SetValue(Blank.Value);
                break;
            case string s:
                cell.SetValue(s);
                break;
            case bool b:
                cell.SetValue(b);
                break;
            case DateTime dt:
                cell.SetValue(dt);
                break;
            case DateTimeOffset dto:
                cell.SetValue(dto.DateTime);
                break;
            case byte or short or int or long or sbyte or ushort or uint or ulong:
                cell.SetValue(Convert.ToInt64(value, CultureInfo.InvariantCulture));
                break;
            case float or double or decimal:
                cell.SetValue(Convert.ToDouble(value, CultureInfo.InvariantCulture));
                break;
            case byte[] bytes:
                cell.SetValue(Convert.ToBase64String(bytes));
                break;
            default:
                cell.SetValue(value.ToString() ?? string.Empty);
                break;
        }
    }
}
