using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mono.Options;
using NuGet.Versioning;

namespace Mono.ApiTools;

public class NuGetDownloadCommand : NuGetBaseCommand
{
	public NuGetDownloadCommand()
		: base("download", "PACKAGE ID", "Download a NuGet package.")
	{
	}

	public string PackageId { get; set; }

	public string Version { get; set; }

	public bool Latest { get; set; }

	protected override OptionSet OnCreateOptions()
	{
		var options = base.OnCreateOptions();
		options.Add("latest", "Compare against the latest", v => Latest = true);
		options.Add("version=", "The version of the package to compare", v => Version = v);
		return options;
	}

	protected override bool OnValidateArguments(IEnumerable<string> extras)
	{
		var hasError = !base.OnValidateArguments(extras);

		var packages = extras.Where(p => !string.IsNullOrEmpty(p)).ToArray();

		if (packages.Length != 1)
		{
			Console.Error.WriteLine($"{Program.Name}: Exactly one package ID is required.");
			hasError = true;
		}
		else
		{
			PackageId = packages[0];
		}

		if (!string.IsNullOrEmpty(Version) && Latest)
		{
			Console.Error.WriteLine($"{Program.Name}: Both `--latest` and `--version=<VERSION>` cannot be provided at the same time.");
			hasError = true;
		}

		if (string.IsNullOrEmpty(Version) && !Latest)
		{
			Console.Error.WriteLine($"{Program.Name}: Either `--latest` or `--version=<VERSION>` must be specified.");
			hasError = true;
		}

		if (!string.IsNullOrEmpty(Version) && !NuGetVersion.TryParse(Version, out _))
		{
			Console.Error.WriteLine($"{Program.Name}: An invalid version was provided.");
			hasError = true;
		}

		return !hasError;
	}

	protected override bool OnInvoke(IEnumerable<string> extras)
	{
		var manager = new NuGetManager(SourceUrl);
		manager.PackageCache = PackageCache;

		var success = DownloadPackageAsync(manager).Result;

		return success;
	}

	private async Task<bool> DownloadPackageAsync(NuGetManager manager)
	{
		string latest;
		if (Latest)
		{
			// get the latest version of this package - if any
			if (Program.Verbose)
				Console.WriteLine($"Determining the latest version of '{PackageId}'...");
			var filter = new NuGetVersions.Filter
			{
				IncludePrerelease = PrePrelease,
				PreferRelease = PreferRelease,
				SourceUrl = SourceUrl,
			};
			latest = (await NuGetVersions.GetLatestAsync(PackageId, filter))?.ToNormalizedString();
		}
		else
		{
			latest = Version;
		}


		if (string.IsNullOrEmpty(latest))
		{
			if (Program.Verbose)
				Console.WriteLine($"No package found for '{PackageId}'...");
			return false;
		}

		if (Program.Verbose)
			Console.WriteLine($"Downloading version '{latest}' of '{PackageId}'...");

		var dest = await manager.ExtractCachedPackageAsync(PackageId, latest);
		
		if (Program.Verbose)
			Console.WriteLine($"Package downloaded to '{dest}'.");

		return true;
	}
}
