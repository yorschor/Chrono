namespace Chrono.InitHelpers;

public class BuildProps
{
    public static string Get()
    {
        return @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""Current"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <ItemGroup>
    <PackageReference Include=""Chrono.DotnetTasks"" Condition=""!Exists('packages.config')"" Version=""0.42.0"" PrivateAssets=""all""/>
  </ItemGroup>
  <PropertyGroup>
    <IncludeSourceRevisionInInformationalVersion>false</IncludeSourceRevisionInInformationalVersion>
  </PropertyGroup>
</Project>";
    }
}