namespace Chrono.Core;

public class VersionFile
{
    public string Version { get; set; }
    public Default Default { get; set; }
    public Dictionary<string, Branch> Branches { get; set; }
    public Global Global { get; set; }
}

public class Default
{
    public string Versionschema { get; set; }
    public string Precision { get; set; }
    public string PrereleaseTag { get; set; }
    public Release Release { get; set; }
}

public class Release
{
    public List<string> Match { get; set; }
    public string Versionschema { get; set; }
}

public class Branch
{
    public string BranchRegexMatcher { get; set; }
    public string Versionschema { get; set; }
    public string Precision { get; set; }
    public string PrereleaseTag { get; set; }
}

public class Global
{
    public bool Metadata { get; set; }
}