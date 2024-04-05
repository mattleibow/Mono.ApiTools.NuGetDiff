# NuGet Diff


NuGetDiff is easy to use if you just want to see what assemblies have been
added, removed or moved in a NuGet package:

```csharp
// create the comparer
NuGetDiff comparer = new NuGetDiff();

// set any properties, in this case ignore errors as this is not essential
comparer.IgnoreResolutionErrors = true;

// generate the object with information on what has changed
NuGetDiffResult diff = await comparer.GenerateAsync("Xamarin.Forms", "3.0.0.446417", "3.1.0.697729");
```

To actually generate a collection of markdown files with all the assembly
diffs, a similar path can be taken:

```csharp
// because comparing a NuGet may involve multiple assemblies, we cannot do
// this in memory, so we output all of this to a directory structure
string diffDir = "diff-out";

// create the comparer
NuGetDiff comparer = new NuGetDiff();

// set any properties
// here, we ignore errors as this is not essential
comparer.IgnoreResolutionErrors = true;
// here, we request that we want to save the assembly diff as a markdown file
comparer.SaveAssemblyMarkdownDiff = true;

// generate the object with information on what has changed
await comparer.SaveCompleteDiffToDirectoryAsync("Xamarin.Forms", "3.0.0.446417", "3.1.0.697729", diffDir);
```
