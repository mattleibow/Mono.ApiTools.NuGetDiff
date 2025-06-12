using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace Mono.ApiTools
{
	public class NuGetVersions
	{
		private static readonly SourceRepository source;
		private static readonly SourceCacheContext cache;
		private static readonly ILogger logger;

		static NuGetVersions()
		{
			source = Repository.Factory.GetCoreV3(NuGetDiff.NuGetSourceUrl);
			cache = new SourceCacheContext();
			logger = NullLogger.Instance;
		}

		public static async Task<NuGetVersion> GetLatestAsync(string id, Filter filter = null, CancellationToken cancellationToken = default)
		{
			var versions = await EnumerateAllAsync(id, filter, cancellationToken);

			// If we prefer release and there are some release, then we should filter out prerelease 
			if (filter?.PreferRelease == true && versions.Any(v => !v.IsPrerelease))
				versions = versions.Where(v => !v.IsPrerelease);

			// If we have a range filter, we find the best match
			if (filter?.VersionRange != null)
				return filter.VersionRange.FindBestMatch(versions);

			// We have no filter, so we just return the latest
			return versions.Reverse().FirstOrDefault();
		}

		public static async Task<NuGetVersion[]> GetAllAsync(string id, Filter filter = null, CancellationToken cancellationToken = default)
		{
			var versions = await EnumerateAllAsync(id, filter, cancellationToken);
			return versions.ToArray();
		}

		private static async Task<IEnumerable<NuGetVersion>> EnumerateAllAsync(string id, Filter filter, CancellationToken cancellationToken)
		{
			var sourceToUse = source;

			filter ??= new Filter();

			if(!string.IsNullOrEmpty(filter.SourceUrl))
				sourceToUse = Repository.Factory.GetCoreV3(filter.SourceUrl);

			var resource = await sourceToUse.GetResourceAsync<MetadataResource>(cancellationToken);

			var versions = await resource.GetVersions(id, filter.IncludePrerelease, filter.IncludeUnlisted, cache, logger, cancellationToken);

			versions = versions.Where(v =>
				(filter.MinimumVersion == null || v >= filter.MinimumVersion) &&
				(filter.MaximumVersion == null || v < filter.MaximumVersion));

			return versions;
		}

		public class Filter
		{
			public bool IncludePrerelease { get; set; }

			public bool PreferRelease { get; set; }

			public bool IncludeUnlisted { get; set; }

			public NuGetVersion MinimumVersion { get; set; }

			public NuGetVersion MaximumVersion { get; set; }

			public VersionRange VersionRange { get; set; }

			public string SourceUrl { get; set; }
		}
	}
}
