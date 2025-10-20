using System.Collections.Generic;
using System.IO;
using Mono.Options;

namespace Mono.ApiTools;

public abstract class NuGetBaseCommand : BaseCommand
{
	protected NuGetBaseCommand(string name, string help)
		: this(name, null, help)
	{
	}

	protected NuGetBaseCommand(string name, string extras, string help)
		: base(name, extras, help)
	{
	}

	public string PackageCache { get; set; }

	public bool PrePrelease { get; set; }

	public bool PreferRelease { get; set; }

	public string SourceUrl { get; set; } = "https://api.nuget.org/v3/index.json";

	protected override OptionSet OnCreateOptions() => new OptionSet
	{
		{ "cache=", "The package cache directory", v => PackageCache = v },
		{ "prerelease", "Include preprelease packages", v => PrePrelease = true },
		{ "prefer-release", "Prefer release packages over prerelease packages", v => PreferRelease = true },
		{ "source=", "The NuGet URL source", v => SourceUrl = v },
	};

	protected override bool OnValidateArguments(IEnumerable<string> extras)
	{
		var hasError = false;

		if (string.IsNullOrEmpty(PackageCache))
			PackageCache = Path.Combine(Directory.GetCurrentDirectory(), "packages");

		return !hasError;
	}
}
