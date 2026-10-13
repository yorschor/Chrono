namespace Chrono.Helpers;

public enum VersionFileVariants
{
	LocalMinimal,
	LocalDefault,
	LocalExpanded,
	InheritedDefault,
	InheritedExpanded,
}

public static class InitTemplates
{
	public static string GetBuildProps(string version = "1.0.0", string versionFileName = "version.yml")
	{
		var props = $"""
		        <?xml version="1.0" encoding="utf-8"?>
		        <Project ToolsVersion="Current" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
		          <ItemGroup>
		            <PackageReference Include="Chrono.DotnetTasks" Condition="!Exists('packages.config')" Version="{version}" PrivateAssets="all"/>
		          </ItemGroup>
		          <PropertyGroup>
		            <IncludeSourceRevisionInInformationalVersion>false</IncludeSourceRevisionInInformationalVersion>
		            <GenerateAssemblyInfo>true</GenerateAssemblyInfo>
		          </PropertyGroup>
		        </Project>
		        """;

		// Only a non-default version file name needs to be told to the MSBuild task.
		return string.Equals(versionFileName, "version.yml", StringComparison.OrdinalIgnoreCase)
			? props
			: props.Replace("<GenerateAssemblyInfo>true</GenerateAssemblyInfo>",
				$"<GenerateAssemblyInfo>true</GenerateAssemblyInfo>\n    <ChronoVersionFile>{versionFileName}</ChronoVersionFile>");
	}

	public static string GetVersionFile(VersionFileVariants variant, string version = "0.0.1.0")
	{
		return variant switch
		{
			VersionFileVariants.LocalMinimal => GetLocalMinimalVersionFile(version),
			VersionFileVariants.LocalDefault => GetLocalDefaultVersionFile(version),
			VersionFileVariants.LocalExpanded => GetLocalExpandedVersionFile(version),
			VersionFileVariants.InheritedDefault => GetInheritedVersionFile(version, "sensibleDefault"),
			VersionFileVariants.InheritedExpanded => GetInheritedVersionFile(version, "expanded"),
			_ => string.Empty
		};
	}

	private static string GetLocalMinimalVersionFile(string version)
	{
		return """
		       version: {{version}}
		       default:
		         versionSchema: '{major}.{minor}.{patch}'
		       """.Replace("{{version}}", version);
	}

	private static string GetLocalDefaultVersionFile(string version)
	{
		return """
		       version: {{version}}
		       default:
		         versionSchema: '{major}.{minor}.{patch}.{build}[-]{branch}[.]{commitShortHash}'
		         precision: build
		         prereleaseTag: dev
		         release:
		           match:
		             - ^release/.*
		           versionSchema: '{major}.{minor}.{patch}'
		       """.Replace("{{version}}", version);
	}

	private static string GetLocalExpandedVersionFile(string version)
	{
		return """
		       version: {{version}}
		       default:
		         versionSchema: '{major}.{minor}.{patch}.{build}[-]{branch}[.]{commitShortHash}'
		         precision: minor
		         prereleaseTag: local
		         release:
		           match:
		             - tag::^v.*
		           versionSchema: '{major}.{minor}.{patch}'
		       branches:
		         release:
		           match:
		             - ^release/.*
		           versionSchema: '{major}.{minor}.{patch}-{prereleaseTag}-{commitShortHash}'
		           precision: patch
		           prereleaseTag: rc
		         development:
		           match:
		             - ^development.*
		           versionSchema: '{major}.{minor}.{patch}.{build}-{prereleaseTag}-{commitShortHash}'
		           precision: build
		           prereleaseTag: dev
		       """.Replace("{{version}}", version);
	}

	private static string GetInheritedVersionFile(string version, string variant)
	{
		return $"""
		        version: {version}
		        default:
		          inheritFrom: https://raw.githubusercontent.com/yorschor/Chrono/refs/heads/trunk/doc/examples/{variant}.yml
		        """;
	}
}