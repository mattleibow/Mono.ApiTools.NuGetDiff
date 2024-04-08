# NuGetDiff

[![Build Status](https://dev.azure.com/mattleibow/OpenSource/_apis/build/status/Mono.ApiTools.NuGetDiff?branchName=master)](https://dev.azure.com/mattleibow/OpenSource/_build/latest?definitionId=20&branchName=master) [![NuGet Pre Release](https://img.shields.io/nuget/vpre/Mono.ApiTools.NuGetDiff.svg)](https://www.nuget.org/packages/Mono.ApiTools.NuGetDiff)

A library and .NET tool to help with .NET API development. There are features to compare both 
assemblies and NuGet packages. There is also a few assembly analyzers to help figure out 
changes and/or breaks in ABI.

## Building

This project is very simple and can be built, packed and tested using
the `dotnet` CLI. But, to do everything in a single step, there
is the .NET Core Cake tool ([`Cake.Tool`](https://www.nuget.org/packages/Cake.Tool)):

```
dotnet cake
```

## Docs

 * [`api-tools` .NET Tool](docs/api-tools.md)
 * [API Info](docs/ApiInfo.md)
 * [API Diff](docs/ApiDiff.md)
 * [API Diff (Formatted)](docs/ApiDiffFormatted.md)
 * [API NuGet Diff](docs/NuGetDiff.md)

## TODO

The library is working very well, but obviously - as with all software -
improvements can be made:

 * Add XML docs for Intellisense
 * Expose more configuration points for generating API info or diffs
 * More things...
