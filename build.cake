#tool "nuget:?package=xunit.runner.console&version=2.4.0"

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

void DownloadMonoSources(DirectoryPath dest, params string[] urls)
{
    var rootUrl = $"https://github.com/mattleibow/mono/raw/mattleibow/make-library";

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

Task("Default")
    .Does(() =>
{
    Information("Downloading sources...");
    DownloadMonoSources("externals/mono-api-info", "mcs/tools/corcompare/mono-api-info.exe.sources");
    DownloadMonoSources("externals/mono-api-diff", "mcs/tools/mono-api-diff/mono-api-diff.exe.sources");
    DownloadMonoSources("externals/mono-api-html", "mcs/tools/mono-api-html/mono-api-html.exe.sources");

    Information("Building solution...");
    MSBuild("Mono.ApiTools.NuGetComparer.sln", cfg => cfg
        .SetVerbosity(Verbosity.Normal)
        .WithRestore()
        .WithProperty("Configuration", new [] { configuration }));

    Information("Running tests...");
    DotNetCoreTool("Mono.ApiTools.NuGetComparer.Tests/Mono.ApiTools.NuGetComparer.Tests.csproj", "xunit", "-verbose");
});

RunTarget(target);
