using NuGet.Versioning;

namespace NuGetComparer
{
	public class VersionSearchSettings
	{
		public bool IncludePrerelease { get; set; }

		public NuGetVersion MinimumVersion { get; set; }

		public NuGetVersion MaximumVersion { get; set; }
	}
}
