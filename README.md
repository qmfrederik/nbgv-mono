# nbgv-mono

This packages contains magic to make `NerdBank.GitVersioning` work when used with Visual Code
on recent versions of Debian and Ubuntu Linux.

Two steps are sufficient to make this work.

1. Add a `PackageReference` to `nbgv-mono` version `1.0.3` or higher
2. Remove `~/.nuget/packages/nerdbank.gitversioning/3.0.50/build/MSBuildFull/LibGit2Sharp.dll.config`

The easiest way to test whether you are able to build from within Visual Studio Code, is to launch
`msbuild` which ships as part of Mono:

1. [Install Mono](https://www.mono-project.com/download/stable/)
2. Run `msbuild` in your project directory. This will invoke Mono's copy of MSBuild. To run
   the copy of `msbuild` which ships with .NET Core, run `dotnet build` or `dotnet msbuild`.