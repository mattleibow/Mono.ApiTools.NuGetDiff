var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var prerelease = Argument("prerelease", true);

// a bit of logic to create the version number:
//  - input                     = 1.2.3.4
//  - package version           = 1.2.3
//  - preview package version   = 1.2.3-preview.4
var version         = Version.Parse(Argument("packageversion", "1.0.0.0"));
var previewNumber   = version.Revision;
var assemblyVersion = $"{version.Major}.0.0.0";
var fileVersion     = $"{version.Major}.{version.Minor}.{version.Build}.0";
var infoVersion     = $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
var packageVersion  = $"{version.Major}.{version.Minor}.{version.Build}";
var previewVersion  = packageVersion + "-preview." + previewNumber;

Task("Build")
    .Does(() =>
{
    var msbuildSettings = new DotNetMSBuildSettings()
        .SetConfiguration(configuration)
        .WithProperty("Version", assemblyVersion)
        .WithProperty("FileVersion", fileVersion)
        .WithProperty("InformationalVersion", infoVersion);
    var settings = new DotNetBuildSettings
    {
        MSBuildSettings = msbuildSettings
    };

    DotNetBuild("Mono.ApiTools.NuGetDiff.sln", settings);
});

Task("Pack")
    .IsDependentOn("Build")
    .Does(() =>
{
    var msbuildSettings = new DotNetMSBuildSettings()
        .SetConfiguration(configuration)
        .WithProperty("Version", assemblyVersion)
        .WithProperty("FileVersion", fileVersion)
        .WithProperty("InformationalVersion", infoVersion)
        .WithProperty("PackageOutputPath", MakeAbsolute((DirectoryPath)"./output/").FullPath)
        .WithTarget("Pack");
    var settings = new DotNetBuildSettings
    {
        MSBuildSettings = msbuildSettings
    };

    if (prerelease) {
        msbuildSettings.WithProperty("PackageVersion", previewVersion);

        DotNetBuild("Mono.ApiTools.NuGetDiff/Mono.ApiTools.NuGetDiff.csproj", settings);
        DotNetBuild("api-tools/api-tools.csproj", settings);
    } else {
        msbuildSettings.WithProperty("PackageVersion", packageVersion);

        DotNetBuild("Mono.ApiTools.NuGetDiff/Mono.ApiTools.NuGetDiff.csproj", settings);
        DotNetBuild("api-tools/api-tools.csproj", settings);
    }
});

Task("Test")
    .IsDependentOn("Build")
    .Does(() =>
{
    Information("Running unit tests...");
    DotNetCoreTest("Mono.ApiTools.NuGetDiff.Tests/Mono.ApiTools.NuGetDiff.Tests.csproj", new DotNetCoreTestSettings {
        Loggers = new [] { "trx" }
    });

    Information("Running app tests...");
    var app = $"api-tools/bin/{configuration}/netcoreapp3.1/api-tools.dll";
    var id = "Mono.ApiTools.NuGetDiff";
    var version = prerelease ? previewVersion : packageVersion;
    DotNetCoreExecute(app, $"nuget-diff ./output/{id}.{version}.nupkg --latest --cache=externals --output=diff");
});

Task("Default")
    .IsDependentOn("Build")
    .IsDependentOn("Pack")
    .IsDependentOn("Test");

RunTarget(target);
