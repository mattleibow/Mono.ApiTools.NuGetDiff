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
			source = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
			cache = new SourceCacheContext();
			logger = NullLogger.Instance;
		}

		public static async Task<NuGetVersion> GetLatestAsync(string id, Filter filter = null, CancellationToken cancellationToken = default)
		{
			var versions = await EnumerateAllAsync(id, filter, cancellationToken);
			return versions.Reverse().FirstOrDefault();
		}

		public static async Task<NuGetVersion[]> GetAllAsync(string id, Filter filter = null, CancellationToken cancellationToken = default)
		{
			var versions = await EnumerateAllAsync(id, filter, cancellationToken);
			return versions.ToArray();
		}

		private static async Task<IEnumerable<NuGetVersion>> EnumerateAllAsync(string id, Filter filter, CancellationToken cancellationToken)
		{
			filter = filter ?? new Filter();

			var resource = await source.GetResourceAsync<MetadataResource>(cancellationToken);

			var versions = await resource.GetVersions(id, filter.IncludePrerelease, filter.IncludeUnlisted, cache, logger, cancellationToken);

			versions = versions.Where(v =>
				(filter.MinimumVersion == null || v >= filter.MinimumVersion) &&
				(filter.MaximumVersion == null || v < filter.MaximumVersion));

			return versions;
		}

		public class Filter
		{
			public bool IncludePrerelease { get; set; }

			public bool IncludeUnlisted { get; set; }

			public NuGetVersion MinimumVersion { get; set; }

			public NuGetVersion MaximumVersion { get; set; }
		}
	}
}
