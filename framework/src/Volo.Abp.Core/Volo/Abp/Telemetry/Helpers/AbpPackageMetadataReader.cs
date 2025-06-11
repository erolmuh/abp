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
            var directoryName = Path.GetDirectoryName(assemblyPath);
            if (directoryName == null)
            {
                return null;
            }

            var abppkgPath = FindFileUpwards(directoryName, AbpPackageSearchPattern);
            if (abppkgPath == null)
            {
                return null;
            }

            var packageMetadata = ReadOrCreateMetadata(abppkgPath);

            var abpslnPath = FindFileUpwards(directoryName, AbpSolutionSearchPattern);
            if (!string.IsNullOrEmpty(abpslnPath))
            {
                packageMetadata.AbpSlnPath = abpslnPath;
            }

            return packageMetadata;
        }
        catch
        {
            return null;
        }
    }

    private static AbpPackageMetadata ReadOrCreateMetadata(string path)
    {
        var fileContent = File.ReadAllText(path);
        
        //TODO: Instead of converting to a dictionary, directly work with JSON objects
        var doc = JsonSerializer.Deserialize<Dictionary<string, object>>(fileContent) ?? new();

        var metadata = new AbpPackageMetadata();

        if (doc.TryGetValue("projectId", out var existingProjectId) && existingProjectId?.ToString() != null)
        {
            //TODO: Ensure that the projectId is GUID
            metadata.ProjectId = existingProjectId.ToString()!;
        }
        else
        {
            metadata.ProjectId = Guid.NewGuid().ToString();
            doc["projectId"] = metadata.ProjectId;
        }

        if (doc.TryGetValue("role", out var existingRole) && existingRole?.ToString() != null)
        {
            metadata.Role = existingRole.ToString()!;
        }

        //TODO: Instead of serializing a dictionary, directly work with JSON objects
        //TODO: Save only if we modified
        var updatedJson = JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        File.WriteAllText(path, updatedJson);

        return metadata;
    }

    private static string? FindFileUpwards(string initialDirectory, string searchPattern)
    {
        const int maxDepth = 10;

        var currentDirectory = new DirectoryInfo(initialDirectory);
        var currentDepth = 0;

        while (currentDirectory != null && currentDepth < maxDepth)
        {
            var file = currentDirectory.GetFiles(searchPattern).FirstOrDefault();
            if (file != null)
            {
                return file.FullName;
            }

            currentDirectory = currentDirectory.Parent;
            currentDepth++;
        }

        return null;
    }
}