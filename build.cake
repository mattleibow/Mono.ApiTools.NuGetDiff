#tool "nuget:?package=xunit.runner.console&version=2.4.0"

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

// a bit of logic to create the version number:
//  - input                     = 1.2.3.4
//  - package version           = 1.2.3
//  - preview package version   = 1.2.3-preview4
var version = Version.Parse(Argument("packageversion", EnvironmentVariable("APPVEYOR_BUILD_VERSION") ?? "1.0.0.0"));
var previewNumber   = version.Revision;
var assemblyVersion = $"{version.Major}.0.0.0";
var fileVersion     = $"{version.Major}.{version.Minor}.{version.Build}.0";
var infoVersion     = $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
var packageVersion  = $"{version.Major}.{version.Minor}.{version.Build}";

void DownloadMonoSources(DirectoryPath dest, params string[] urls)
{
    var rootUrl = $"https://github.com/mono/mono/raw/c32af8905b5d672f58acad6fc9e08bf61375b850";

    EnsureDirectoryExists(dest);
    foreach (var originalUrl in urls) {
        // make sure the urls are rooted
        var url = originalUrl;
        if (!url.StartsWith("http:") && !url.StartsWith("https:")) {
            url = $"{rootUrl}/{url}";
        }
        // get the path parts
        var file = url.Substring(url.LastIndexOf("/") + 1);
        var dir = url.Substring(0, url.LastIndexOf("/"));
        var destFile = dest.CombineWithFilePath(file);
        // download the file
        if (!FileExists(destFile)) {
            Information($"Downloading '{url}' to '{destFile}'...");
            DownloadFile(url, destFile);
        }
        // if this is a .sources file, download all the listed files too
        if (file.EndsWith(".sources")) {
            var listedFiles = System.IO.File.ReadAllLines(destFile.FullPath)
                .Where(f => !f.StartsWith(".."))
                .Select(f => $"{dir}/{f}")
                .ToArray();
            DownloadMonoSources(dest, listedFiles);
        }
    }
}

Task("Clean")
    .Does(() =>
{
    CleanDirectories("externals");
});

Task("Build")
    .Does(() =>
{
    DownloadMonoSources("externals/mono-api-info", "mcs/tools/corcompare/mono-api-info.exe.sources");
    DownloadMonoSources("externals/mono-api-diff", "mcs/tools/mono-api-diff/mono-api-diff.exe.sources");
    DownloadMonoSources("externals/mono-api-html", "mcs/tools/mono-api-html/mono-api-html.exe.sources");

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
    DotNetCoreTool("Mono.ApiTools.NuGetDiff.Tests/Mono.ApiTools.NuGetDiff.Tests.csproj", "xunit", "-verbose");
});

Task("Default")
    .IsDependentOn("Build")
    .IsDependentOn("Pack")
    .IsDependentOn("Test");

RunTarget(target);
