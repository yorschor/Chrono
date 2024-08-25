using System;
using System.IO;
using System.Text.RegularExpressions;
using Chrono.Core;
using Chrono.Core.Helpers;
using Huxy;
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
    readonly AbsolutePath PackagesDirectory = RootDirectory / "out";
    readonly AbsolutePath SourceDirectory = RootDirectory / "src";

    Target PrepareVersions => t => t.Executes(() =>
    {
        try
        {
            var infoGetResult = VersionInfo.Get();
            if (infoGetResult is IErrorResult)
            {
                Log.Error(infoGetResult.Message);
                return false;
            }

            var versionInfo = infoGetResult.Data;

            var parseFullVersionResult = versionInfo.GetVersion();
            if (parseFullVersionResult.Success)
            {
                Version = parseFullVersionResult.Data;
            }

            Log.Information("Chrono -> Resolving full version to " + parseFullVersionResult.Data);
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

    Target AdjustDependencies => t => t
        .DependsOn(Compile)
        .Executes(() =>
        {
            var p = Solution.GetProject(TargetProjectName)?.Directory;
            var net472 = p / "bin" / Configuration / "net472" / "publish" / "LibGit2Sharp.dll.config";
            var net6 = p / "bin" / Configuration / "net6.0" / "publish" / "Chrono.DotnetTasks.deps.json";
            
            AdjustDllConfigPaths(net472);
            AdjustDllConfigPaths(net6);
        });

    void AdjustDllConfigPaths(string configFilePath)
    {
        if (File.Exists(configFilePath))
        {
            var content = File.ReadAllText(configFilePath);
                const string pattern = @"runtimes/(?<platform>[^/]+)/native/(?<filename>[^""]+)";
                const string replacement = @"../MSBuildFull/lib/${platform}/${filename}";
                content = Regex.Replace(content, pattern, replacement);

                content = content.Replace("../MSBuildFull/lib/win-arm64/", "../MSBuildFull/lib/win32/arm64/");
                content = content.Replace("../MSBuildFull/lib/win-x64/", "../MSBuildFull/lib/win32/x64/");
                content = content.Replace("../MSBuildFull/lib/win-x86/", "../MSBuildFull/lib/win32/x86/");
            
            File.WriteAllText(configFilePath, content);
        }
        else
        {
            Logger.Warn($"File not found: {configFilePath}");
        }
    }

    Target Pack => t => t
        .DependsOn(AdjustDependencies)
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
            DotNetTasks.DotNet(
                $"tool update -g {ProjectName} --add-source {localNugetStoreName} --no-cache --ignore-failed-sources --version {Version}");
        });
}