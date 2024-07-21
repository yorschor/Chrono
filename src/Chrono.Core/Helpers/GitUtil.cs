using Huxy;
using LibGit2Sharp;
using NLog;
using NLog.Fluent;

namespace Chrono.Core.Helpers;

public class GitUtil
{
    public static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    
    public static Result<string> GetRepoRootPath(string startDir ="")
    {
        if (string.IsNullOrEmpty(startDir))
        {
            startDir = Directory.GetCurrentDirectory();
        }
        var repoPath = Repository.Discover(startDir);
        Logger.Trace($"Discorvered Repo: {repoPath}");
        var rootPath = Directory.GetParent(repoPath)?.Parent?.FullName;
        Logger.Trace($"Root: {rootPath}");
        if (string.IsNullOrEmpty(rootPath))
        {
            return Result.Error<string>($"No Repo found from starting directory {startDir}");
        }
        return Result.Ok(rootPath);
    }

    /// <summary>
    /// Tags the current commit with the current version as defined in the version file
    /// </summary>
    /// <returns></returns>
    public static Result TagCommit()
    {
        var rootPathResult = GetRepoRootPath();
        if (rootPathResult is IErrorResult)
        {
            return Result.Error(rootPathResult);
        }
        var infoFileResult = VersionInfo.Get();
        if (infoFileResult is IErrorResult)
        {
            return Result.Error(infoFileResult);
        }

        var parseRes= infoFileResult.Data.GetVersion();
        if (parseRes is IErrorResult)
        {
            return Result.Error(parseRes);
        }
        
        // //Tag current Commit
        // var repo = new Repository(rootPathResult.Data);
        // repo.Tags.Add($"v{parseRes.Data.Version}");
        // Logger.Info($"Tagged commit with version {parseRes.Data.Version}");
        // //Change Version to next one
        // repo.Commit($"Version {parseRes.Data.Version}", repo.Head.Tip, repo.Head.Tip);
        // Logger.Info($"Changed version to {parseRes.Data.NextVersion}");
        // //Commit version.yml
        // var versionFile = Path.Combine(rootPathResult.Data, "version.yml");
        return null;
    }
}