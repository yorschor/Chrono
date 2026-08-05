# Chrono.DotnetTasks

## 📚 About

[Chrono](https://github.com/yorschor/Chrono) is a git versioning tool with a focus on being customizable and easy to configure.

Based on your configured templates it easily retrieves version information based on the current state or your repo.

It provides a CLI and a NugetPackage for automatic version stamping.

The DotnetTasks project contains a MSBuild task that hooks into the build process and automatically sets the version of your project according to the version.yml file.


### 🚀 Getting started 

Create a version.yml
For the content of version.yml either checkout the default example below.

```yml
version: 1.0.0.0
default:
  versionSchema: '{major}.{minor}.{patch}.{build}[-]{branch}[.]g{commitShortHash}'
  precision: build
  prereleaseTag: dev
  release:
    match:
      - ^release/.*
    versionSchema: '{major}.{minor}.{patch}'

```

or read the full [documentation](https://github.com/yorschor/Chrono/blob/trunk/doc/Version.adoc).                       
    
### 🕹️ ️Automatic Version stamping (dotnet) 

Prerequisites:
- dotnet 6.0+ or .NET Framework 4.7.2+
- SDK style project files
- No existing AssemblyVersion.cs files or the likes present
- A version.yml file in the root of your git repo


If you have the cli tool installed you can run

```[Chrono.DotnetTasks.csproj](Chrono.DotnetTasks.csproj)
chrono init
```

in your project root.

This adds a Directory.Build.props file to the directory which takes care of including the DotnetTasks package into your projects.

with the following content:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="Current" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <ItemGroup>
        <PackageReference Include="Chrono.DotnetTasks" Condition="!Exists('packages.config')" Version="{yourInstalledChronoVersion}" PrivateAssets="all"/>
    </ItemGroup>
    <PropertyGroup>
        <IncludeSourceRevisionInInformationalVersion>false</IncludeSourceRevisionInInformationalVersion>
        <GenerateAssemblyInfo>true</GenerateAssemblyInfo>
    </PropertyGroup>
</Project>
```

This ensures that every project includes the DotnetTasks project and also sets some interfering properties to false.
