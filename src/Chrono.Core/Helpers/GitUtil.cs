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
            return new ErrorResult<string>($"Not Repo found from starting directory {startDir}");
        }
        return new SuccessResult<string>(rootPath);
    }

    public static Result TagCommit()
    {
        var infoFileResult = VersionInfo.Get();
        if (infoFileResult is IErrorResult infoResErr)
        {
            return new ErrorResult(infoResErr.Message);
        }

        var parseRes= infoFileResult.Data.ParseVersion();
        if (parseRes is IErrorResult parseErr)
        {
            return new ErrorResult(parseErr.Message);
        }
        
        //Tag current Commit 
        //Change Version to next one
        //Commit version.yml
        return null;
    }
}