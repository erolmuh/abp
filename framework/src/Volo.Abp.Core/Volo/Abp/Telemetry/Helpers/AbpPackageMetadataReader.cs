using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace Volo.Abp.Telemetry.Helpers;

static internal class AbpProjectMetadataReader
{
    private const string AbpPackageSearchPattern = "*.abppkg";
    private const string AbpSolutionSearchPattern = "*.abpsln";
    public static AbpPackageMetadata? ReadProjectMetadata(Assembly assembly)
    {
        var assemblyPath = assembly.Location;
        try
        {
            var dir = Path.GetDirectoryName(assemblyPath);
            if (dir == null)
            {
                return null;
            }

            var abppkgPath = FindFileUpwards(dir, AbpPackageSearchPattern);
            if (abppkgPath == null)
            {
                return null;
            }

            var metadata = ReadOrCreateMetadata(abppkgPath);

            var abpslnPath = FindFileUpwards(dir, AbpSolutionSearchPattern);
            if (!string.IsNullOrEmpty(abpslnPath))
            {
                metadata.AbpSlnPath = abpslnPath;
            }

            return metadata;
        }
        catch
        {
            return null;
        }
    }

    private static AbpPackageMetadata ReadOrCreateMetadata(string path)
    {
        var json = File.ReadAllText(path);
        var doc = JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new();

        var metadata = new AbpPackageMetadata();

        if (doc.TryGetValue("projectId", out var existingProjectId) && existingProjectId.ToString() != null)
        {
            metadata.ProjectId = existingProjectId.ToString()!;
        }
        else
        {
            metadata.ProjectId = Guid.NewGuid().ToString();
            doc["projectId"] = metadata.ProjectId;
        }

        if (doc.TryGetValue("role", out var existingRole) && existingRole.ToString() != null)
        {
            metadata.Role = existingRole.ToString()!;
        }

        var updatedJson = JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        File.WriteAllText(path, updatedJson);

        return metadata;
    }

    private static string? FindFileUpwards(string startingDir, string searchPattern)
    {
        var currentDir = new DirectoryInfo(startingDir);
        const int maxDepth = 10;
        var currentDepth = 0;

        while (currentDir != null && currentDepth < maxDepth)
        {
            var file = currentDir.GetFiles(searchPattern).FirstOrDefault();
            if (file != null)
            {
                return file.FullName;
            }

            currentDir = currentDir.Parent;
            currentDepth++;
        }

        return null;
    }
}

public class AbpPackageMetadata
{
    public string? ProjectId { get; set; }
    public string? Role { get; set; }
    public string? AbpSlnPath { get; set; }
}