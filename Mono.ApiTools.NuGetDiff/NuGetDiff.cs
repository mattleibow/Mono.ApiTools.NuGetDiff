using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using NuGet.Common;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace Mono.ApiTools
{
	public class NuGetDiff
	{
		internal const string NuGetSourceUrl = "https://api.nuget.org/v3/index.json";

		private const int DefaultSaveBufferSize = 1024;
		private const int DefaultCopyBufferSize = 81920;
		private static readonly Encoding UTF8NoBOM = new UTF8Encoding(false, true);

		private const string DefaultXmlDiffFileExtension = ".diff.xml";
		private const string DefaultHtmlDiffFileExtension = ".diff.html";
		private const string DefaultMarkdownDiffFileExtension = ".diff.md";
		private const string DefaultApiInfoFileExtension = ".info.xml";
		private readonly SourceRepository source;
		private readonly SourceCacheContext cache;
		private readonly ILogger logger;

		public NuGetDiff()
			: this(NuGetSourceUrl)
		{
		}

		public NuGetDiff(string sourceUrl)
		{
			source = Repository.Factory.GetCoreV3(sourceUrl);
			cache = new SourceCacheContext();
			logger = NullLogger.Instance;
		}


		// Properties

		public List<string> SearchPaths { get; set; } = new List<string>();

		public string PackageCache { get; set; } = "packages";

		public bool IgnoreResolutionErrors { get; set; } = false;

		public bool IgnoreInheritedInterfaces { get; set; } = false;

		public bool IgnoreAddedAssemblies { get; set; } = false;

		public bool IgnoreSimilarFrameworks { get; set; } = false;

		public bool IgnoreNonBreakingChanges { get; set; } = false;

		public bool SaveAssemblyApiInfo { get; set; } = false;

		public bool SaveAssemblyHtmlDiff { get; set; } = false;

		public bool SaveAssemblyMarkdownDiff { get; set; } = false;

		public bool SaveAssemblyXmlDiff { get; set; } = false;

		public bool SaveNuGetXmlDiff { get; set; } = true;

		public string ApiInfoFileExtension { get; set; } = DefaultApiInfoFileExtension;

		public string HtmlDiffFileExtension { get; set; } = DefaultHtmlDiffFileExtension;

		public string MarkdownDiffFileExtension { get; set; } = DefaultMarkdownDiffFileExtension;

		public string XmlDiffFileExtension { get; set; } = DefaultXmlDiffFileExtension;


		// GenerateAsync

		public Task<NuGetDiffResult> GenerateAsync(string id, string oldVersion, string newVersion, CancellationToken cancellationToken = default)
		{
			var oldId = string.IsNullOrEmpty(oldVersion) ? null : new PackageIdentity(id, NuGetVersion.Parse(oldVersion));
			var newId = new PackageIdentity(id, NuGetVersion.Parse(newVersion));
			return GenerateAsync(oldId, newId, cancellationToken);
		}

		public Task<NuGetDiffResult> GenerateAsync(string id, NuGetVersion oldVersion, NuGetVersion newVersion, CancellationToken cancellationToken = default)
		{
			var oldId = oldVersion == null ? null : new PackageIdentity(id, oldVersion);
			var newId = new PackageIdentity(id, newVersion);
			return GenerateAsync(oldId, newId, cancellationToken);
		}

		public async Task<NuGetDiffResult> GenerateAsync(PackageIdentity oldPackage, PackageIdentity newPackage, CancellationToken cancellationToken = default)
		{
			using (var oldReader = oldPackage == null ? null : await OpenPackageAsync(oldPackage, cancellationToken).ConfigureAwait(false))
			using (var newReader = await OpenPackageAsync(newPackage, cancellationToken).ConfigureAwait(false))
			{
				return await GenerateAsync(oldReader, newReader, cancellationToken).ConfigureAwait(false);
			}
		}

		public Task<NuGetDiffResult> GenerateAsync(string oldId, string oldVersion, PackageArchiveReader newReader, CancellationToken cancellationToken = default)
		{
			var oldPackageId = string.IsNullOrEmpty(oldVersion) ? null : new PackageIdentity(oldId, NuGetVersion.Parse(oldVersion));
			return GenerateAsync(oldPackageId, newReader, cancellationToken);
		}

		public Task<NuGetDiffResult> GenerateAsync(string oldId, NuGetVersion oldVersion, PackageArchiveReader newReader, CancellationToken cancellationToken = default)
		{
			var oldPackageId = oldVersion == null ? null : new PackageIdentity(oldId, oldVersion);
			return GenerateAsync(oldPackageId, newReader, cancellationToken);
		}

		public async Task<NuGetDiffResult> GenerateAsync(PackageIdentity oldPackage, PackageArchiveReader newReader, CancellationToken cancellationToken = default)
		{
			using (var oldReader = oldPackage == null ? null : await OpenPackageAsync(oldPackage, cancellationToken).ConfigureAwait(false))
			{
				return await GenerateAsync(oldReader, newReader, cancellationToken).ConfigureAwait(false);
			}
		}

		public Task<NuGetDiffResult> GenerateAsync(string oldPath, string newpath, CancellationToken cancellationToken = default)
		{
			using (var oldReader = string.IsNullOrEmpty(oldPath) ? null : new PackageArchiveReader(oldPath))
			using (var newReader = new PackageArchiveReader(newpath))
			{
				return GenerateAsync(oldReader, newReader, cancellationToken);
			}
		}

		public async Task<NuGetDiffResult> GenerateAsync(PackageArchiveReader oldReader, PackageArchiveReader newReader, CancellationToken cancellationToken = default)
		{
			// get the identities
			var oldIdentity = oldReader == null ? null : await oldReader.GetIdentityAsync(cancellationToken).ConfigureAwait(false);
			var newIdentity = await newReader.GetIdentityAsync(cancellationToken).ConfigureAwait(false);

			// get the items
			var oldItems = oldReader == null ? Enumerable.Empty<FrameworkSpecificGroup>() : await oldReader.GetLibItemsAsync(cancellationToken).ConfigureAwait(false);
			var newItems = await newReader.GetLibItemsAsync(cancellationToken).ConfigureAwait(false);

			// create a new collection for the updates
			var oldFrameworks = oldItems.Select(i => i.TargetFramework).Distinct().ToArray();
			var newFrameworks = newItems.Select(i => i.TargetFramework).Distinct().ToArray();
			var mergedFrameworks = oldFrameworks.Union(newFrameworks);
			var mergedGroup = mergedFrameworks.Select(fw => new FrameworkGroup
			{
				Framework = fw,
				OldItems = GetFrameworkAssemblies(fw, oldItems),
				NewItems = GetFrameworkAssemblies(fw, newItems),
			}).ToArray();

			var result = new NuGetDiffResult
			{
				// versions
				OldIdentity = oldIdentity,
				NewIdentity = newIdentity,

				// frameworks
				AddedFrameworks = mergedGroup.Where(g => !g.OldItems.Any() && !oldFrameworks.Contains(g.Framework)).Select(g => g.Framework).ToArray(),
				RemovedFrameworks = mergedGroup.Where(g => !g.NewItems.Any() && !newFrameworks.Contains(g.Framework)).Select(g => g.Framework).ToArray(),
				UnchangedFrameworks = mergedGroup.Where(g => g.OldItems.Any() && g.NewItems.Any()).Select(g => g.Framework).ToArray(),
				SimilarFrameworks = new Dictionary<NuGetFramework, NuGetFramework>(),

				// assemblies
				AddedAssemblies = new Dictionary<NuGetFramework, string[]>(),
				RemovedAssemblies = new Dictionary<NuGetFramework, string[]>(),
				UnchangedAssemblies = new Dictionary<NuGetFramework, (string, string)[]>(),
				SimilarAssemblies = new Dictionary<NuGetFramework, (string, string)[]>()
			};

			// using the assembly "name" as a matcher, sort them into the groups
			foreach (var group in mergedGroup)
			{
				var added = new List<string>();
				var removed = new List<string>();
				var unchanged = new List<(string, string)>();

				foreach (var (path, name) in group.NewItems)
				{
					var match = group.OldItems.FirstOrDefault(i => i.name == name);
					if (match.path != null)
						unchanged.Add((path, match.path));
					else
						added.Add(path);
				}
				foreach (var (path, name) in group.OldItems)
				{
					if (!group.NewItems.Any(i => i.name == name))
						removed.Add(path);
				}

				if (added.Count > 0)
					result.AddedAssemblies.Add(group.Framework, added.ToArray());
				if (removed.Count > 0)
					result.RemovedAssemblies.Add(group.Framework, removed.ToArray());
				if (unchanged.Count > 0)
					result.UnchangedAssemblies.Add(group.Framework, unchanged.ToArray());
			}

			// add an extra layer of matching for any changes
			if (!IgnoreSimilarFrameworks)
			{
				foreach (var addedFw in result.AddedFrameworks)
				{
					// try match the frameworks
					//  - first the removed (may have been a rename or update)
					//  - next the existing (may have been a second version)
					var matchedFw =
						TryMatchFramework(addedFw, result.RemovedFrameworks) ??
						TryMatchFramework(addedFw, result.UnchangedFrameworks);

					if (matchedFw != null)
					{
						var addedGroup = mergedGroup.FirstOrDefault(g => g.Framework == addedFw);
						var matchGroup = mergedGroup.FirstOrDefault(g => g.Framework == matchedFw);

						var matches = new List<(string, string)>();
						foreach (var (path, name) in addedGroup.NewItems)
						{
							var match = matchGroup.OldItems.FirstOrDefault(i => i.name == name);
							if (match.path != null)
								matches.Add((path, match.path));
						}
						if (matches.Count > 0)
						{
							result.SimilarAssemblies.Add(addedFw, matches.ToArray());
							result.SimilarFrameworks.Add(addedFw, matchedFw);
						}
					}
				}
			}

			return result;

			NuGetFramework TryMatchFramework(NuGetFramework added, NuGetFramework[] choices)
			{
				// there may be a case where the spelling has changed
				var exact = choices.FirstOrDefault(fw => fw == added);
				if (exact != null)
					return exact;

				// match frameworks that have just changed version
				var name = choices.FirstOrDefault(fw => NuGetFramework.FrameworkNameComparer.Equals(fw, added));
				if (name != null)
					return name;

				// .NET Standard may have been an upgrade from PCL
				var netstd = new NuGetFramework(".NETStandard");
				var pcl = new NuGetFramework(".NETPortable");
				if (NuGetFramework.FrameworkNameComparer.Equals(added, netstd))
				{
					var pclnetstd = choices.FirstOrDefault(fw => NuGetFramework.FrameworkNameComparer.Equals(fw, pcl));
					if (pclnetstd != null)
						return pclnetstd;
				}
				// the horror that a .NET Standard was devolved into a PCL
				if (NuGetFramework.FrameworkNameComparer.Equals(added, pcl))
				{
					var pclnetstd = choices.FirstOrDefault(fw => NuGetFramework.FrameworkNameComparer.Equals(fw, netstd));
					if (pclnetstd != null)
						return pclnetstd;
				}

				return null;
			}

			(string path, string name)[] GetFrameworkAssemblies(NuGetFramework fw, IEnumerable<FrameworkSpecificGroup> items)
			{
				return items
					?.FirstOrDefault(i => i.TargetFramework == fw)
					?.Items
					?.Where(i => Path.GetExtension(i).ToLowerInvariant() == ".dll")
					?.Select(i => (i, GetOutputFilenameBase(i, false)))
					?.ToArray()
					?? Array.Empty<(string, string)>();
			}
		}


		// GenerateApiInfoAsync

		public async Task<Stream> GenerateAssemblyApiInfoAsync(PackageArchiveReader reader, string assemblyPath, CancellationToken cancellationToken = default)
		{
			var info = new MemoryStream();

			using (var buffer = await OpenAssemblyAsync(reader, assemblyPath, cancellationToken).ConfigureAwait(false))
			using (var writer = new StreamWriter(info, UTF8NoBOM, DefaultSaveBufferSize, true))
			{
				var config = CreateApiInfoConfig();

				// add the other assemblies in this framework
				var libs = await reader.GetLibItemsAsync(cancellationToken).ConfigureAwait(false);
				var fw = libs.FirstOrDefault(l => l.Items.Contains(assemblyPath));
				if (fw != null)
				{
					foreach (var lib in fw.Items.Where(l => Path.GetExtension(l).ToLowerInvariant() == ".dll"))
					{
						var s = await OpenAssemblyAsync(reader, lib, cancellationToken);
						config.ResolveStreams.Add(s);
					}
				}

				ApiInfo.Generate(buffer, writer, config);
			}

			info.Position = 0;
			return info;
		}


		// GenerateAssemblyXmlDiffAsync

		public async Task<Stream> GenerateAssemblyXmlDiffAsync(PackageArchiveReader oldReader, PackageArchiveReader newReader, string assemblyPath, CancellationToken cancellationToken = default)
		{
			using (var oldInfo = await GenerateAssemblyApiInfoAsync(oldReader, assemblyPath, cancellationToken).ConfigureAwait(false))
			using (var newInfo = await GenerateAssemblyApiInfoAsync(newReader, assemblyPath, cancellationToken).ConfigureAwait(false))
			{
				return await GenerateAssemblyXmlDiffAsync(oldInfo, newInfo, cancellationToken).ConfigureAwait(false);
			}
		}


		// GenerateAssemblyHtmlDiffAsync

		public async Task<Stream> GenerateAssemblyHtmlDiffAsync(PackageArchiveReader oldReader, PackageArchiveReader newReader, string assemblyPath, CancellationToken cancellationToken = default)
		{
			using (var oldInfo = await GenerateAssemblyApiInfoAsync(oldReader, assemblyPath, cancellationToken).ConfigureAwait(false))
			using (var newInfo = await GenerateAssemblyApiInfoAsync(newReader, assemblyPath, cancellationToken).ConfigureAwait(false))
			{
				return await GenerateAssemblyHtmlDiffAsync(oldInfo, newInfo, cancellationToken).ConfigureAwait(false);
			}
		}


		// GenerateAssemblyMarkdownDiffAsync

		public async Task<Stream> GenerateAssemblyMarkdownDiffAsync(PackageArchiveReader oldReader, PackageArchiveReader newReader, string assemblyPath, CancellationToken cancellationToken = default)
		{
			using (var oldInfo = await GenerateAssemblyApiInfoAsync(oldReader, assemblyPath, cancellationToken).ConfigureAwait(false))
			using (var newInfo = await GenerateAssemblyApiInfoAsync(newReader, assemblyPath, cancellationToken).ConfigureAwait(false))
			{
				return await GenerateAssemblyMarkdownDiffAsync(oldInfo, newInfo, cancellationToken).ConfigureAwait(false);
			}
		}


		// GenerateXmlDiffAsync

		public async Task<Stream> GenerateXmlDiffAsync(PackageArchiveReader oldReader, PackageArchiveReader newReader, CancellationToken cancellationToken = default)
		{
			var packageDiff = await GenerateAsync(oldReader, newReader, cancellationToken).ConfigureAwait(false);

			var xPackageDiff = CreatePackageDiff(packageDiff, cancellationToken);

			var stream = new MemoryStream();
			xPackageDiff.Save(stream);
			stream.Position = 0;

			return stream;
		}


		// SaveCompleteDiffToDirectoryAsync

		public Task SaveCompleteDiffToDirectoryAsync(string id, string oldVersion, string newVersion, string outputDirectory, CancellationToken cancellationToken = default)
		{
			var oldId = string.IsNullOrEmpty(oldVersion) ? null : new PackageIdentity(id, NuGetVersion.Parse(oldVersion));
			var newId = new PackageIdentity(id, NuGetVersion.Parse(newVersion));
			return SaveCompleteDiffToDirectoryAsync(oldId, newId, outputDirectory, cancellationToken);
		}

		public Task SaveCompleteDiffToDirectoryAsync(string id, NuGetVersion oldVersion, NuGetVersion newVersion, string outputDirectory, CancellationToken cancellationToken = default)
		{
			var oldId = oldVersion == null ? null : new PackageIdentity(id, oldVersion);
			var newId = new PackageIdentity(id, newVersion);
			return SaveCompleteDiffToDirectoryAsync(oldId, newId, outputDirectory, cancellationToken);
		}

		public async Task SaveCompleteDiffToDirectoryAsync(PackageIdentity oldPackage, PackageIdentity newPackage, string outputDirectory, CancellationToken cancellationToken = default)
		{
			using (var oldReader = oldPackage == null ? null : await OpenPackageAsync(oldPackage, cancellationToken).ConfigureAwait(false))
			using (var newReader = await OpenPackageAsync(newPackage, cancellationToken).ConfigureAwait(false))
			{
				await SaveCompleteDiffToDirectoryAsync(oldReader, newReader, outputDirectory, cancellationToken).ConfigureAwait(false);
			}
		}

		public Task SaveCompleteDiffToDirectoryAsync(string oldId, string oldVersion, PackageArchiveReader newReader, string outputDirectory, CancellationToken cancellationToken = default)
		{
			var oldPackageId = string.IsNullOrEmpty(oldVersion) ? null : new PackageIdentity(oldId, NuGetVersion.Parse(oldVersion));
			return SaveCompleteDiffToDirectoryAsync(oldPackageId, newReader, outputDirectory, cancellationToken);
		}

		public Task SaveCompleteDiffToDirectoryAsync(string oldId, NuGetVersion oldVersion, PackageArchiveReader newReader, string outputDirectory, CancellationToken cancellationToken = default)
		{
			var oldPackageId = oldVersion == null ? null : new PackageIdentity(oldId, oldVersion);
			return SaveCompleteDiffToDirectoryAsync(oldPackageId, newReader, outputDirectory, cancellationToken);
		}

		public async Task SaveCompleteDiffToDirectoryAsync(PackageIdentity oldPackage, PackageArchiveReader newReader, string outputDirectory, CancellationToken cancellationToken = default)
		{
			using (var oldReader = oldPackage == null ? null : await OpenPackageAsync(oldPackage, cancellationToken).ConfigureAwait(false))
			{
				await SaveCompleteDiffToDirectoryAsync(oldReader, newReader, outputDirectory, cancellationToken).ConfigureAwait(false);
			}
		}

		public Task SaveCompleteDiffToDirectoryAsync(string oldPath, string newpath, string outputDirectory, CancellationToken cancellationToken = default)
		{
			using (var oldReader = string.IsNullOrEmpty(oldPath) ? null : new PackageArchiveReader(oldPath))
			using (var newReader = new PackageArchiveReader(newpath))
			{
				return SaveCompleteDiffToDirectoryAsync(oldReader, newReader, outputDirectory, cancellationToken);
			}
		}

		public async Task SaveCompleteDiffToDirectoryAsync(PackageArchiveReader oldReader, PackageArchiveReader newReader, string outputDirectory, CancellationToken cancellationToken = default)
		{
			var packageDiff = await GenerateAsync(oldReader, newReader, cancellationToken).ConfigureAwait(false);

			// create the base package diff
			var xPackageDiff = CreatePackageDiff(packageDiff, cancellationToken);

			var totalMissing = 0;
			var totalExtra = 0;
			var totalWarning = 0;

			foreach (var (assembly, oldAssembly) in packageDiff.GetAllFrameworks().SelectMany(GetAllAssemblies))
			{
				var baseName = Path.Combine(outputDirectory, GetOutputFilenameBase(assembly, true));

				// load the assembly info and generate the diff
				using (var oldInfo = oldAssembly == null ? CreateEmptyApiInfo(assembly) : await GenerateAssemblyApiInfoAsync(oldReader, oldAssembly, cancellationToken).ConfigureAwait(false))
				using (var newInfo = await GenerateAssemblyApiInfoAsync(newReader, assembly, cancellationToken).ConfigureAwait(false))
				using (var xmlDiff = await GenerateAssemblyXmlDiffAsync(oldInfo, newInfo, cancellationToken).ConfigureAwait(false))
				{
					// there was a problem generating a diff, so bail out
					if (xmlDiff.Length == 0)
						continue;

					var xdoc = XDocument.Load(xmlDiff);
					var xassembly = xdoc.Root.Element("assembly");

					var xpresent = xassembly.Attribute("present_total");
					var xok = xassembly.Attribute("ok_total");
					var xcomplete = xassembly.Attribute("complete_total");
					var xmissing = xassembly.Attribute("missing_total");
					var xextras = xassembly.Attribute("extra_total");
					var xwarnings = xassembly.Attribute("warning_total");

					// copy the assembly changes to the package diff
					var xPackageAssembly = xPackageDiff.Root.Descendants("assembly").FirstOrDefault(a => a.Attribute("path")?.Value == assembly);
					// xPackageAssembly will be null if we are ignoring new assemblies and this is a new assembly
					xPackageAssembly?.Add(xpresent, xok, xcomplete, xmissing, xextras, xwarnings);
					totalMissing += int.Parse(xmissing?.Value ?? "0");
					totalExtra += int.Parse(xextras?.Value ?? "0");
					totalWarning += int.Parse(xwarnings?.Value ?? "0");

					// save the xml diff
					if (SaveAssemblyXmlDiff)
					{
						xmlDiff.Position = 0;
						await SaveToFileAsync(xmlDiff, baseName + GetExt(XmlDiffFileExtension, DefaultXmlDiffFileExtension), cancellationToken).ConfigureAwait(false);
					}

					// save the html diff
					if (SaveAssemblyHtmlDiff)
					{
						oldInfo.Position = 0;
						newInfo.Position = 0;
						using (var htmlDiff = await GenerateAssemblyHtmlDiffAsync(oldInfo, newInfo, cancellationToken).ConfigureAwait(false))
						{
							if (htmlDiff.Length > 0)
							{
								await SaveToFileAsync(htmlDiff, baseName + GetExt(HtmlDiffFileExtension, DefaultHtmlDiffFileExtension), cancellationToken).ConfigureAwait(false);
							}
						}
					}

					// save the md diff
					if (SaveAssemblyMarkdownDiff)
					{
						oldInfo.Position = 0;
						newInfo.Position = 0;
						using (var mdDiff = await GenerateAssemblyMarkdownDiffAsync(oldInfo, newInfo, cancellationToken).ConfigureAwait(false))
						{
							if (mdDiff.Length > 0)
							{
								await SaveToFileAsync(mdDiff, baseName + GetExt(MarkdownDiffFileExtension, DefaultMarkdownDiffFileExtension), cancellationToken).ConfigureAwait(false);
							}
						}
					}

					// save the api info
					if (SaveAssemblyApiInfo)
					{
						oldInfo.Position = 0;
						newInfo.Position = 0;
						await SaveToFileAsync(oldInfo, baseName + ".old" + GetExt(ApiInfoFileExtension, DefaultApiInfoFileExtension), cancellationToken).ConfigureAwait(false);
						await SaveToFileAsync(newInfo, baseName + ".new" + GetExt(ApiInfoFileExtension, DefaultApiInfoFileExtension), cancellationToken).ConfigureAwait(false);
					}
				}
			}

			// add the totals to the package element
			var xpackage = xPackageDiff.Root.Element("package");
			if (totalMissing > 0)
				xpackage.Add(new XAttribute("missing_total", totalMissing));
			if (totalExtra > 0)
				xpackage.Add(new XAttribute("extra_total", totalExtra));
			if (totalWarning > 0)
				xpackage.Add(new XAttribute("warning_total", totalWarning));

			// save the package diff
			if (SaveNuGetXmlDiff)
			{
				if (!Directory.Exists(outputDirectory))
					Directory.CreateDirectory(outputDirectory);

				var diffPath = Path.Combine(outputDirectory, $"{(packageDiff.OldIdentity ?? packageDiff.NewIdentity).Id}.nupkg" + GetExt(XmlDiffFileExtension, DefaultXmlDiffFileExtension));
				xPackageDiff.Save(diffPath);
			}

			IEnumerable<(string newA, string oldA)> GetAllAssemblies(NuGetFramework framework)
			{
				if (packageDiff.UnchangedAssemblies.TryGetValue(framework, out var unchangedAssemblies))
				{
					foreach (var a in unchangedAssemblies)
						yield return a;
				}

				if (packageDiff.AddedAssemblies.TryGetValue(framework, out var addedAssemblies))
				{
					var matched = packageDiff.SimilarAssemblies.TryGetValue(framework, out var matchedAssemblies);

					foreach (var a in addedAssemblies)
					{
						if (!IgnoreSimilarFrameworks && matched)
							yield return (a, matchedAssemblies.FirstOrDefault(m => m.newPath == a).oldPath);
						else if (!IgnoreAddedAssemblies)
							yield return (a, null);
					}
				}
			}
		}


		// OpenPackageAsync

		public Task<PackageArchiveReader> OpenPackageAsync(string id, string version, CancellationToken cancellationToken = default)
		{
			var identity = new PackageIdentity(id, NuGetVersion.Parse(version));
			return OpenPackageAsync(identity, cancellationToken);
		}

		public Task<PackageArchiveReader> OpenPackageAsync(string id, NuGetVersion version, CancellationToken cancellationToken = default)
		{
			var identity = new PackageIdentity(id, version);
			return OpenPackageAsync(identity, cancellationToken);
		}

		public async Task<PackageArchiveReader> OpenPackageAsync(PackageIdentity identity, CancellationToken cancellationToken = default)
		{
			var nupkgPath = await GetPackagePathAsync(identity, cancellationToken).ConfigureAwait(false);
			return new PackageArchiveReader(nupkgPath);
		}


		// ExtractPackageToDirectoryAsync

		public Task ExtractPackageToDirectoryAsync(string id, string version, string outputDirectory, CancellationToken cancellationToken = default)
		{
			var identity = new PackageIdentity(id, NuGetVersion.Parse(version));
			return ExtractPackageToDirectoryAsync(identity, outputDirectory, cancellationToken);
		}

		public Task ExtractPackageToDirectoryAsync(string id, NuGetVersion version, string outputDirectory, CancellationToken cancellationToken = default)
		{
			var identity = new PackageIdentity(id, version);
			return ExtractPackageToDirectoryAsync(identity, outputDirectory, cancellationToken);
		}

		public async Task ExtractPackageToDirectoryAsync(PackageIdentity identity, string outputDirectory, CancellationToken cancellationToken = default)
		{
			var nupkgPath = await GetPackagePathAsync(identity, cancellationToken).ConfigureAwait(false);

			using (var reader = await OpenPackageAsync(identity, cancellationToken).ConfigureAwait(false))
			{
				var files = await reader.GetFilesAsync(cancellationToken);
				foreach (var file in files.ToArray())
				{
					var dest = Path.Combine(outputDirectory, file);
					await reader.CopyFilesAsync(outputDirectory, files, ExtractFile, logger, cancellationToken);
				}
			}

			string ExtractFile(string source, string target, Stream stream)
			{
				var extractDirectory = Path.GetDirectoryName(target);
				if (!Directory.Exists(extractDirectory))
					Directory.CreateDirectory(extractDirectory);

				// the main .nuspec should be all lowercase to make things easy to find and match the clientz
				if (Path.GetFileName(source) == source && Path.GetExtension(source).ToLowerInvariant() == ".nuspec")
					target = Path.Combine(Path.GetDirectoryName(target), Path.GetFileName(target).ToLower());

				// copying files stream-to-stream is less efficient
				// attempt to copy using File.Copy if we the source is a file on disk
				if (Path.IsPathRooted(source))
					File.Copy(source, target, true);
				else
					stream.CopyToFile(target);

				return target;
			}
		}


		// ExtractCachedPackageAsync

		public Task<string> ExtractCachedPackageAsync(string id, string version, CancellationToken cancellationToken = default)
		{
			var identity = new PackageIdentity(id, NuGetVersion.Parse(version));
			return ExtractCachedPackageAsync(identity, cancellationToken);
		}

		public Task<string> ExtractCachedPackageAsync(string id, NuGetVersion version, CancellationToken cancellationToken = default)
		{
			var identity = new PackageIdentity(id, version);
			return ExtractCachedPackageAsync(identity, cancellationToken);
		}

		public async Task<string> ExtractCachedPackageAsync(PackageIdentity identity, CancellationToken cancellationToken = default)
		{
			var dir = GetCachedPackageDirectory(identity);

			// a quick check to make sure the package has not already beed extracted
			var nupkg = GetCachedPackagePath(identity);
			var extractedFlag = $"{nupkg}.extracted";
			if (File.Exists(nupkg) && File.Exists(extractedFlag))
				return dir;

			await ExtractPackageToDirectoryAsync(identity, dir, cancellationToken);

			File.WriteAllText(extractedFlag, "");

			return dir;
		}


		// OpenAssemblyAsync

		public async Task<Stream> OpenAssemblyAsync(PackageArchiveReader reader, string assemblyPath, CancellationToken cancellationToken = default)
		{
			var buffer = new MemoryStream();

			using (var stream = await reader.GetStreamAsync(assemblyPath, cancellationToken).ConfigureAwait(false))
			{
				await stream.CopyToAsync(buffer, DefaultCopyBufferSize, cancellationToken).ConfigureAwait(false);
			}

			buffer.Position = 0;
			return buffer;
		}


		// GetPackagePath

		public string GetCachedPackagePath(string id, string version)
		{
			var identity = new PackageIdentity(id, NuGetVersion.Parse(version));
			return GetCachedPackagePath(identity);
		}

		public string GetCachedPackagePath(string id, NuGetVersion version)
		{
			var identity = new PackageIdentity(id, version);
			return GetCachedPackagePath(identity);
		}

		public string GetCachedPackagePath(PackageIdentity ident)
		{
			var nupkgDir = GetCachedPackageDirectory(ident);
			return Path.Combine(nupkgDir, $"{ident.Id.ToLowerInvariant()}.{ident.Version.ToNormalizedString()}.nupkg");
		}


		// GetPackageRootDirectory

		public string GetCachedPackageDirectory(string id, string version)
		{
			var identity = new PackageIdentity(id, NuGetVersion.Parse(version));
			return GetCachedPackageDirectory(identity);
		}

		public string GetCachedPackageDirectory(string id, NuGetVersion version)
		{
			var identity = new PackageIdentity(id, version);
			return GetCachedPackageDirectory(identity);
		}

		public string GetCachedPackageDirectory(PackageIdentity ident)
		{
			return Path.Combine(PackageCache, GetPackageDirectoryBase(ident));
		}


		// Private members

		private static string GetExt(string extension, string fallback)
		{
			if (string.IsNullOrWhiteSpace(extension))
				return fallback;
			if (extension.StartsWith("."))
				return extension;
			return "." + extension;
		}

		private Stream CreateEmptyApiInfo(string assemblyPath)
		{
			var xdoc = new XDocument(new XElement("assemblies",
				new XElement("assembly",
					new XAttribute("name", Path.GetFileNameWithoutExtension(assemblyPath)),
					new XAttribute("version", "0.0.0.0"),
					new XElement("attributes"),
					new XElement("namespaces")
				)
			));

			var stream = new MemoryStream();
			xdoc.Save(stream);
			stream.Position = 0;

			return stream;
		}

		private ApiInfoConfig CreateApiInfoConfig()
		{
			var config = new ApiInfoConfig();

			if (SearchPaths?.Count > 0)
				config.SearchDirectories.AddRange(SearchPaths);
			config.IgnoreResolutionErrors = IgnoreResolutionErrors;
			config.IgnoreInheritedInterfaces = IgnoreInheritedInterfaces;

			return config;
		}

		private ApiDiffFormattedConfig CreateApiDiffFormattedConfig(ApiDiffFormatter formatter)
		{
			var config = new ApiDiffFormattedConfig();

			config.Formatter = formatter;
			config.IgnoreNonbreaking = IgnoreNonBreakingChanges;

			return config;
		}

		private XDocument CreatePackageDiff(NuGetDiffResult diff, CancellationToken cancellationToken = default)
		{
			var xdoc = new XDocument(
				new XElement("packages",
					new XElement("package",
						new XAttribute("id", (diff.OldIdentity ?? diff.NewIdentity).Id),
						diff.OldIdentity != null ? new XAttribute("version", diff.OldIdentity.Version) : null,
						new XElement("warnings", CreateWarnings().Select(w =>
							new XElement("warning", new XAttribute("text", w)))),
						new XElement("frameworks", diff.GetAllFrameworks().Select(fw =>
							new XElement("framework",
								new XAttribute("name", fw.Framework),
								new XAttribute("version", fw.Version),
								fw.HasProfile ? new XAttribute("profile", fw.Profile) : null,
								new XAttribute("short_name", fw.GetShortFolderName()),
								CreatePresence(fw),
								CreateAssemblies(fw))
							)
						)
					)
				)
			);

			return xdoc;

			XElement CreateAssemblies(NuGetFramework framework)
			{
				var assemblies = new List<(string newPath, string oldPath)>();

				// add the unchanged assemblies
				if (diff.UnchangedAssemblies.TryGetValue(framework, out var unchanged))
					assemblies.AddRange(unchanged);

				// add the new assemblies
				if (!IgnoreSimilarFrameworks && diff.SimilarAssemblies.TryGetValue(framework, out var matched))
					assemblies.AddRange(matched);
				else if (!IgnoreAddedAssemblies && diff.AddedAssemblies.TryGetValue(framework, out var added))
					assemblies.AddRange(added.Select(a => (a, a)));

				if (assemblies.Count == 0)
					return null;

				return
					new XElement("assemblies", assemblies.Select(ass =>
						new XElement("assembly",
							new XAttribute("name", Path.GetFileNameWithoutExtension(ass.newPath)),
							new XAttribute("path", ass.newPath),
							ass.oldPath != ass.newPath ? new XAttribute("old_path", ass.oldPath) : null,
							CreateAssemblyPresence(ass.newPath))
						)
					);
			}

			IEnumerable<string> CreateWarnings()
			{
				if (diff.OldIdentity?.Id != diff.NewIdentity.Id)
					yield return $"Package IDs not equal: {diff.OldIdentity?.Id}, {diff.NewIdentity.Id}";

				if (diff.OldIdentity?.Version != diff.NewIdentity.Version)
					yield return $"Package versions not equal: {diff.OldIdentity?.Version.ToNormalizedString()}, {diff.NewIdentity.Version.ToNormalizedString()}";
			}

			XAttribute CreatePresence(NuGetFramework framework)
			{
				if (diff.AddedFrameworks.Contains(framework))
					return new XAttribute("presence", "extra");

				if (diff.RemovedFrameworks.Contains(framework))
					return new XAttribute("presence", "missing");

				return null;
			}

			XAttribute CreateAssemblyPresence(string assembly)
			{
				if (diff.GetAllAddedAssemblies().Contains(assembly))
					return new XAttribute("presence", "extra");

				if (diff.GetAllRemovedAssemblies().Contains(assembly))
					return new XAttribute("presence", "missing");

				return null;
			}
		}

		private async Task<string> GetPackagePathAsync(PackageIdentity identity, CancellationToken cancellationToken)
		{
			var metadataResource = await source.GetResourceAsync<PackageMetadataResource>();

			var metadata = await metadataResource.GetMetadataAsync(identity, cache, logger, cancellationToken).ConfigureAwait(false);

			if (metadata == null)
				throw new ArgumentException($"Package identity is not valid: {identity}", nameof(identity));

			var ident = metadata.Identity;

			var nupkgDir = GetCachedPackageDirectory(ident);
			if (!Directory.Exists(nupkgDir))
				Directory.CreateDirectory(nupkgDir);

			var byId = await source.GetResourceAsync<FindPackageByIdResource>(cancellationToken).ConfigureAwait(false);

			var nupkgPath = GetCachedPackagePath(ident);
			var nupkgHashPath = $"{nupkgPath}.sha512";
			if (!File.Exists(nupkgPath) || !File.Exists(nupkgHashPath))
			{
				using (var downloader = await byId.GetPackageDownloaderAsync(ident, cache, logger, cancellationToken).ConfigureAwait(false))
				{
					await downloader.CopyNupkgFileToAsync(nupkgPath, cancellationToken).ConfigureAwait(false);

					var sha512 = await downloader.GetPackageHashAsync("SHA512", cancellationToken).ConfigureAwait(false);
					File.WriteAllText(nupkgHashPath, sha512);
				}
			}

			return nupkgPath;
		}

		private Task<Stream> GenerateAssemblyXmlDiffAsync(Stream oldInfo, Stream newInfo, CancellationToken cancellationToken)
		{
			return Task.Run(() =>
			{
				Stream info = new MemoryStream();

				using (var writer = new StreamWriter(info, UTF8NoBOM, DefaultSaveBufferSize, true))
				{
					ApiDiff.Generate(oldInfo, newInfo, writer);
				}

				info.Position = 0;
				return info;
			});
		}

		private Task<Stream> GenerateAssemblyHtmlDiffAsync(Stream oldInfo, Stream newInfo, CancellationToken cancellationToken)
		{
			return Task.Run(() =>
			{
				Stream info = new MemoryStream();

				using (var writer = new StreamWriter(info, UTF8NoBOM, DefaultSaveBufferSize, true))
				{
					ApiDiffFormatted.Generate(oldInfo, newInfo, writer, CreateApiDiffFormattedConfig(ApiDiffFormatter.Html));
				}

				info.Position = 0;
				return info;
			});
		}

		private Task<Stream> GenerateAssemblyMarkdownDiffAsync(Stream oldInfo, Stream newInfo, CancellationToken cancellationToken)
		{
			return Task.Run(() =>
			{
				Stream info = new MemoryStream();

				using (var writer = new StreamWriter(info, UTF8NoBOM, DefaultSaveBufferSize, true))
				{
					ApiDiffFormatted.Generate(oldInfo, newInfo, writer, CreateApiDiffFormattedConfig(ApiDiffFormatter.Markdown));
				}

				info.Position = 0;
				return info;
			});
		}

		private string GetOutputFilenameBase(string assembly, bool includePlatform)
		{
			assembly = assembly ?? string.Empty;
			var parts = assembly.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);

			// '/'
			if (parts.Length == 0)
				throw new ArgumentException($"Assembly path is not valid: {assembly}", nameof(assembly));

			// '/assembly.dll'
			if (parts.Length == 1)
				return parts[0];

			// '/lib/assembly.dll'
			// '/lib/<platform>/assembly.dll'
			// '/lib/<platform>/folder/assembly.dll'
			var skip = parts.Length == 2 || includePlatform ? 1 : 2;
			return string.Join(Path.DirectorySeparatorChar.ToString(), parts.Skip(skip));
		}

		private string GetPackageDirectoryBase(PackageIdentity ident)
		{
			return Path.Combine(ident.Id.ToLowerInvariant(), ident.Version.ToNormalizedString());
		}

		private async Task SaveToFileAsync(Stream stream, string path, CancellationToken cancellationToken)
		{
			var directory = Path.GetDirectoryName(path);
			if (!Directory.Exists(directory))
				Directory.CreateDirectory(directory);

			using (var file = File.Create(path))
			{
				await stream.CopyToAsync(file, DefaultCopyBufferSize, cancellationToken).ConfigureAwait(false);
			}
		}

		private class FrameworkGroup
		{
			public NuGetFramework Framework { get; set; }

			public (string path, string name)[] OldItems { get; set; }

			public (string path, string name)[] NewItems { get; set; }
		}
	}
}
