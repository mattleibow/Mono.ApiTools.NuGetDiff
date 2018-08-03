using NuGet.Versioning;
using System;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using Xunit;

namespace Mono.ApiTools.Tests
{
	public class PackageVersionsTests
	{
		[Fact]
		public async Task TestGetLatest()
		{
			var version = await NuGetVersions.GetLatestAsync("Xamarin.Forms");

			Assert.True(version >= NuGetVersion.Parse("3.1.0.637273"));
		}

		[Fact]
		public async Task TestGetLatestWithMax()
		{
			var settings = new NuGetVersions.Filter
			{
				MaximumVersion = NuGetVersion.Parse("3.1.0")
			};

			var version = await NuGetVersions.GetLatestAsync("Xamarin.Forms", settings);

			Assert.Equal(NuGetVersion.Parse("3.0.0.561731"), version);
		}

		[Fact]
		public async Task TestGetLatestWithMin()
		{
			var settings = new NuGetVersions.Filter
			{
				MinimumVersion = NuGetVersion.Parse("2.0.0")
			};

			var version = await NuGetVersions.GetLatestAsync("Xamarin.Forms", settings);

			Assert.True(version >= NuGetVersion.Parse("3.1.0.637273"));
		}

		[Fact]
		public async Task TestGetLatestWithMaxMin()
		{
			var settings = new NuGetVersions.Filter
			{
				MaximumVersion = NuGetVersion.Parse("3.1.0"),
				MinimumVersion = NuGetVersion.Parse("2.0.0")
			};

			var version = await NuGetVersions.GetLatestAsync("Xamarin.Forms", settings);

			Assert.Equal(NuGetVersion.Parse("3.0.0.561731"), version);
		}

		[Fact]
		public async Task TestGetLatestWithReversedMaxMin()
		{
			var settings = new NuGetVersions.Filter
			{
				MinimumVersion = NuGetVersion.Parse("3.1.0"),
				MaximumVersion = NuGetVersion.Parse("2.0.0")
			};

			var version = await NuGetVersions.GetLatestAsync("Xamarin.Forms", settings);

			Assert.Null(version);
		}
	}
}