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
var previewVersion = packageVersion + "-preview." + previewNumber;

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
        .WithProperty("PackageOutputPath", MakeAbsolute((DirectoryPath)"./output/").FullPath)
        .WithTarget("Pack");

    // stable
    MSBuild("Mono.ApiTools.NuGetDiff/Mono.ApiTools.NuGetDiff.csproj", settings);
    MSBuild("api-tools/api-tools.csproj", settings);

    // pre-release
    settings.WithProperty("PackageVersion", previewVersion);
    MSBuild("Mono.ApiTools.NuGetDiff/Mono.ApiTools.NuGetDiff.csproj", settings);
    MSBuild("api-tools/api-tools.csproj", settings);
});

Task("Test")
    .IsDependentOn("Build")
    .Does(() =>
{
    Information("Running unit tests...");
    DotNetCoreTest("Mono.ApiTools.NuGetDiff.Tests/Mono.ApiTools.NuGetDiff.Tests.csproj", new DotNetCoreTestSettings {
        Logger = "trx"
    });

    Information("Running app tests...");
    var app = $"api-tools/bin/{configuration}/netcoreapp2.2/api-tools.dll";
    var id = "Mono.ApiTools.NuGetDiff";
    DotNetCoreExecute(app, $"nuget-diff ./output/{id}.{packageVersion}.nupkg --latest --cache=externals --output=test-output");
    DotNetCoreExecute(app, $"nuget-diff ./output/{id}.{previewVersion}.nupkg ./output/{id}.{packageVersion}.nupkg --output=test-output");
});

Task("Default")
    .IsDependentOn("Build")
    .IsDependentOn("Pack")
    .IsDependentOn("Test");

RunTarget(target);
