namespace Chrono.Core.Helpers;

public class CiHelper
{
    public static bool IsCiBuild()
    {
        return IsGitHubActions() || IsGitLabCi() || IsJenkinsCi() || IsAzurePipelines() || IsTeamCity();
    }
    
    static bool IsGitLabCi()
    {
        return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITLAB_CI"));
    }
    static bool IsJenkinsCi()
    {
        return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("JENKINS_URL"));
    }

    static bool IsAzurePipelines()
    {
        return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TF_BUILD"));
    }

    static bool IsGitHubActions()
    {
        return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
    }

    static bool IsTeamCity()
    {
        return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TEAMCITY_VERSION"));
    }
}