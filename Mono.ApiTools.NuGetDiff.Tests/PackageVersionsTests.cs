using System.Linq;
using System.Threading.Tasks;
using NuGet.Versioning;
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
				MinimumVersion = NuGetVersion.Parse("2.0.0"),
				MaximumVersion = NuGetVersion.Parse("3.1.0")
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

		[Fact]
		public async Task TestGetAll()
		{
			var versions = await NuGetVersions.GetAllAsync("Xamarin.Forms");

			Assert.Equal(NuGetVersion.Parse("1.0.6188"), versions.First());
			Assert.NotEqual(NuGetVersion.Parse("1.2.1.6224"), versions[7]);
			Assert.Equal(NuGetVersion.Parse("1.2.2.6243"), versions[5]);
		}

		[Fact]
		public async Task TestGetAllWithUnlisted()
		{
			var settings = new NuGetVersions.Filter
			{
				IncludeUnlisted = true
			};

			var versions = await NuGetVersions.GetAllAsync("Xamarin.Forms", settings);

			Assert.Equal(NuGetVersion.Parse("1.0.6186"), versions.First());
			Assert.Equal(NuGetVersion.Parse("1.2.1.6224"), versions[7]);
			Assert.Equal(NuGetVersion.Parse("1.2.2.6243"), versions[9]);
		}

		[Fact]
		public async Task TestGetAllWithPrerelease()
		{
			var settings = new NuGetVersions.Filter
			{
				IncludePrerelease = true
			};

			var versions = await NuGetVersions.GetAllAsync("Xamarin.Forms", settings);

			Assert.Equal(NuGetVersion.Parse("1.0.6188"), versions.First());
			Assert.Equal(NuGetVersion.Parse("1.2.2.6238-pre1"), versions[5]);
		}

		[Fact]
		public async Task TestGetAllWithMax()
		{
			var settings = new NuGetVersions.Filter
			{
				MaximumVersion = NuGetVersion.Parse("3.1.0")
			};

			var version = await NuGetVersions.GetAllAsync("Xamarin.Forms", settings);

			Assert.Equal(NuGetVersion.Parse("3.0.0.561731"), version.Last());
			Assert.Equal(NuGetVersion.Parse("3.0.0.561731"), version.Last());
		}

		[Fact]
		public async Task TestGetAllWithMin()
		{
			var settings = new NuGetVersions.Filter
			{
				MinimumVersion = NuGetVersion.Parse("2.0.0")
			};

			var version = await NuGetVersions.GetAllAsync("Xamarin.Forms", settings);

			Assert.True(version.Last() >= NuGetVersion.Parse("3.1.0.637273"));
		}

		[Fact]
		public async Task TestGetAllWithMaxMin()
		{
			var settings = new NuGetVersions.Filter
			{
				MinimumVersion = NuGetVersion.Parse("2.0.0"),
				MaximumVersion = NuGetVersion.Parse("3.1.0")
			};

			var version = await NuGetVersions.GetAllAsync("Xamarin.Forms", settings);

			Assert.Equal(NuGetVersion.Parse("3.0.0.561731"), version.Last());
		}

		[Fact]
		public async Task TestGetAllWithReversedMaxMin()
		{
			var settings = new NuGetVersions.Filter
			{
				MinimumVersion = NuGetVersion.Parse("3.1.0"),
				MaximumVersion = NuGetVersion.Parse("2.0.0")
			};

			var version = await NuGetVersions.GetAllAsync("Xamarin.Forms", settings);

			Assert.Empty(version);
		}

		[Fact]
		public async Task TestGetLatestWithRange()
		{
			var version = await NuGetVersions.GetLatestAsync(
				"Microsoft.Extensions.DependencyInjection",
				new NuGetVersions.Filter
				{
					VersionRange = VersionRange.Parse("5.0.0")
				});

			Assert.Equal(NuGetVersion.Parse("5.0.0"), version);
		}

		[Theory]
		[InlineData("[5.0.0-preview.*,5.0.0)", "5.0.0-preview.8.20407.11")]
		[InlineData("[5.0.0-preview.7.*,5.0.0)", "5.0.0-preview.7.20364.11")]
		public async Task TestGetLatestWithPreReleaseRange(string range, string matched)
		{
			var version = await NuGetVersions.GetLatestAsync(
				"Microsoft.Extensions.DependencyInjection",
				new NuGetVersions.Filter
				{
					IncludePrerelease = true,
					VersionRange = VersionRange.Parse(range),
				});

			Assert.Equal(NuGetVersion.Parse(matched), version);
		}

		[Theory]
		[InlineData(true, true, "2.0.0")]
		[InlineData(true, false, "2.0.0")]
		[InlineData(false, true, "2.1.0-preview.2")]
		[InlineData(false, false, "2.0.0")]
		public async Task TestGetLatestWithPreferRelease(bool preferRelease, bool includePrerelease, string expectedVersion)
		{
			var version = await NuGetVersions.GetLatestAsync(
				"SkiaSharp.Extended.UI.Forms",
				new NuGetVersions.Filter
				{
					PreferRelease = preferRelease,
					IncludePrerelease = includePrerelease
				});

			Assert.NotNull(version);
			Assert.Equal(NuGetVersion.Parse(expectedVersion), version);
		}

		[Theory]
		[InlineData(true, "6.0.101-preview.11.2349")]
		[InlineData(false, "6.0.101-preview.11.2349")]
		public async Task TestGetLatestPrereleaseWithPreferReleaseWhenNoneAvailable(bool preferRelease, string expectedVersion)
		{
			var version = await NuGetVersions.GetLatestAsync(
				"Microsoft.NET.Sdk.Maui.Manifest-6.0.100",
				new NuGetVersions.Filter
				{
					IncludePrerelease = true,
					PreferRelease = preferRelease
				});

			Assert.NotNull(version);
			Assert.True(version.IsPrerelease);
			Assert.Equal(NuGetVersion.Parse(expectedVersion), version);
		}
	}
}
