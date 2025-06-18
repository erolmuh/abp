using System;

namespace Volo.Abp.Internal.Telemetry.Activity.Storage;

public class TelemetryPeriod
{
    private const string TestModeEnvironmentVariable = "ABP_TELEMETRY_TEST_MODE";
    public TelemetryPeriod()
    {
        var isTestMode = IsTestModeEnabled();
        
        InformationSendPeriod = isTestMode 
            ? TimeSpan.FromSeconds(15) 
            : TimeSpan.FromDays(7);
        
        ActivitySendPeriod = isTestMode 
            ? TimeSpan.FromSeconds(5) 
            : TimeSpan.FromDays(1);
    }
    
    public TimeSpan ActivitySendPeriod { get;  }
    public TimeSpan InformationSendPeriod { get;  }
    
    private static bool IsTestModeEnabled()
    {
        var testModeVariable = Environment.GetEnvironmentVariable(TestModeEnvironmentVariable, EnvironmentVariableTarget.User);
        return bool.TryParse(testModeVariable, out var isTestMode) && isTestMode;
    }
}