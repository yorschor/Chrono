using System;
using Huxy;

namespace Chrono.Core.GitInfo
{
    public interface IGitInfoProvider
    {
        public string BranchName { get; }
        public string TagName { get; }
        public string CommitHash { get; }
        public string CommitShortHash { get; }

        public Result LoadGitInfo(bool allowDirtyRepo, string dirtyRepoPlaceholder = "");
    }
    public class GitInfo
    {
        public static IGitInfoProvider Get()
        {
            // Explicit, CI-agnostic opt-in. Wins over auto-detected providers.
            if (CheckEnv(GenericEnvProvider.CiVar)) return new GenericEnvProvider();
            if (CheckEnv("GITHUB_REPOSITORY")) return new GitHubProvider();
            if (CheckEnv("GITLAB_CI")) return new GitLabProvider();
            if (CheckEnv("CIRCLECI")) return new CircleCiProvider();
            if (CheckEnv("TRAVIS")) return new TravisCiProvider();
            if (CheckEnv("TF_BUILD")) return new AzureDevOpsProvider();
            if (CheckEnv("BITBUCKET_BUILD_NUMBER")) return new BitbucketProvider();
            if (CheckEnv("APPVEYOR")) return new AppVeyorProvider();
            if (CheckEnv("DRONE")) return new DroneCiProvider();
            if (CheckEnv("BUILDKITE")) return new BuildkiteProvider();

            return new GitRepoProvider(); // Default to local Git repository provider

            bool CheckEnv(string envvar) => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(envvar));
        }
    }
    
    /// <summary>
    /// CI-agnostic provider, enabled explicitly by setting CHRONO_CI to any non-empty value. Branch, tag and commit
    /// come from CHRONO_BRANCH, CHRONO_TAG and CHRONO_COMMIT_SHA. Use it for CI systems without a dedicated
    /// provider (for example OneDev: map @branch@, @tag@ and @commit_hash@ to these variables).
    /// CHRONO_COMMIT_SHA and at least one of CHRONO_BRANCH or CHRONO_TAG are required; otherwise loading fails.
    /// A leading "refs/heads/" or "refs/tags/" is stripped from branch and tag.
    /// </summary>
    public class GenericEnvProvider : IGitInfoProvider
    {
        public const string CiVar = "CHRONO_CI";
        public const string BranchVar = "CHRONO_BRANCH";
        public const string TagVar = "CHRONO_TAG";
        public const string CommitShaVar = "CHRONO_COMMIT_SHA";

        public string BranchName => StripRef(Read(BranchVar));
        public string TagName => StripRef(Read(TagVar));
        public string CommitHash => Read(CommitShaVar);
        public string CommitShortHash => CommitHash.Length > 7 ? CommitHash.Substring(0, 7) : CommitHash;

        public Result LoadGitInfo(bool allowDirtyRepo, string dirtyRepoPlaceholder = "")
        {
            if (string.IsNullOrEmpty(CommitHash))
            {
                return Result.Fail($"{CiVar} is set but {CommitShaVar} is empty or missing.");
            }

            if (string.IsNullOrEmpty(BranchName) && string.IsNullOrEmpty(TagName))
            {
                return Result.Fail($"{CiVar} is set but neither {BranchVar} nor {TagVar} is set.");
            }

            return Result.Ok();
        }

        private static string Read(string name) => Environment.GetEnvironmentVariable(name) ?? string.Empty;

        private static string StripRef(string value)
        {
            foreach (var prefix in new[] { "refs/heads/", "refs/tags/" })
            {
                if (value.StartsWith(prefix, StringComparison.Ordinal)) return value.Substring(prefix.Length);
            }

            return value;
        }
    }

    public class GitHubProvider : IGitInfoProvider
    {
        public string BranchName => Environment.GetEnvironmentVariable("GITHUB_REF");
        public string TagName => Environment.GetEnvironmentVariable("GITHUB_REF");
        public string CommitHash => Environment.GetEnvironmentVariable("GITHUB_SHA");
        public string CommitShortHash => CommitHash.Substring(0, 7);
        public Result LoadGitInfo(bool allowDirtyRepo, string dirtyRepoPlaceholder = "") => Result.Ok();
    }

    public class GitLabProvider : IGitInfoProvider
    {
        public string BranchName => Environment.GetEnvironmentVariable("CI_COMMIT_REF_NAME");
        public string TagName => Environment.GetEnvironmentVariable("CI_COMMIT_TAG");
        public string CommitHash => Environment.GetEnvironmentVariable("CI_COMMIT_SHA");
        public string CommitShortHash => Environment.GetEnvironmentVariable("CI_COMMIT_SHORT_SHA")?.Substring(0,7);
        public Result LoadGitInfo(bool allowDirtyRepo, string dirtyRepoPlaceholder = "") => Result.Ok();
    }

    public class CircleCiProvider : IGitInfoProvider
    {
        public string BranchName => Environment.GetEnvironmentVariable("CIRCLE_BRANCH");
        public string TagName => Environment.GetEnvironmentVariable("CIRCLE_TAG");
        public string CommitHash => Environment.GetEnvironmentVariable("CIRCLE_SHA1");
        public string CommitShortHash => CommitHash.Substring(0, 7);
        public Result LoadGitInfo(bool allowDirtyRepo, string dirtyRepoPlaceholder = "") => Result.Ok();
    }

    public class TravisCiProvider : IGitInfoProvider
    {
        public string BranchName => Environment.GetEnvironmentVariable("TRAVIS_BRANCH");
        public string TagName => Environment.GetEnvironmentVariable("TRAVIS_TAG");
        public string CommitHash => Environment.GetEnvironmentVariable("TRAVIS_COMMIT");
        public string CommitShortHash => CommitHash.Substring(0, 7);
        public Result LoadGitInfo(bool allowDirtyRepo, string dirtyRepoPlaceholder = "") => Result.Ok();
    }

    public class AzureDevOpsProvider : IGitInfoProvider
    {
        public string BranchName => Environment.GetEnvironmentVariable("BUILD_SOURCEBRANCHNAME");
        public string TagName => Environment.GetEnvironmentVariable("BUILD_SOURCEBRANCHNAME");
        public string CommitHash => Environment.GetEnvironmentVariable("BUILD_SOURCEVERSION");
        public string CommitShortHash => CommitHash.Substring(0, 7);
        public Result LoadGitInfo(bool allowDirtyRepo, string dirtyRepoPlaceholder = "") => Result.Ok();
    }

    public class BitbucketProvider : IGitInfoProvider
    {
        public string BranchName => Environment.GetEnvironmentVariable("BITBUCKET_BRANCH");
        public string TagName => Environment.GetEnvironmentVariable("BITBUCKET_TAG");
        public string CommitHash => Environment.GetEnvironmentVariable("BITBUCKET_COMMIT");
        public string CommitShortHash => CommitHash.Substring(0, 7);
        public Result LoadGitInfo(bool allowDirtyRepo, string dirtyRepoPlaceholder = "") => Result.Ok();
    }

    public class AppVeyorProvider : IGitInfoProvider
    {
        public string BranchName => Environment.GetEnvironmentVariable("APPVEYOR_REPO_BRANCH");
        public string TagName => Environment.GetEnvironmentVariable("APPVEYOR_REPO_TAG_NAME");
        public string CommitHash => Environment.GetEnvironmentVariable("APPVEYOR_REPO_COMMIT");
        public string CommitShortHash => CommitHash.Substring(0, 7);
        public Result LoadGitInfo(bool allowDirtyRepo, string dirtyRepoPlaceholder = "") => Result.Ok();
    }

    public class DroneCiProvider : IGitInfoProvider
    {
        public string BranchName => Environment.GetEnvironmentVariable("DRONE_BRANCH");
        public string TagName => Environment.GetEnvironmentVariable("DRONE_TAG");
        public string CommitHash => Environment.GetEnvironmentVariable("DRONE_COMMIT_SHA");
        public string CommitShortHash => CommitHash.Substring(0, 7);
        public Result LoadGitInfo(bool allowDirtyRepo, string dirtyRepoPlaceholder = "") => Result.Ok();
    }

    public class BuildkiteProvider : IGitInfoProvider
    {
        public string BranchName => Environment.GetEnvironmentVariable("BUILDKITE_BRANCH");
        public string TagName => Environment.GetEnvironmentVariable("BUILDKITE_TAG");
        public string CommitHash => Environment.GetEnvironmentVariable("BUILDKITE_COMMIT");
        public string CommitShortHash => CommitHash.Substring(0, 7);
        public Result LoadGitInfo(bool allowDirtyRepo, string dirtyRepoPlaceholder = "") => Result.Ok();
    }
    
}
