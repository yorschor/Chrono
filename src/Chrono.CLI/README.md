# Chrono.CLI 

## 📚 About

[Chrono](https://github.com/yorschor/Chrono) is a git versioning tool with a focus on being customizable and easy to configure.

Based on your configured templates it easily retrieves version information based on the current state or your repo.

It provides a CLI and a NugetPackage for automatic version stamping.
For a detailed description please consider the [documentation](https://github.com/yorschor/Chrono/blob/trunk/doc/CLI.adoc)

The CLI consists of a few commands that can be split into two categories.
Version and Git commands.
Version commands like _**get/set/bump**_ are used to retrieve, set or bump the version.

Git commands like _**branch/release/tag**_ are used to create a new branch or tag based on the template provided in the version file.

### 🚀 Getting started
In order to use Chrono you first need to have a version.yml file in the root of your project.
Secondly you also either need to have the Chrono CLI installed or use the Chrono.DotnetVersioning package in your project.

The CLI provides a convenience command "_chrono init_" to create a version.yml and optionally a Directory.Build.props file for setting up initial versioning.

Next up create a version.yml (for now only a version.yml at the root of your git repo is supported)

For the content of version.yml either checkout the default example below.

```yml
version: 1.0.0
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