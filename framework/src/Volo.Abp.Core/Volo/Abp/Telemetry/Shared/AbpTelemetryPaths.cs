using System;
using System.IO;

namespace Shared;

public static class AbpTelemetryPaths
{
    public static string AccessToken => Path.Combine(AbpRootPath, "cli", "access-token.bin");
    public static string ComputerId => Path.Combine(AbpRootPath, "cli", "computer-id.bin");
    public static string ActivityStorage => Path.Combine(AbpRootPath, "activity-storage.json");
    public static string Studio => Path.Combine(AbpRootPath, "studio");
    private readonly static string AbpRootPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".abp");
}