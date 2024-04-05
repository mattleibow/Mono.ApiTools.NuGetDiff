# `api-tools` .NET Tool

The `api-tools` .NET CLI tool is a useful way to do tasks quickly when trying 
to analyze and compar NuGet packages and/or .NET assemblies.

```
usage: api-tools COMMAND [OPTIONS]

A set of tools to help with .NET API development and and NuGet diff-ing.

Global options:
  -v, --verbose              Use a more verbose output

Available commands:
        api-info             Generate API info XML for assemblies.
        compat               Determine how compatible assemblies are.
        diff                 Compare two assemblies.
        merge                Merge multiple .NET assemblies.
        nuget-diff           Compare two NuGet packages.
```

## Commands

 * [api-info](#api-info-command) - Generate API info XML for assemblies.
 * [compat](#compat-command) - Determine how compatible assemblies are.
 * [diff](#diff-command) - Compare two assemblies.
 * [merge](#merge-command) - Merge multiple .NET assemblies.
 * [nuget-diff](#nuget-diff-command) - Compare two NuGet packages.


# `api-info` Command

```
usage: api-tools api-info ASSEMBLY ... [OPTIONS]

Generate API info XML for assemblies.

Options:
  -o, --output=VALUE         The output file path
  -?, -h, --help             Show this message and exit
```

# `compat` Command

The `compat` command is very useful as it allows the verification of API usage
in one binary agains another binary. An example would be to verify that a
particular update to a shared dependency is ABI compatible with another
dependency.

```
usage: api-tools compat ASSEMBLY1 ASSEMBLY2 [OPTIONS]

Determine how compatible assemblies are.

Options:
  -s, --search=VALUE         A search path directory for the main assembly
      --dependency-search=VALUE
                             A search path directory for the dependency
  -o, --output=VALUE         The output file path
  -?, -h, --help             Show this message and exit
```

## Example

Assumptions:
 - You are developing a project called `Resizetizer`
 - You depend on `SkiaSharp` for drawing/generating images
 - You depend on `Svg.Skia` for reading and rasterizing SVG files
 - The `Svg.Skia` dependency also depends on `SkiaSharp`

Because there is a shared dependency (`SkiaSharp`), there may be breaking
changes in a particular update. For example, the `SkiaSharp` 3.x series has a
few breaking changes when compared to 2.x.

If you were to just update `SkiaSharp` and build, your code in `Resizetizer`
may run fine because the testing code paths may not hit a particular missing
method or use a particular type. The only way to test would be to run a large
set of unit tests that would cover all of the potential code paths.

This `compat` command can help by inspecting the binary and detecting any
types and/or members that are not available in an update.

In our example, if we were to run this command against the `SkiaSharp` 3.x assembly:

```sh
dotnet api-tools compat Svg.Skia/Svg.Skia.dll SkiaSharp/v3/SkiaSharp.dll
```

We would get an output similar to:

```xml
<?xml version="1.0" encoding="utf-8"?>
<assemblies>
  <assembly>
    <types>
      <type fullname="SkiaSharp.SKImageFilter/CropRect" />
      <type fullname="SkiaSharp.SKCropRectFlags" />
    </types>
    <members>
      <member fullname="System.Void SkiaSharp.SKImageFilter/CropRect::.ctor(SkiaSharp.SKRect,SkiaSharp.SKCropRectFlags)" />
      <member fullname="SkiaSharp.SKImageFilter SkiaSharp.SKImageFilter::CreateMerge(SkiaSharp.SKImageFilter[],SkiaSharp.SKImageFilter/CropRect)" />
      <member fullname="SkiaSharp.SKImageFilter SkiaSharp.SKImageFilter::CreatePaint(SkiaSharp.SKPaint,SkiaSharp.SKImageFilter/CropRect)" />
      <!-- several other items -->
      <member fullname="System.Void SkiaSharp.SKPath::Transform(SkiaSharp.SKMatrix)" />
      <member fullname="System.Void SkiaSharp.SKCanvas::SetMatrix(SkiaSharp.SKMatrix)" />
    </members>
  </assembly>
</assemblies>
```

This output indicates that there are 2 missing types and quite a few missing
methods. If we were to load a simple SVG with just a few things, maybe our
code would run fine. However, if we perhaps decided to use some matrix
transformations in our SVG, the code would crash with missing members.

However, we can also use this information to maybe see if there is an
alternative API that can be used that is in both, or maybe we can write some
defensive code around these locations to make sure there are other checks
that prevent the actual execution of this code.

# `diff` Command

```
usage: api-tools diff ASSEMBLY1 ASSEMBLY2 [OPTIONS]

Compare two assemblies.

Options:
  -o, --output=VALUE         The output file path
      --ignore-nonbreaking   Ignore the non-breaking API changes
      --ignore-param-names   Ignore the changes to parameter names
      --ignore-virtual       Ignore the changes to virtual modifiers
  -?, -h, --help             Show this message and exit
```

# `merge` Command

```
usage: api-tools merge ASSEMBLY | DIRECTORY [OPTIONS]

Merge multiple .NET assemblies.

Options:
  -o, --output=VALUE         The output path to use for the merged assembly
  -s, --search=VALUE         One or more search directories
      --inject-assembly-name Add the assembly names to the types
      --attribute-type=VALUE The full name of the attribute
  -n, --assembly-name=VALUE  The name of the merged assembly
      --inject-assemblyname  [Obsolete] Use `--inject-assembly-name`
  -?, -h, --help             Show this message and exit
```

# `nuget-diff` Command

```
usage: api-tools nuget-diff [PACKAGES | DIRECTORIES] [OPTIONS]

Compare two NuGet packages.

Options:
      --cache=VALUE          The package cache directory
      --group-ids            Group the output by package ID
      --group-versions       Group the output by version
      --latest               Compare against the latest
      --output=VALUE         The output directory
      --prerelease           Include preprelease packages
      --ignore-unchanged     Ignore unchanged packages and assemblies
      --search-path=VALUE    A search path directory
  -s, --search=VALUE         A search path directory
      --source=VALUE         The NuGet URL source
      --version=VALUE        The version of the package to compare
      --compare-nuget-structure
                             Compare NuGet metadata and file contents
  -?, -h, --help             Show this message and exit
```
