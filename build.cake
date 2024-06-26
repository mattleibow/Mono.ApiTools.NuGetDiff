var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var pullrequest = Argument("pullrequest", 0);
var prerelease = Argument("prerelease", true) || pullrequest > 0;

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
var previewlabel = pullrequest > 0 ? $"pr.{pullrequest}" : "preview";
var previewVersion  = $"{packageVersion}-{previewlabel}.{previewNumber}";
var finalBuildNumber = prerelease ? previewVersion : packageVersion;

Information("Build configuration: {0}", configuration);
Information("Input version number: {0}", version);
Information("Package version number: {0}", finalBuildNumber);
Information($"##vso[build.updatebuildnumber]{finalBuildNumber}");

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

    DotNetBuild("Mono.ApiTools.sln", settings);
});

Task("Pack")
    .Does(() =>
{
    var msbuildSettings = new DotNetMSBuildSettings()
        .SetConfiguration(configuration)
        .WithProperty("Version", assemblyVersion)
        .WithProperty("FileVersion", fileVersion)
        .WithProperty("InformationalVersion", infoVersion)
        .WithProperty("PackageOutputPath", MakeAbsolute((DirectoryPath)"./output/").FullPath)
        .WithProperty("PackageVersion", prerelease ? previewVersion : packageVersion)
        .WithTarget("Pack");
    var settings = new DotNetBuildSettings
    {
        MSBuildSettings = msbuildSettings
    };

    DotNetBuild("Mono.ApiTools.slnf", settings);
});

Task("Test")
    .IsDependentOn("Build")
    .IsDependentOn("Pack")
    .Does(() =>
{
    Information("Running unit tests...");
    var msbuildSettings = new DotNetMSBuildSettings()
        .SetConfiguration(configuration)
        .WithProperty("Version", assemblyVersion)
        .WithProperty("FileVersion", fileVersion)
        .WithProperty("InformationalVersion", infoVersion);
    DotNetTest("Mono.ApiTools.sln", new DotNetTestSettings
    {
        MSBuildSettings = msbuildSettings,
        Loggers = new [] { "trx" }
    });

    Information("Running app tests...");
    var app = $"api-tools/bin/{configuration}/net8.0/api-tools.dll";
    var id = "Mono.ApiTools.NuGetDiff";
    var version = prerelease ? previewVersion : packageVersion;
    DotNetExecute(app, $"nuget-diff ./output/{id}.{version}.nupkg --latest --cache=externals --output=diff");
});

Task("Default")
    .IsDependentOn("Pack")
    .IsDependentOn("Build")
    .IsDependentOn("Test");

RunTarget(target);
