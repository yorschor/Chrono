using System;
using System.IO;
using Chrono.Core;
using Chrono.Core.Helpers;
using Nuke.Common;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using Serilog;

[GitHubActions(
    "continuous",
    GitHubActionsImage.UbuntuLatest,
    On = [GitHubActionsTrigger.Push],
    InvokedTargets = [nameof(Compile)])]
[GitHubActions(
    "tagPush",
    GitHubActionsImage.UbuntuLatest,
    OnPushTags = ["v*"],
    ImportSecrets = [nameof(NuGetApiKey)],
    InvokedTargets = [nameof(PushNugetPackage)])]
class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Parameter("The version to use for the build")] string Version = "0.42.0";
    [Parameter("The numeric version excluding e.g. prerelease branch tags")] string NumericVersion = "0.42.0";

    [Solution] readonly Solution Solution;

    [Parameter] [Secret] readonly string NuGetApiKey;

    readonly GitHubActions GitHubActions = GitHubActions.Instance;

    const string ProjectName = "Chrono.CLI";
    const string TargetProjectName = "Chrono.DotnetTasks";
    const string TestLibs = "Chrono.TestLib.*";
    readonly AbsolutePath PackagesDirectory = RootDirectory / "PackageDirectory";
    readonly AbsolutePath SourceDirectory = RootDirectory / "src";

    Target PrepareVersions => t => t.
        Executes(() =>
            {
                try
                {
                    var repoFoundResult = GitUtil.GetRepoRootPath();
                    if (repoFoundResult is IErrorResult repoErr)
                    {
                        Log.Error(repoErr.Message);
                        return false;
                    }
                    var versionFileFoundResult = VersionFile.Find(
                        Directory.GetCurrentDirectory(),
                        repoFoundResult.Data);

                    if (versionFileFoundResult is IErrorResult verErr)
                    {
                        Log.Error(verErr.Message);
                        return false;
                    }
            
                    var versionInfo = new VersionInfo(versionFileFoundResult.Data);
            
                    var parseFullVersionResult = versionInfo.GetVersion();
                    if (parseFullVersionResult.Success)
                    {
                        Version = parseFullVersionResult.Data;
                    }
                    Log.Information("Chrono -> Resolving full version to "+ parseFullVersionResult.Data);
                    var parseNumericVersionResult = versionInfo.GetNumericVersion();
                    if (parseNumericVersionResult.Success)
                    {
                        NumericVersion = parseNumericVersionResult.Data;
                    }
                    Log.Information("Chrono -> Resolving numeric version to " + parseNumericVersionResult.Data);

                    return true;
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Message);
                    return false;
                }
            });
    
    Target Compile => t => t
        .DependsOn(PrepareVersions)
        .Executes(() =>
        {
            DotNetTasks.DotNetPublish(s => s
                .SetProject(Solution.GetProject(ProjectName))
                .SetConfiguration(Configuration)
                .SetSelfContained(true)
            );
            DotNetTasks.DotNetPublish(s => s
                .SetProject(Solution.GetProject(TargetProjectName))
                .SetConfiguration(Configuration)
                .SetVersion(Version)
                .SetAssemblyVersion(NumericVersion)
                .SetFileVersion(NumericVersion)
                .SetSelfContained(true)
                .SetFramework("net6.0")
            );
            DotNetTasks.DotNetPublish(s => s
                .SetProject(Solution.GetProject(TargetProjectName))
                .SetConfiguration(Configuration)
                .SetVersion(Version)
                .SetAssemblyVersion(NumericVersion)
                .SetFileVersion(NumericVersion)
                .SetSelfContained(true)
                .SetFramework("net472")
            );
        });

    Target Pack => t => t
        .DependsOn(Compile)
        .Produces(PackagesDirectory / "*.nupkg")
        .Executes(() =>
        {
            PackagesDirectory.DeleteDirectory();
            // Version = string.IsNullOrEmpty(GitHubActions?.RefName) ? Version : GitHubActions.RefName.TrimStart('v');
            DotNetTasks.DotNetPack(s => s
                .SetProject(SourceDirectory / ProjectName)
                .SetVersion(Version)
                .SetAssemblyVersion(NumericVersion)
                .SetFileVersion(NumericVersion)
                .SetOutputDirectory(PackagesDirectory)
            );
            DotNetTasks.DotNetPack(s => s
                .SetProject(SourceDirectory / TargetProjectName)
                .SetVersion(Version)
                .SetAssemblyVersion(NumericVersion)
                .SetFileVersion(NumericVersion)
                .SetOutputDirectory(PackagesDirectory)
            );
        });

    Target PushNugetPackage => t => t
        .DependsOn(Pack)
        .Executes(() =>
        {
            DotNetTasks.DotNetNuGetPush(s => s
                .SetTargetPath(PackagesDirectory / "*.nupkg")
                .SetApiKey(NuGetApiKey)
                .SetSource("https://www.nuget.org/"));
        });

    Target LocalDeploy => t => t
        .DependsOn(Pack)
        .Executes(() =>
        {
            var localNugetStoreName = "LocalNuggets";
            DotNetTasks.DotNetNuGetPush(s => s
                .SetTargetPath(PackagesDirectory / "*.nupkg")
                .SetApiKey(NuGetApiKey)
                .SetSource(localNugetStoreName));
            DotNetTasks.DotNet($"tool update -g {ProjectName} --add-source {localNugetStoreName} --no-cache --ignore-failed-sources");
        });
}