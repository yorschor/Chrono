using System;
using Huxy;

namespace Chrono.Core.GitInfo
{
    public interface IGitInfoProvider
    {
        public string BranchName { get; }
        public string TagName { get; }
        public string CommitShortHash { get; }

        public Result LoadGitInfo(bool allowDirtyRepo, string dirtyRepoPlaceholder = "");
    }
    public class GitInfo
    {
        public static IGitInfoProvider Get()
        {
            if (CheckEnv("GITHUB_ACTIONS")) return new GitHubProvider();
            if (CheckEnv("GITLAB_CI")) return new GitLabProvider();
            if (CheckEnv("CIRCLECI")) return new CircleCiProvider();
            if (CheckEnv("TRAVIS")) return new TravisCiProvider();
            if (CheckEnv("JENKINS_URL")) return new JenkinsProvider();
            if (CheckEnv("TF_BUILD")) return new AzureDevOpsProvider();
            if (CheckEnv("BITBUCKET_BUILD_NUMBER")) return new BitbucketProvider();
            if (CheckEnv("TEAMCITY_VERSION")) return new TeamCityProvider();
            if (CheckEnv("APPVEYOR")) return new AppVeyorProvider();
            if (CheckEnv("DRONE")) return new DroneCiProvider();
            if (CheckEnv("BUILDKITE")) return new BuildkiteProvider();

            return new GitRepoProvider(); // Default to local Git repository provider

            bool CheckEnv(string envvar) => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(envvar));
        }
    }
    
    public class GitHubProvider : IGitInfoProvider
    {
        public string BranchName => Environment.GetEnvironmentVariable("GITHUB_REF_NAME");
        public string TagName => Environment.GetEnvironmentVariable("GITHUB_REF_NAME"); // Check if it's a tag
        public string CommitShortHash => Environment.GetEnvironmentVariable("GITHUB_SHA");
        public Result LoadGitInfo(bool allowDirtyRepo, string dirtyRepoPlaceholder = "") => Result.Ok();
    }

    public class GitLabProvider : IGitInfoProvider
    {
        public string BranchName => Environment.GetEnvironmentVariable("CI_COMMIT_REF_NAME");
        public string TagName => Environment.GetEnvironmentVariable("CI_COMMIT_TAG");
        public string CommitShortHash => Environment.GetEnvironmentVariable("CI_COMMIT_SHA");
        public Result LoadGitInfo(bool allowDirtyRepo, string dirtyRepoPlaceholder = "") => Result.Ok();
    }

    public class CircleCiProvider : IGitInfoProvider
    {
        public string BranchName => Environment.GetEnvironmentVariable("CIRCLE_BRANCH");
        public string TagName => Environment.GetEnvironmentVariable("CIRCLE_TAG");
        public string CommitShortHash => Environment.GetEnvironmentVariable("CIRCLE_SHA1");
        public Result LoadGitInfo(bool allowDirtyRepo, string dirtyRepoPlaceholder = "") => Result.Ok();
    }

    public class TravisCiProvider : IGitInfoProvider
    {
        public string BranchName => Environment.GetEnvironmentVariable("TRAVIS_BRANCH");
        public string TagName => Environment.GetEnvironmentVariable("TRAVIS_TAG");
        public string CommitShortHash => Environment.GetEnvironmentVariable("TRAVIS_COMMIT");
        public Result LoadGitInfo(bool allowDirtyRepo, string dirtyRepoPlaceholder = "") => Result.Ok();
    }

    public class JenkinsProvider : IGitInfoProvider
    {
        public string BranchName => Environment.GetEnvironmentVariable("BRANCH_NAME");
        public string TagName => Environment.GetEnvironmentVariable("GIT_TAG");
        public string CommitShortHash => Environment.GetEnvironmentVariable("GIT_COMMIT");
        public Result LoadGitInfo(bool allowDirtyRepo, string dirtyRepoPlaceholder = "") => Result.Ok();
    }

    public class AzureDevOpsProvider : IGitInfoProvider
    {
        public string BranchName => Environment.GetEnvironmentVariable("BUILD_SOURCEBRANCHNAME");
        public string TagName => null; // Parse from BUILD_SOURCEBRANCH if needed
        public string CommitShortHash => Environment.GetEnvironmentVariable("BUILD_SOURCEVERSION");
        public Result LoadGitInfo(bool allowDirtyRepo, string dirtyRepoPlaceholder = "") => Result.Ok();
    }

    public class BitbucketProvider : IGitInfoProvider
    {
        public string BranchName => Environment.GetEnvironmentVariable("BITBUCKET_BRANCH");
        public string TagName => Environment.GetEnvironmentVariable("BITBUCKET_TAG");
        public string CommitShortHash => Environment.GetEnvironmentVariable("BITBUCKET_COMMIT");
        public Result LoadGitInfo(bool allowDirtyRepo, string dirtyRepoPlaceholder = "") => Result.Ok();
    }

    public class TeamCityProvider : IGitInfoProvider
    {
        public string BranchName => Environment.GetEnvironmentVariable("TEAMCITY_BRANCH");
        public string TagName => null; // Not directly available
        public string CommitShortHash => Environment.GetEnvironmentVariable("BUILD_VCS_NUMBER");
        public Result LoadGitInfo(bool allowDirtyRepo, string dirtyRepoPlaceholder = "") => Result.Ok();
    }

    public class AppVeyorProvider : IGitInfoProvider
    {
        public string BranchName => Environment.GetEnvironmentVariable("APPVEYOR_REPO_BRANCH");
        public string TagName => Environment.GetEnvironmentVariable("APPVEYOR_REPO_TAG_NAME");
        public string CommitShortHash => Environment.GetEnvironmentVariable("APPVEYOR_REPO_COMMIT");
        public Result LoadGitInfo(bool allowDirtyRepo, string dirtyRepoPlaceholder = "") => Result.Ok();
    }

    public class DroneCiProvider : IGitInfoProvider
    {
        public string BranchName => Environment.GetEnvironmentVariable("DRONE_BRANCH");
        public string TagName => Environment.GetEnvironmentVariable("DRONE_TAG");
        public string CommitShortHash => Environment.GetEnvironmentVariable("DRONE_COMMIT_SHA");
        public Result LoadGitInfo(bool allowDirtyRepo, string dirtyRepoPlaceholder = "") => Result.Ok();
    }

    public class BuildkiteProvider : IGitInfoProvider
    {
        public string BranchName => Environment.GetEnvironmentVariable("BUILDKITE_BRANCH");
        public string TagName => Environment.GetEnvironmentVariable("BUILDKITE_TAG");
        public string CommitShortHash => Environment.GetEnvironmentVariable("BUILDKITE_COMMIT");
        public Result LoadGitInfo(bool allowDirtyRepo, string dirtyRepoPlaceholder = "") => Result.Ok();
    }
    
}
