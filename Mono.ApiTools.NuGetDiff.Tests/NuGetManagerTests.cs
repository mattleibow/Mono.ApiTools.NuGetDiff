using NuGet.Versioning;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Mono.ApiTools.Tests
{
	public class NuGetManagerTests
	{
		[Fact]
		public async Task TestNuGetManagerCanOpenPackage()
		{
			var manager = new NuGetManager();

			using (var reader = await manager.OpenPackageAsync("Newtonsoft.Json", "13.0.1"))
			{
				Assert.NotNull(reader);
				var identity = await reader.GetIdentityAsync(default);
				Assert.Equal("Newtonsoft.Json", identity.Id);
				Assert.Equal(NuGetVersion.Parse("13.0.1"), identity.Version);
			}
		}

		[Fact]
		public async Task TestNuGetManagerCanExtractPackage()
		{
			var manager = new NuGetManager();
			var outputDir = Path.Combine(Path.GetTempPath(), "Mono.ApiTools.NuGetDiff", Path.GetRandomFileName());

			try
			{
				await manager.ExtractPackageToDirectoryAsync("Newtonsoft.Json", "13.0.1", outputDir);

				Assert.True(Directory.Exists(outputDir));
				Assert.NotEmpty(Directory.GetFiles(outputDir, "*.nuspec", SearchOption.AllDirectories));
			}
			finally
			{
				if (Directory.Exists(outputDir))
				{
					try
					{
						Directory.Delete(outputDir, true);
					}
					catch
					{
						// Ignore cleanup errors
					}
				}
			}
		}

		[Fact]
		public async Task TestNuGetManagerCanExtractCachedPackage()
		{
			var manager = new NuGetManager();
			manager.PackageCache = Path.Combine(Path.GetTempPath(), "Mono.ApiTools.NuGetDiff", "cache", Path.GetRandomFileName());

			try
			{
				var dir = await manager.ExtractCachedPackageAsync("Newtonsoft.Json", "13.0.1");

				Assert.True(Directory.Exists(dir));
				Assert.NotEmpty(Directory.GetFiles(dir, "*.nuspec", SearchOption.AllDirectories));
			}
			finally
			{
				if (Directory.Exists(manager.PackageCache))
				{
					try
					{
						Directory.Delete(manager.PackageCache, true);
					}
					catch
					{
						// Ignore cleanup errors
					}
				}
			}
		}

		[Fact]
		public void TestNuGetManagerGetCachedPackagePath()
		{
			var manager = new NuGetManager();
			manager.PackageCache = "packages";

			var path = manager.GetCachedPackagePath("Newtonsoft.Json", "13.0.1");

			Assert.Equal("packages/newtonsoft.json/13.0.1/newtonsoft.json.13.0.1.nupkg", path.Replace('\\', '/'));
		}

		[Fact]
		public void TestNuGetManagerGetCachedPackageDirectory()
		{
			var manager = new NuGetManager();
			manager.PackageCache = "packages";

			var dir = manager.GetCachedPackageDirectory("Newtonsoft.Json", "13.0.1");

			Assert.Equal("packages/newtonsoft.json/13.0.1", dir.Replace('\\', '/'));
		}
	}
}
