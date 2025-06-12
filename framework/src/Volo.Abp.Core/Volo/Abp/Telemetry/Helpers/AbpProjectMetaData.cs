using System;

namespace Volo.Abp.Telemetry.Helpers;

public class AbpProjectMetaData
{
    public Guid? ProjectId { get; set; }
    public string? Role { get; set; }
    public string? AbpSlnPath { get; set; }
}