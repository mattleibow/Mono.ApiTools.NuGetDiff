using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace Mono.ApiTools.NuGetComparer
{
	public class PackageVersions
	{
		private static readonly SourceRepository source;
		private static readonly SourceCacheContext cache;
		private static readonly ILogger logger;

		static PackageVersions()
		{
			source = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
			cache = new SourceCacheContext();
			logger = NullLogger.Instance;
		}

		public static async Task<NuGetVersion> GetLatestAsync(string id, VersionSearchSettings settings = null, CancellationToken cancellationToken = default)
		{
			settings = settings ?? new VersionSearchSettings();

			NuGetVersion latestVersion = null;

			var versions = await GetAllAsync(id, cancellationToken);

			foreach (var version in versions.Reverse())
			{
				// first check against settings
				if (!settings.IncludePrerelease && version.IsPrerelease)
					continue;
				if (settings.MinimumVersion != null && version < settings.MinimumVersion)
					continue;
				if (settings.MaximumVersion != null && version > settings.MaximumVersion)
					continue;

				// check against last version
				if (latestVersion != null && version < latestVersion)
					continue;

				// looks good
				latestVersion = version;
				break;
			}

			return latestVersion;
		}

		public static async Task<NuGetVersion[]> GetAllAsync(string id, CancellationToken cancellationToken = default)
		{
			var byId = await source.GetResourceAsync<FindPackageByIdResource>(cancellationToken);

			var versions = await byId.GetAllVersionsAsync(id, cache, logger, cancellationToken);

			return versions.ToArray();
		}
	}
}
