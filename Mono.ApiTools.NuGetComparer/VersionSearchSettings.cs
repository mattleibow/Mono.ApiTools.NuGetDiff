using NuGet.Versioning;

namespace Mono.ApiTools.NuGetComparer
{
	public class VersionSearchSettings
	{
		public bool IncludePrerelease { get; set; }

		public NuGetVersion MinimumVersion { get; set; }

		public NuGetVersion MaximumVersion { get; set; }
	}
}
