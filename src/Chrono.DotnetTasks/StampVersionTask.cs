using Chrono.Core;
using LibGit2Sharp;
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
            var repoPath = Repository.Discover(Environment.CurrentDirectory);
            const string versionFileName = "version.yml";
            var p = Path.Combine(repoPath, "..", versionFileName);
            if (!File.Exists(p))
            {
                Log.LogWarning("No Git repository found!");
                return false;
            }

            var versionInfo = new VersionInfo(p);

            var parseFullVersionResult = versionInfo.ParseVersion();
            if (parseFullVersionResult.Success)
            {
                InformationalVersion = parseFullVersionResult.Data;
            }

            var parseNumericVersionResult = versionInfo.GetNumericVersion();
            if (parseNumericVersionResult.Success)
            {
                AssemblyVersion = parseNumericVersionResult.Data;
                FileVersion = parseNumericVersionResult.Data;
                PackageVersion = parseNumericVersionResult.Data;
            }

            return true;
        }
        catch (Exception ex)
        {
            Log.LogErrorFromException(ex);
            return false;
        }
    }
}