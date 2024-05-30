using Chrono.Core;
using Chrono.Core.Helpers;
using Microsoft.Build.Framework;

namespace Chrono.DotnetTasks;

public class StampVersionTask : Microsoft.Build.Utilities.Task
{
    [Output] public string AssemblyVersion { get; private set; }

    [Output] public string FileVersion { get; private set; }

    [Output] public string InformationalVersion { get; private set; }

    [Output] public string PackageVersion { get; private set; }

    public override bool Execute()
    {
        try
        {
            var repoFoundResult = GitUtil.GetRepoRootPath();
            if (repoFoundResult is IErrorResult repoErr)
            {
                Log.LogError(repoErr.Message);
                return false;
            }
            var versionFileFoundResult = VersionFileFinder.FindVersionFile(
                Directory.GetCurrentDirectory(),
                repoFoundResult.Data);

            if (versionFileFoundResult is IErrorResult verErr)
            {
                Log.LogError(verErr.Message);
                return false;
            }
            
            var versionInfo = new VersionInfo(versionFileFoundResult.Data);
            
            var parseFullVersionResult = versionInfo.ParseVersion();
            if (parseFullVersionResult.Success)
            {
                InformationalVersion = parseFullVersionResult.Data;
            }
            Log.LogMessage("Chrono -> Resolving full version to "+ parseFullVersionResult.Data);
            var parseNumericVersionResult = versionInfo.GetNumericVersion();
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