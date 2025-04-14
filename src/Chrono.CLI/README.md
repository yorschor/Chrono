# Chrono.CLI 

## 📚 About

[Chrono](https://github.com/yorschor/Chrono) is a git versioning tool with a focus on being customizable and easy to configure.

It is inspired by the likes of [GitVersion](https://github.com/GitTools/GitVersion) and [Nerdbank.GitVersioning](https://github.com/dotnet/Nerdbank.GitVersioning).

### 🚀 Getting started 

Install/Update the dotnet tool using

```
dotnet tool install -g Chrono
```

Next up create a version.yml (for now only a version.yml at the root of your git repo is supported)

For the content of version.yml either checkout the default example below.

```yml
version: 1.0.0
default:
  versionSchema: '{major}.{minor}.{patch}.{build}[-]{branch}[.]{commitShortHash}'
  precision: build
  prereleaseTag: dev
  release:
    match:
      - ^release/.*
    versionSchema: '{major}.{minor}.{patch}'

```

or read the full [documentation](https://github.com/yorschor/Chrono/blob/trunk/doc/Version.adoc).