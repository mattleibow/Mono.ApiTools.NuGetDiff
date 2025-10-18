using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace Mono.ApiTools
{
	public class NuGetManager
	{
		internal const string NuGetSourceUrl = "https://api.nuget.org/v3/index.json";

		private const int DefaultCopyBufferSize = 81920;

		protected readonly SourceRepository source;
		protected readonly SourceCacheContext cache;
		protected readonly ILogger logger;

		public NuGetManager()
			: this(NuGetSourceUrl)
		{
		}

		public NuGetManager(string sourceUrl)
		{
			source = Repository.Factory.GetCoreV3(sourceUrl);
			cache = new SourceCacheContext();
			logger = NullLogger.Instance;
		}


		// Properties

		public string PackageCache { get; set; } = "packages";


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
			return ExtractPackageToDirectoryAsync(id, version, outputDirectory, false, cancellationToken);
		}

		public Task ExtractPackageToDirectoryAsync(string id, NuGetVersion version, string outputDirectory, CancellationToken cancellationToken = default)
		{
			return ExtractPackageToDirectoryAsync(id, version, outputDirectory, false, cancellationToken);
		}

		public Task ExtractPackageToDirectoryAsync(PackageIdentity identity, string outputDirectory, CancellationToken cancellationToken = default)
		{
			return ExtractPackageToDirectoryAsync(identity, outputDirectory, false, cancellationToken);
		}

		public Task ExtractPackageToDirectoryAsync(string id, string version, string outputDirectory, bool includeDependencies, CancellationToken cancellationToken = default)
		{
			var identity = new PackageIdentity(id, NuGetVersion.Parse(version));
			return ExtractPackageToDirectoryAsync(identity, outputDirectory, includeDependencies, cancellationToken);
		}

		public Task ExtractPackageToDirectoryAsync(string id, NuGetVersion version, string outputDirectory, bool includeDependencies, CancellationToken cancellationToken = default)
		{
			var identity = new PackageIdentity(id, version);
			return ExtractPackageToDirectoryAsync(identity, outputDirectory, includeDependencies, cancellationToken);
		}

		public async Task ExtractPackageToDirectoryAsync(PackageIdentity identity, string outputDirectory, bool includeDependencies, CancellationToken cancellationToken = default)
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

				// Extract dependencies recursively if requested
				if (includeDependencies)
				{
					var dependencySets = await reader.GetPackageDependenciesAsync(cancellationToken).ConfigureAwait(false);
					foreach (var dependencySet in dependencySets)
					{
						foreach (var dependency in dependencySet.Packages)
						{
							// Try to use MinVersion if available, otherwise try MaxVersion
							var version = dependency.VersionRange?.MinVersion ?? dependency.VersionRange?.MaxVersion;
							if (version != null)
							{
								var depIdentity = new PackageIdentity(dependency.Id, version);
								try
								{
									await ExtractPackageToDirectoryAsync(depIdentity, outputDirectory, includeDependencies, cancellationToken).ConfigureAwait(false);
								}
								catch
								{
									// Silently ignore errors extracting dependencies
									// This prevents failures when dependencies are not available
								}
							}
						}
					}
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
			return ExtractCachedPackageAsync(id, version, false, cancellationToken);
		}

		public Task<string> ExtractCachedPackageAsync(string id, NuGetVersion version, CancellationToken cancellationToken = default)
		{
			return ExtractCachedPackageAsync(id, version, false, cancellationToken);
		}

		public Task<string> ExtractCachedPackageAsync(PackageIdentity identity, CancellationToken cancellationToken = default)
		{
			return ExtractCachedPackageAsync(identity, false, cancellationToken);
		}

		public Task<string> ExtractCachedPackageAsync(string id, string version, bool includeDependencies, CancellationToken cancellationToken = default)
		{
			var identity = new PackageIdentity(id, NuGetVersion.Parse(version));
			return ExtractCachedPackageAsync(identity, includeDependencies, cancellationToken);
		}

		public Task<string> ExtractCachedPackageAsync(string id, NuGetVersion version, bool includeDependencies, CancellationToken cancellationToken = default)
		{
			var identity = new PackageIdentity(id, version);
			return ExtractCachedPackageAsync(identity, includeDependencies, cancellationToken);
		}

		public async Task<string> ExtractCachedPackageAsync(PackageIdentity identity, bool includeDependencies, CancellationToken cancellationToken = default)
		{
			var dir = GetCachedPackageDirectory(identity);

			// a quick check to make sure the package has not already beed extracted
			var nupkg = GetCachedPackagePath(identity);
			var extractedFlag = $"{nupkg}.extracted";
			if (File.Exists(nupkg) && File.Exists(extractedFlag))
				return dir;

			await ExtractPackageToDirectoryAsync(identity, dir, includeDependencies, cancellationToken);

			File.WriteAllText(extractedFlag, "");

			return dir;
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

		private string GetPackageDirectoryBase(PackageIdentity ident)
		{
			return Path.Combine(ident.Id.ToLowerInvariant(), ident.Version.ToNormalizedString());
		}
	}
}
