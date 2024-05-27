using Nuke.Common;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;

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
    [Parameter("The version suffix that should be appended to the version")] readonly string VersionSuffix = "";

    [Solution] readonly Solution Solution;

    [Parameter] [Secret] readonly string NuGetApiKey;

    readonly GitHubActions GitHubActions = GitHubActions.Instance;

    const string ProjectName = "Chrono";
    const string TargetProjectName = "Chrono.DotnetTasks";
    const string TestLibs = "Chrono.TestLib.*";
    readonly AbsolutePath PackagesDirectory = RootDirectory / "PackageDirectory";
    readonly AbsolutePath SourceDirectory = RootDirectory / "src";

    Target Compile => t => t
        .Executes(() =>
        {
            DotNetTasks.DotNetPublish(s => s
                .SetProject(Solution.GetProject(ProjectName))
                .SetConfiguration(Configuration)
            );
        });

    Target Pack => t => t
        .DependsOn(Compile)
        .Produces(PackagesDirectory / "*.nupkg")
        .Executes(() =>
        {
            PackagesDirectory.DeleteDirectory();
            Version = string.IsNullOrEmpty(GitHubActions?.RefName) ? Version : GitHubActions.RefName.TrimStart('v');
            DotNetTasks.DotNetPack(s => s
                .SetProject(SourceDirectory / ProjectName)
                .SetVersionPrefix(Version)
                .SetVersionSuffix(VersionSuffix)
                .SetOutputDirectory(PackagesDirectory)
            );
            DotNetTasks.DotNetPack(s => s
                .SetProject(SourceDirectory / TargetProjectName)
                .SetVersionPrefix(Version)
                .SetVersionSuffix(VersionSuffix)
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

    // Target BuildTestLibs => t => t
    //     .Executes(() =>
    //     {
    //         // DotNetTasks.DotNetBuild(s => s
    //         //     .SetProjectFile(Solution.GetProject(TestDotnet48ProjectName).Path)
    //         //     .SetNoCache(true));
    //         var testLibs = Solution.GetAllProjects(TestLibs);
    //         foreach (var test in testLibs)
    //         {
    //             DotNetTasks.DotNetPublish(s => s
    //                 .SetProject(Solution.GetProject(test))
    //                 .SetNoCache(true));
    //         }
    //     });
}