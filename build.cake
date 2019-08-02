#tool nuget:?package=xunit.runner.console&version=2.4.1

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

// a bit of logic to create the version number:
//  - input                     = 1.2.3.4
//  - package version           = 1.2.3
//  - preview package version   = 1.2.3-preview-4
var version = Version.Parse(Argument("packageversion", EnvironmentVariable("BUILD_BUILDNUMBER") ?? EnvironmentVariable("APPVEYOR_BUILD_VERSION") ?? "1.0.0.0"));
var previewNumber   = version.Revision;
var assemblyVersion = $"{version.Major}.0.0.0";
var fileVersion     = $"{version.Major}.{version.Minor}.{version.Build}.0";
var infoVersion     = $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
var packageVersion  = $"{version.Major}.{version.Minor}.{version.Build}";

Task("Build")
    .Does(() =>
{
    var settings = new MSBuildSettings()
        .SetConfiguration(configuration)
        .SetVerbosity(Verbosity.Minimal)
        .WithRestore()
        .WithProperty("Version", assemblyVersion)
        .WithProperty("FileVersion", fileVersion)
        .WithProperty("InformationalVersion", infoVersion);

    MSBuild("Mono.ApiTools.NuGetDiff.sln", settings);
});

Task("Pack")
    .IsDependentOn("Build")
    .Does(() =>
{
    var settings = new MSBuildSettings()
        .SetConfiguration(configuration)
        .SetVerbosity(Verbosity.Minimal)
        .WithProperty("IncludeSymbols", "True")
        .WithProperty("PackageVersion", packageVersion)
        .WithProperty("Version", assemblyVersion)
        .WithProperty("FileVersion", fileVersion)
        .WithProperty("InformationalVersion", infoVersion)
        .WithProperty("PackageOutputPath", MakeAbsolute((DirectoryPath)"./output/").FullPath);

    // stable
    settings = settings.WithTarget("Pack");
    MSBuild("Mono.ApiTools.NuGetDiff/Mono.ApiTools.NuGetDiff.csproj", settings);

    // pre-release
    settings.WithProperty("PackageVersion", packageVersion + "-preview-" + previewNumber);
    MSBuild("Mono.ApiTools.NuGetDiff/Mono.ApiTools.NuGetDiff.csproj", settings);
});

Task("Test")
    .IsDependentOn("Build")
    .Does(() =>
{
    Information("Running tests...");
    DotNetCoreTest("Mono.ApiTools.NuGetDiff.Tests/Mono.ApiTools.NuGetDiff.Tests.csproj");
});

Task("Default")
    .IsDependentOn("Build")
    .IsDependentOn("Pack")
    .IsDependentOn("Test");

RunTarget(target);
