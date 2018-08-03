using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Mono.ApiTools;
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
		private const int DefaultSaveBufferSize = 1024;
		private const int DefaultCopyBufferSize = 81920;
		private static readonly Encoding UTF8NoBOM = new UTF8Encoding(false, true);

		private static readonly SourceRepository source;
		private static readonly SourceCacheContext cache;
		private static readonly ILogger logger;

		static NuGetDiff()
		{
			source = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
			cache = new SourceCacheContext();
			logger = NullLogger.Instance;
		}


		// Properties

		public List<string> SearchPaths { get; set; } = new List<string>();

		public string PackageCache { get; set; } = "packages";

		public bool IgnoreResolutionErrors { get; set; } = false;

		public bool IgnoreInheritedInterfaces { get; set; } = false;

		public bool IgnoreAddedAssemblies { get; set; } = false;

		public bool SaveAssemblyApiInfo { get; set; } = false;

		public bool SaveAssemblyHtmlDiff { get; set; } = false;

		public bool SaveAssemblyMarkdownDiff { get; set; } = false;

		public bool SaveAssemblyXmlDiff { get; set; } = false;


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
			var mergedFrameworks = oldItems.Select(i => i.TargetFramework).Union(newItems.Select(i => i.TargetFramework));
			var mergedGroup = mergedFrameworks.Select(fw => new FrameworkGroup
			{
				Framework = fw,
				OldItems = GetFrameworkAssemblies(fw, oldItems),
				NewItems = GetFrameworkAssemblies(fw, newItems),
			}).ToArray();

			return new NuGetDiffResult
			{
				// versions
				OldIdentity = oldIdentity,
				NewIdentity = newIdentity,

				// frameworks
				AddedFrameworks = mergedGroup.Where(g => !g.OldItems.Any()).Select(g => g.Framework).ToArray(),
				RemovedFrameworks = mergedGroup.Where(g => !g.NewItems.Any()).Select(g => g.Framework).ToArray(),
				UnchangedFrameworks = mergedGroup.Where(g => g.OldItems.Any() && g.NewItems.Any()).Select(g => g.Framework).ToArray(),

				// assemblies
				AddedAssemblies = GetAssemblies(g => g.NewItems.Except(g.OldItems)),
				RemovedAssemblies = GetAssemblies(g => g.OldItems.Except(g.NewItems)),
				UnchangedAssemblies = GetAssemblies(g => g.OldItems.Intersect(g.NewItems)),
			};

			Dictionary<NuGetFramework, string[]> GetAssemblies(Func<FrameworkGroup, IEnumerable<string>> filter)
			{
				return mergedGroup
					.Select(g => new { g.Framework, Items = filter(g).ToArray() })
					.Where(g => g.Items.Any())
					.ToDictionary(g => g.Framework, g => g.Items);
			}

			string[] GetFrameworkAssemblies(NuGetFramework fw, IEnumerable<FrameworkSpecificGroup> items)
			{
				return items
					?.FirstOrDefault(i => i.TargetFramework == fw)
					?.Items
					?.Where(i => Path.GetExtension(i).ToLowerInvariant() == ".dll")
					?.ToArray()
					?? new string[0];
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

			var unchanged = packageDiff.GetAllUnchangedAssemblies();
			var added = packageDiff.GetAllAddedAssemblies();
			var assemblies = IgnoreAddedAssemblies ? unchanged : unchanged.Union(added);

			foreach (var assembly in assemblies)
			{
				var baseName = Path.Combine(outputDirectory, GetOutputFilenameBase(assembly));
				var isNew = added.Contains(assembly);

				// load the assembly info and generate the diff
				using (var oldInfo = isNew ? CreateEmptyApiInfo(assembly) : await GenerateAssemblyApiInfoAsync(oldReader, assembly, cancellationToken).ConfigureAwait(false))
				using (var newInfo = await GenerateAssemblyApiInfoAsync(newReader, assembly, cancellationToken).ConfigureAwait(false))
				using (var xmlDiff = await GenerateAssemblyXmlDiffAsync(oldInfo, newInfo, cancellationToken).ConfigureAwait(false))
				{
					// there were no changes at all, so skip this assembly
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

					// there appears to be no changes or warnings, so skip as well
					if (xmissing == null && xextras == null && xwarnings == null)
						continue;

					// copy the assembly changes to the package diff
					var xPackageAssembly = xPackageDiff.Root.Descendants("assembly").FirstOrDefault(a => a.Attribute("path")?.Value == assembly);
					xPackageAssembly.Add(xpresent, xok, xcomplete, xmissing, xextras, xwarnings);
					totalMissing += int.Parse(xmissing?.Value ?? "0");
					totalExtra += int.Parse(xextras?.Value ?? "0");
					totalWarning += int.Parse(xwarnings?.Value ?? "0");

					// save the xml diff
					if (SaveAssemblyXmlDiff)
					{
						xmlDiff.Position = 0;
						await SaveToFileAsync(xmlDiff, baseName + ".diff.xml", cancellationToken).ConfigureAwait(false);
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
								await SaveToFileAsync(htmlDiff, baseName + ".diff.html", cancellationToken).ConfigureAwait(false);
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
								await SaveToFileAsync(mdDiff, baseName + ".diff.md", cancellationToken).ConfigureAwait(false);
							}
						}
					}

					// save the api info
					if (SaveAssemblyApiInfo)
					{
						oldInfo.Position = 0;
						newInfo.Position = 0;
						await SaveToFileAsync(oldInfo, baseName + ".old.info.xml", cancellationToken).ConfigureAwait(false);
						await SaveToFileAsync(newInfo, baseName + ".new.info.xml", cancellationToken).ConfigureAwait(false);
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
			if (!Directory.Exists(outputDirectory))
				Directory.CreateDirectory(outputDirectory);
			var diffPath = Path.Combine(outputDirectory, $"{(packageDiff.OldIdentity ?? packageDiff.NewIdentity).Id}.nupkg.diff.xml");
			xPackageDiff.Save(diffPath);
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

			await ExtractPackageToDirectoryAsync(identity, dir);

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
				var assemblies = new List<string>();

				if (diff.UnchangedAssemblies.TryGetValue(framework, out var unchanged))
					assemblies.AddRange(unchanged);
				if (!IgnoreAddedAssemblies && diff.AddedAssemblies.TryGetValue(framework, out var added))
					assemblies.AddRange(added);

				if (assemblies.Count == 0)
					return null;

				return
					new XElement("assemblies", assemblies.Select(ass =>
						new XElement("assembly",
							new XAttribute("name", Path.GetFileNameWithoutExtension(ass)),
							new XAttribute("path", ass),
							CreateAssemblyPresence(ass))
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
					ApiDiffFormatted.Generate(oldInfo, newInfo, writer, new ApiDiffFormattedConfig { Formatter = ApiDiffFormatter.Html });
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
					ApiDiffFormatted.Generate(oldInfo, newInfo, writer, new ApiDiffFormattedConfig { Formatter = ApiDiffFormatter.Markdown });
				}

				info.Position = 0;
				return info;
			});
		}

		private string GetOutputFilenameBase(string assembly)
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
			return string.Join(Path.DirectorySeparatorChar.ToString(), parts.Skip(1));
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

			using (var file = File.OpenWrite(path))
			{
				await stream.CopyToAsync(file, DefaultCopyBufferSize, cancellationToken).ConfigureAwait(false);
			}
		}

		private class FrameworkGroup
		{
			public NuGetFramework Framework { get; set; }

			public string[] OldItems { get; set; }

			public string[] NewItems { get; set; }
		}
	}
}
