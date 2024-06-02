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
            return Result.Error<string>($"Not Repo found from starting directory {startDir}");
        }
        return Result.Ok(rootPath);
    }

    public static Result TagCommit()
    {
        var infoFileResult = VersionInfo.Get();
        if (infoFileResult is IErrorResult infoResErr)
        {
            return Result.Error(infoResErr.Message);
        }

        var parseRes= infoFileResult.Data.ParseVersion();
        if (parseRes is IErrorResult parseErr)
        {
            return Result.Error(parseErr.Message);
        }
        
        //Tag current Commit 
        //Change Version to next one
        //Commit version.yml
        return null;
    }
}