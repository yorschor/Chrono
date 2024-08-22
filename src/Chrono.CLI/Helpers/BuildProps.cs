namespace Chrono.Helpers;

public class BuildProps
{
    public static string Get(string version = "0.8.0")
    {
        return $"""
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
    }
}