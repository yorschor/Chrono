using Chrono.Core;
using Microsoft.Build.Framework;

namespace Chrono.DotnetTasks;

public class StampVersionTask : Microsoft.Build.Utilities.Task
{
    public bool IgnoreDirtyRepo { get; set; }
    [Output] public string AssemblyVersion { get; private set; }

    [Output] public string FileVersion { get; private set; }

    [Output] public string InformationalVersion { get; private set; }

    [Output] public string PackageVersion { get; private set; }

    public override bool Execute()
    {
        try
        {
            var infoGetResult = VersionInfo.Get(IgnoreDirtyRepo);
            if (!infoGetResult)
            {
                Log.LogError(infoGetResult.Message);
                return false;
            }

            var parseFullVersionResult = infoGetResult.Data.GetVersion();
            if (parseFullVersionResult)
            {
                InformationalVersion = parseFullVersionResult.Data;
            }

            Log.LogMessage("Chrono -> Resolving full version to " + parseFullVersionResult.Data);
            var parseNumericVersionResult = infoGetResult.Data.GetNumericVersion();
            if (parseNumericVersionResult.Success)
            {
                AssemblyVersion = parseNumericVersionResult.Data;
                FileVersion = parseNumericVersionResult.Data;
                PackageVersion = parseNumericVersionResult.Data;
            }

            Log.LogMessage("Chrono -> Resolving numeric version to " + parseNumericVersionResult.Data);

            return true;
        }
        catch (Exception ex)
        {
            Log.LogErrorFromException(ex);
            return false;
        }
    }
}