using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Mono.Options;
using NuGet.Packaging;
using NuGet.Versioning;

namespace Mono.ApiTools
{
	public class NuGetDiffCommand : BaseCommand
	{
		public NuGetDiffCommand()
			: base("nuget-diff", "[PACKAGES | DIRECTORIES]", "Compare two NuGet packages.")
		{
		}

		public List<string> Packages { get; set; } = new List<string>();

		public string PackageCache { get; set; }

		public List<string> SearchPaths { get; set; } = new List<string>();

		public bool GroupByPackageId { get; set; }

		public bool GroupByVersion { get; set; }

		public string Version { get; set; }

		public bool Latest { get; set; }

		public bool PrePrelease { get; set; }

		public bool PreferRelease { get; set; }

		public bool IgnoreUnchanged { get; set; }

		public string OutputDirectory { get; set; }

		public string SourceUrl { get; set; } = "https://api.nuget.org/v3/index.json";

		public bool CompareNuGetStructure { get; set; }

		protected override OptionSet OnCreateOptions() => new OptionSet
		{
			{ "cache=", "The package cache directory", v => PackageCache = v },
			{ "group-ids", "Group the output by package ID", v => GroupByPackageId = true },
			{ "group-versions", "Group the output by version", v => GroupByVersion = true },
			{ "latest", "Compare against the latest", v => Latest = true },
			{ "output=", "The output directory", v => OutputDirectory = v },
			{ "prerelease", "Include preprelease packages", v => PrePrelease = true },
			{ "prefer-release", "Prefer release packages over prerelease packages", v => PreferRelease = true },
			{ "ignore-unchanged", "Ignore unchanged packages and assemblies", v => IgnoreUnchanged = true },
			{ "search-path=", "A search path directory", v => SearchPaths.Add(v) },
			{ "s|search=", "A search path directory", v => SearchPaths.Add(v) },
			{ "source=", "The NuGet URL source", v => SourceUrl = v },
			{ "version=", "The version of the package to compare", v => Version = v },
			{ "compare-nuget-structure", "Compare NuGet metadata and file contents", v => CompareNuGetStructure = true },
			{ "include-structure", "Compare NuGet metadata and file contents", v => CompareNuGetStructure = true },
		};

		protected override bool OnValidateArguments(IEnumerable<string> extras)
		{
			var hasError = false;

			var packages = extras.Where(p => !string.IsNullOrEmpty(p)).ToArray();

			foreach (var pkg in packages)
			{
				if (Directory.Exists(pkg))
				{
					Packages.AddRange(Directory.EnumerateFiles(pkg, "*.nupkg"));
				}
				else if (File.Exists(pkg))
				{
					Packages.Add(pkg);
				}
				else
				{
					Console.Error.WriteLine($"{Program.Name}: Package does not exist: `{pkg}`.");
					hasError = true;
				}
			}

			if (Packages.Count == 0)
			{
				Console.Error.WriteLine($"{Program.Name}: At least one package is required.");
				hasError = true;
			}

			if (!string.IsNullOrEmpty(Version) && Latest)
			{
				Console.Error.WriteLine($"{Program.Name}: Both `--latest` and `--version=<VERSION>` cannot be provided at the same time.");
				hasError = true;
			}

			if (!string.IsNullOrEmpty(Version) && !NuGetVersion.TryParse(Version, out _))
			{
				Console.Error.WriteLine($"{Program.Name}: An invalid version was provided.");
				hasError = true;
			}

			if (string.IsNullOrEmpty(Version) && !Latest && Packages.Count != 2)
			{
				Console.Error.WriteLine($"{Program.Name}: If `--latest` or `--version=<VERSION>` is not specified, then exactly two packages are required.");
				hasError = true;
			}

			if (string.IsNullOrEmpty(OutputDirectory))
				OutputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "api-diff");

			if (string.IsNullOrEmpty(PackageCache))
				PackageCache = Path.Combine(Directory.GetCurrentDirectory(), "packages");

			return !hasError;
		}

		protected override bool OnInvoke(IEnumerable<string> extras)
		{
			// create comparer
			var comparer = new NuGetDiff(SourceUrl);
			comparer.SearchPaths.AddRange(SearchPaths);
			comparer.PackageCache = PackageCache;

			if (string.IsNullOrEmpty(Version) && !Latest)
			{
				using (var older = new PackageArchiveReader(Packages[0]))
				using (var reader = new PackageArchiveReader(Packages[1]))
				{
					DiffPackage(comparer, reader, older).Wait();
				}
			}
			else
			{
				foreach (var nupkg in Packages)
				{
					using (var older = new PackageArchiveReader(nupkg))
					using (var latest = GetOtherPackage(comparer, older).Result)
					{
						DiffPackage(comparer, older, latest).Wait();
					}
				}
			}

			return true;
		}

