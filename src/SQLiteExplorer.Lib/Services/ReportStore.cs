using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using SQLiteExplorer.Lib.Models;

namespace SQLiteExplorer.Lib.Services;

/// <summary>
/// Persists <see cref="ReportDefinition"/> items as JSON. The standalone app
/// uses the default directory (%APPDATA%/SQLiteExplorer); hosts can point the
/// store at a per-workspace directory instead.
/// </summary>
public class ReportStore
{
    private readonly string _filePath;

    public ReportStore(string? directory = null)
    {
        var dir = directory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SQLiteExplorer");
        _filePath = Path.Combine(dir, "reports.json");
    }

    public List<ReportDefinition> Load()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                return new List<ReportDefinition>();
            }

            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<ReportDefinition>>(json) ?? new List<ReportDefinition>();
        }
        catch (Exception)
        {
            return new List<ReportDefinition>();
        }
    }

    public void Save(ReportDefinition report)
    {
        var reports = Load();
        var existing = reports.FindIndex(r => r.Id == report.Id);
        if (existing >= 0)
        {
            reports[existing] = report;
        }
        else
        {
            reports.Add(report);
        }

        Persist(reports);
    }

    public void Delete(string id)
    {
        var reports = Load();
        Persist(reports.Where(r => r.Id != id).ToList());
    }

    private void Persist(List<ReportDefinition> reports)
    {
        var dir = Path.GetDirectoryName(_filePath)!;
        Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(reports, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_filePath, json);
    }
}
