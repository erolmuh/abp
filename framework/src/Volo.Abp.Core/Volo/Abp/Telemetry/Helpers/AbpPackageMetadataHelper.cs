using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace Volo.Abp.Telemetry.Helpers;

public static class AbpPackageMetadataHelper
{
    public static AbpPackageMetadata? GetMetaData(Assembly assembly)
    {
        var assemblyPath = assembly.Location;

        if (string.IsNullOrWhiteSpace(assemblyPath))
            return null;

        var dir = Path.GetDirectoryName(assemblyPath);
        if (dir == null)
            return null;

        var abppkgPath = Directory.GetFiles(dir, "*.abppkg").FirstOrDefault();
        if (abppkgPath == null)
        {
            return null;
        }

        var metadata =  ReadOrCreateMetadata(abppkgPath);
        var abpslnPath = FindAbpSlnFile(dir);
        if (!abpslnPath.IsNullOrEmpty())
        {
            metadata.AbpSlnPath = abpslnPath;
        }
        
        
        return metadata;
    }

    private static AbpPackageMetadata ReadOrCreateMetadata(string path)
    {
        var json = File.ReadAllText(path);
        var doc = JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new();

        var metadata = new AbpPackageMetadata();

        if (doc.TryGetValue("projectId", out var existingProjectId) && existingProjectId is string id &&
            !string.IsNullOrWhiteSpace(id))
        {
            metadata.ProjectId = id;
        }
        else
        {
            metadata.ProjectId = Guid.NewGuid().ToString();
            doc["projectId"] = metadata.ProjectId;
        }

        // Role
        if (doc.TryGetValue("role", out var existingRole) && existingRole is string role)
        {
            metadata.Role = role;
        }

        // Save if new ProjectId was generated
        var updatedJson = JsonSerializer.Serialize(doc, new JsonSerializerOptions {WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase});
        File.WriteAllText(path, updatedJson);

        return metadata;
    }
    
    private static string? FindAbpSlnFile(string startingDir)
    {
        var currentDir = new DirectoryInfo(startingDir);

        while (currentDir != null)
        {
            var abpslnFile = currentDir.GetFiles("*.abpsln").FirstOrDefault();
            if (abpslnFile != null)
            {
                return abpslnFile.FullName;
            }

            currentDir = currentDir.Parent;
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