		private async Task<PackageArchiveReader> GetOtherPackage(NuGetDiff comparer, PackageArchiveReader reader)
		{
			var identity = reader.GetIdentity();
			var packageId = identity.Id;

			string latest;
			if (Latest)
			{
				// get the latest version of this package - if any
				if (Program.Verbose)
					Console.WriteLine($"Determining the latest version of '{packageId}'...");
				var filter = new NuGetVersions.Filter
				{
					IncludePrerelease = PrePrelease,
					PreferRelease = PreferRelease,
					SourceUrl = SourceUrl,
				};
				latest = (await NuGetVersions.GetLatestAsync(packageId, filter))?.ToNormalizedString();
			}
			else
			{
				latest = Version;
			}


			if (string.IsNullOrEmpty(latest))
			{
				if (Program.Verbose)
					Console.WriteLine($"No package found for '{packageId}'...");
				return null;
			}

			if (Program.Verbose)
				Console.WriteLine($"Downloading version '{latest}' of '{packageId}'...");
			return await comparer.OpenPackageAsync(packageId, latest);
		}

		private async Task DiffPackage(NuGetDiff comparer, PackageArchiveReader reader, PackageArchiveReader olderReader)
		{
			// get the id from the package and the version number
			var identity = reader.GetIdentity();
			var packageId = identity.Id;
			var currentVersionNo = identity.Version.ToNormalizedString();
			var olderIdentity = olderReader?.GetIdentity();
			var olderVersion = olderIdentity?.Version?.ToNormalizedString();

			// calculate the diff storage path from the location of the nuget
			var diffRoot = OutputDirectory;
			if (GroupByPackageId)
				diffRoot = Path.Combine(diffRoot, packageId);
			if (GroupByVersion)
				diffRoot = Path.Combine(diffRoot, currentVersionNo);

			// log what is going to happen
			if (string.IsNullOrEmpty(olderVersion))
				Console.WriteLine($"Running a diff on a new package '{packageId}'...");
			else
				Console.WriteLine($"Running a diff on '{currentVersionNo}' vs '{olderVersion}' of '{packageId}'...");

			// run the diff with all changes
			comparer.SaveNuGetXmlDiff = false;                      // this is not needed for this type of diff
			comparer.SaveAssemblyApiInfo = !IgnoreUnchanged;        // this lets us know if there were no changes
			comparer.SaveAssemblyMarkdownDiff = true;               // we want markdown
			comparer.IgnoreResolutionErrors = true;                 // we don't care if frameowrk/platform types can't be found
			comparer.MarkdownDiffFileExtension = ".diff.md";
			comparer.IgnoreNonBreakingChanges = false;
			comparer.SaveNuGetStructureDiff = CompareNuGetStructure;

			await comparer.SaveCompleteDiffToDirectoryAsync(olderReader, reader, diffRoot);

			// run the diff with just the breaking changes
			comparer.MarkdownDiffFileExtension = ".breaking.md";
			comparer.IgnoreNonBreakingChanges = true;
			await comparer.SaveCompleteDiffToDirectoryAsync(olderReader, reader, diffRoot);

			if (Directory.Exists(diffRoot))
			{
				// TODO: there are two bugs in this version of mono-api-html
				var mdFiles = Directory.EnumerateFiles(diffRoot, "*.md", SearchOption.AllDirectories);
				foreach (var md in mdFiles)
				{
					var contents = await File.ReadAllTextAsync(md);

					// 1. the <h4> doesn't look pretty in the markdown
					contents = contents.Replace("<h4>", "> ");
					contents = contents.Replace("</h4>", Environment.NewLine);

					// 2. newlines are inccorrect on Windows: https://github.com/mono/mono/pull/9918
					contents = contents.Replace("\r\r", "\r");

					await File.WriteAllTextAsync(md, contents);
				}

				// clean up the changes
				var xmlFiles = Directory.EnumerateFiles(diffRoot, "*.xml", SearchOption.AllDirectories);
				foreach (var file in xmlFiles)
				{
					// make sure to create markdown files for unchanged assemblies
					if (file.EndsWith(".new.info.xml", StringComparison.OrdinalIgnoreCase))
					{
						var dll = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(file)));
						var md = $"{Path.GetDirectoryName(file)}/{dll}.diff.md";
						if (!File.Exists(md))
						{
							var n = Environment.NewLine;
							var noChangesText = $"# API diff: {dll}{n}{n}## {dll}{n}{n}> No changes.{n}";
							await File.WriteAllTextAsync(md, noChangesText);
						}
					}

					// delete the info files now
					File.Delete(file);
				}
			}

			// we are done
			Console.WriteLine($"Diff complete of '{packageId}'.");
		}
	}
}
