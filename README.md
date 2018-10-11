# NuGetDiff

[![Build status](https://ci.appveyor.com/api/projects/status/y8yct1q94vxaw3i6/branch/master?svg=true)](https://ci.appveyor.com/project/mattleibow/nugetcomparer/branch/master) [![NuGet Pre Release](https://img.shields.io/nuget/vpre/Mono.ApiTools.NuGetDiff.svg)](https://www.nuget.org/packages/Mono.ApiTools.NuGetDiff)

A library to help with .NET API development and and NuGet diff-ing.

This library is really just a collection of other libraries to make a single
point to diff a NuGet package:

 * [NuGet][nuget] to download and process the NuGet packages
 * [Mono.ApiTools][api-tools] to process the assemblies and generate the 
   API information and the XML, HTML & Markdown diffs.

## Building

This project is very simple and can be built, packed and tested using
`msbuild` and `dotnet`. But, to do everything in a single step, there is
a cake script:

```
.\build.ps1
```

## Using

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

## TODO

The library is working very well, but obviously - as with all software -
improvements can be made:

 * Add XML docs for Intellisense
 * Expose more configuration points for generating API info or diffs
 * More...

[nuget]: https://github.com/NuGet/NuGet.Client
[api-tools]: https://www.nuget.org/packages/Mono.ApiTools
