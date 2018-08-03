using Mono.Cecil;
using NuGet.Packaging;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using Xunit;

namespace Mono.ApiTools.Tests
{
	public class PackageComparerTests
	{
		private const string FormsPackageId = "Xamarin.Forms";

		private const string FormsV15Number1 = "1.5.1.6471";
		private const string FormsV20Number1 = "2.0.0.6482";
		private const string FormsV25Number1 = "2.5.0.280555";
		private const string FormsV30Number1 = "3.0.0.550146";
		private const string FormsV30Number2 = "3.0.0.561731";
		private const string FormsV30Number3 = "3.0.0.446417";
		private const string FormsV31Number1 = "3.1.0.697729";

		private const string FormsV20Url1 = "https://www.nuget.org/api/v2/package/Xamarin.Forms/2.0.0.6482";
		private const string FormsV30Url1 = "https://www.nuget.org/api/v2/package/Xamarin.Forms/3.0.0.550146";
		private const string FormsV3Url2 = "https://www.nuget.org/api/v2/package/Xamarin.Forms/3.0.0.561731";

		private const string SkiaPackageId = "SkiaSharp";
		private const string SkiaV600Number1 = "1.60.0";
		private const string SkiaV602Number1 = "1.60.2";

		private static readonly string[] searchPaths;

		static PackageComparerTests()
		{
			searchPaths = GetSearchPaths().ToArray();
		}

		[Fact]
		public async Task TestComparePackageWithSameAssemblies()
		{
			var comparer = new NuGetDiff();
			comparer.SearchPaths.AddRange(searchPaths);

			var diff = await comparer.GenerateAsync(FormsPackageId, FormsV30Number1, FormsV30Number2);

			Assert.Equal(NuGetVersion.Parse(FormsV30Number1), diff.OldIdentity.Version);
			Assert.Equal(NuGetVersion.Parse(FormsV30Number2), diff.NewIdentity.Version);

			Assert.Empty(diff.AddedFrameworks);
			Assert.Empty(diff.RemovedFrameworks);
			Assert.NotEmpty(diff.UnchangedFrameworks);

			Assert.Empty(diff.AddedAssemblies);
			Assert.Empty(diff.RemovedAssemblies);
			Assert.NotEmpty(diff.UnchangedAssemblies);
		}

		[Fact]
		public async Task TestComparePackage()
		{
			var comparer = new NuGetDiff();
			comparer.SearchPaths.AddRange(searchPaths);

			var diff = await comparer.GenerateAsync(FormsPackageId, FormsV20Number1, FormsV30Number2);

			Assert.Equal(NuGetVersion.Parse(FormsV20Number1), diff.OldIdentity.Version);
			Assert.Equal(NuGetVersion.Parse(FormsV30Number2), diff.NewIdentity.Version);

			Assert.NotEmpty(diff.AddedFrameworks);
			Assert.NotEmpty(diff.RemovedFrameworks);
			Assert.NotEmpty(diff.UnchangedFrameworks);

			Assert.NotEmpty(diff.AddedAssemblies);
			Assert.NotEmpty(diff.RemovedAssemblies);
			Assert.NotEmpty(diff.UnchangedAssemblies);
		}

		[Fact]
		public async Task TestCompareSamePackage()
		{
			var comparer = new NuGetDiff();
			comparer.SearchPaths.AddRange(searchPaths);

			var diff = await comparer.GenerateAsync(FormsPackageId, FormsV20Number1, FormsV20Number1);

			Assert.Equal(NuGetVersion.Parse(FormsV20Number1), diff.OldIdentity.Version);
			Assert.Equal(NuGetVersion.Parse(FormsV20Number1), diff.NewIdentity.Version);

			Assert.Empty(diff.AddedFrameworks);
			Assert.Empty(diff.RemovedFrameworks);
			Assert.NotEmpty(diff.UnchangedFrameworks);

			Assert.Empty(diff.AddedAssemblies);
			Assert.Empty(diff.RemovedAssemblies);
			Assert.NotEmpty(diff.UnchangedAssemblies);
		}

		[Fact]
		public async Task TestCompareRemoteFileWithLocalFile()
		{
			var newPath = GenerateTestOutputPath();

			using (var wc = new WebClient())
			{
				await wc.DownloadFileTaskAsync(FormsV3Url2, newPath);
			}

			var comparer = new NuGetDiff();
			comparer.SearchPaths.AddRange(searchPaths);

			var diff = await comparer.GenerateAsync(FormsPackageId, FormsV20Number1, new PackageArchiveReader(newPath));

			Assert.Equal(NuGetVersion.Parse(FormsV20Number1), diff.OldIdentity.Version);
			Assert.Equal(NuGetVersion.Parse(FormsV30Number2), diff.NewIdentity.Version);

			Assert.NotEmpty(diff.AddedFrameworks);
			Assert.NotEmpty(diff.RemovedFrameworks);
			Assert.NotEmpty(diff.UnchangedFrameworks);

			Assert.NotEmpty(diff.AddedAssemblies);
			Assert.NotEmpty(diff.RemovedAssemblies);
			Assert.NotEmpty(diff.UnchangedAssemblies);
		}

		[Fact]
		public async Task TestCompareTwoLocalFiles()
		{
			var oldPath = GenerateTestOutputPath();
			var newPath = GenerateTestOutputPath();

			using (var wc = new WebClient())
			{
				await wc.DownloadFileTaskAsync(FormsV20Url1, oldPath);
				await wc.DownloadFileTaskAsync(FormsV3Url2, newPath);
			}

			var comparer = new NuGetDiff();
			comparer.SearchPaths.AddRange(searchPaths);

			var diff = await comparer.GenerateAsync(oldPath, newPath);

			Assert.Equal(NuGetVersion.Parse(FormsV20Number1), diff.OldIdentity.Version);
			Assert.Equal(NuGetVersion.Parse(FormsV30Number2), diff.NewIdentity.Version);

			Assert.NotEmpty(diff.AddedFrameworks);
			Assert.NotEmpty(diff.RemovedFrameworks);
			Assert.NotEmpty(diff.UnchangedFrameworks);

			Assert.NotEmpty(diff.AddedAssemblies);
			Assert.NotEmpty(diff.RemovedAssemblies);
			Assert.NotEmpty(diff.UnchangedAssemblies);
		}

		[Fact]
		public async Task TestCompletePackageDiffIsGeneratedCorrectlyWithoutAllReferences()
		{
			var diffDir = GenerateTestOutputPath();

			var comparer = new NuGetDiff();
			comparer.IgnoreResolutionErrors = true;

			await comparer.SaveCompleteDiffToDirectoryAsync(FormsPackageId, FormsV25Number1, FormsV31Number1, diffDir);
		}

		[Fact]
		public async Task TestCompletePackageDiffThrowsWithoutAllReferencesAndFlag()
		{
			var diffDir = GenerateTestOutputPath();

			var comparer = new NuGetDiff();
			comparer.IgnoreResolutionErrors = false;

			var task = comparer.SaveCompleteDiffToDirectoryAsync(FormsPackageId, FormsV25Number1, FormsV31Number1, diffDir);
			await Assert.ThrowsAsync<AssemblyResolutionException>(() => task);
		}

		[Fact]
		public async Task TestCompleteSkiaPackageDiffIsGeneratedCorrectly()
		{
			var diffDir = GenerateTestOutputPath();

			var comparer = new NuGetDiff();
			comparer.SearchPaths.AddRange(searchPaths);

			await comparer.SaveCompleteDiffToDirectoryAsync(SkiaPackageId, SkiaV600Number1, SkiaV602Number1, diffDir);
		}

		[Fact]
		public async Task TestCompletePackageDiffIsGeneratedCorrectly()
		{
			var diffDir = GenerateTestOutputPath();

			var comparer = new NuGetDiff();
			comparer.SearchPaths.AddRange(searchPaths);

			// download extra dependencies
			await AddDependencyAsync(comparer, "Xamarin.Android.Support.v7.AppCompat", "25.4.0.2", "MonoAndroid70");
			await AddDependencyAsync(comparer, "Xamarin.Android.Support.Fragment", "25.4.0.2", "MonoAndroid70");
			await AddDependencyAsync(comparer, "Xamarin.Android.Support.Core.Utils", "25.4.0.2", "MonoAndroid70");
			await AddDependencyAsync(comparer, "Xamarin.Android.Support.Compat", "25.4.0.2", "MonoAndroid70");
			await AddDependencyAsync(comparer, "Xamarin.Android.Support.Core.UI", "25.4.0.2", "MonoAndroid70");
			await AddDependencyAsync(comparer, "Xamarin.Android.Support.v7.CardView", "25.4.0.2", "MonoAndroid70");
			await AddDependencyAsync(comparer, "Xamarin.Android.Support.Design", "25.4.0.2", "MonoAndroid70");

			await comparer.SaveCompleteDiffToDirectoryAsync(FormsPackageId, FormsV25Number1, FormsV31Number1, diffDir);
		}

		[Fact]
		public async Task TestCompletePackageDiffIsTheSameEvenWithoutReferences()
		{
			var missingDir = GenerateTestOutputPath();
			var allDir = GenerateTestOutputPath();

			// generate diff with missing references
			var missing = new NuGetDiff();
			missing.IgnoreResolutionErrors = true;
			missing.IgnoreInheritedInterfaces = true;
			missing.SaveAssemblyMarkdownDiff = true;
			await missing.SaveCompleteDiffToDirectoryAsync(FormsPackageId, FormsV25Number1, FormsV31Number1, missingDir);

			// generate diff with everything
			var all = new NuGetDiff();
			all.SearchPaths.AddRange(searchPaths);
			all.IgnoreInheritedInterfaces = true;
			all.SaveAssemblyMarkdownDiff = true;
			await AddDependencyAsync(all, "Xamarin.Android.Support.v7.AppCompat", "25.4.0.2", "MonoAndroid70");
			await AddDependencyAsync(all, "Xamarin.Android.Support.Fragment", "25.4.0.2", "MonoAndroid70");
			await AddDependencyAsync(all, "Xamarin.Android.Support.Core.Utils", "25.4.0.2", "MonoAndroid70");
			await AddDependencyAsync(all, "Xamarin.Android.Support.Compat", "25.4.0.2", "MonoAndroid70");
			await AddDependencyAsync(all, "Xamarin.Android.Support.Core.UI", "25.4.0.2", "MonoAndroid70");
			await AddDependencyAsync(all, "Xamarin.Android.Support.v7.CardView", "25.4.0.2", "MonoAndroid70");
			await AddDependencyAsync(all, "Xamarin.Android.Support.Design", "25.4.0.2", "MonoAndroid70");
			await all.SaveCompleteDiffToDirectoryAsync(FormsPackageId, FormsV25Number1, FormsV31Number1, allDir);

			// test the markdown files as the xml will be different as some dependency type will not be loaded
			// are there the same files
			var missingFiles = Directory.GetFiles(missingDir, "*.md", SearchOption.AllDirectories);
			var allFiles = Directory.GetFiles(allDir, "*.md", SearchOption.AllDirectories);
			var missingDic = missingFiles.ToDictionary(f => Path.GetRelativePath(missingDir, f), f => f);
			var allDic = allFiles.ToDictionary(f => Path.GetRelativePath(allDir, f), f => f);
			Assert.Equal(14, missingDic.Count);
			Assert.Equal(14, allDic.Count);
			Assert.Equal(allDic.Keys, missingDic.Keys);
			foreach (var pair in allDic)
			{
				var allFile = await File.ReadAllTextAsync(pair.Value);
				var missingFile = await File.ReadAllTextAsync(missingDic[pair.Key]);
				Assert.Equal(allFile, missingFile);
			}
		}

		private static string GenerateTestOutputPath()
		{
			var dir = Path.Combine(Path.GetTempPath(), "Mono.ApiTools.NuGetDiff");
			if (!Directory.Exists(dir))
				Directory.CreateDirectory(dir);
			return Path.Combine(dir, Path.GetRandomFileName());
		}

		private static IEnumerable<string> GetSearchPaths()
		{
			var paths = new List<string>();

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				var pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

				// find out where VS is installed with Xamarin
				var vswhere = Process.Start(new ProcessStartInfo
				{
					FileName = $@"{pf}\Microsoft Visual Studio\Installer\vswhere.exe",
					Arguments = "-requires Component.Xamarin -latest -property installationPath",
					RedirectStandardOutput = true
				});
				vswhere.WaitForExit();
				var vs = vswhere.StandardOutput.ReadLine();
				var referenceAssemblies = $@"{vs}\Common7\IDE\ReferenceAssemblies\Microsoft\Framework";

				paths.Add($@"{referenceAssemblies}\MonoTouch\v1.0");
				paths.Add($@"{referenceAssemblies}\MonoAndroid\v1.0");
				paths.Add($@"{referenceAssemblies}\MonoAndroid\v8.1");
				paths.Add($@"{referenceAssemblies}\Xamarin.iOS\v1.0");
				paths.Add($@"{referenceAssemblies}\Xamarin.TVOS\v1.0");
				paths.Add($@"{referenceAssemblies}\Xamarin.WatchOS\v1.0");
				paths.Add($@"{referenceAssemblies}\Xamarin.Mac\v2.0");
				paths.Add($@"{pf}\Windows Kits\10\References\10.0.17134.0\Windows.Foundation.UniversalApiContract\6.0.0.0");
				paths.Add($@"{pf}\Windows Kits\10\References\10.0.16299.0\Windows.Foundation.UniversalApiContract\5.0.0.0");
				paths.Add($@"{pf}\Windows Kits\10\References\10.0.15063.0\Windows.Foundation.UniversalApiContract\5.0.0.0");
				paths.Add($@"{pf}\Windows Kits\10\References\Windows.Foundation.UniversalApiContract\3.0.0.0");
				paths.Add($@"{pf}\Windows Kits\10\References\Windows.Foundation.UniversalApiContract\2.0.0.0");
				paths.Add($@"{pf}\Windows Kits\10\References\Windows.Foundation.UniversalApiContract\1.0.0.0");
				paths.Add($@"{pf}\Windows Kits\10\References\10.0.17134.0\Windows.Foundation.FoundationContract\6.0.0.0");
				paths.Add($@"{pf}\Windows Kits\10\References\10.0.16299.0\Windows.Foundation.FoundationContract\5.0.0.0");
				paths.Add($@"{pf}\Windows Kits\10\References\10.0.15063.0\Windows.Foundation.FoundationContract\5.0.0.0");
				paths.Add($@"{pf}\Windows Kits\10\References\Windows.Foundation.FoundationContract\3.0.0.0");
				paths.Add($@"{pf}\Windows Kits\10\References\Windows.Foundation.FoundationContract\2.0.0.0");
				paths.Add($@"{pf}\Windows Kits\10\References\Windows.Foundation.FoundationContract\1.0.0.0");
				paths.Add($@"{pf}\GtkSharp\2.12\lib");
				paths.Add($@"{vs}\Common7\IDE\PublicAssemblies");
			}
			else
			{
				// TODO
			}

			return paths;
		}

		private async Task AddDependencyAsync(NuGetDiff comparer, string id, string version, string platform)
		{
			await comparer.ExtractCachedPackageAsync(id, version);
			comparer.SearchPaths.Add(Path.Combine(comparer.GetCachedPackageDirectory(id, version), "lib", platform));
		}
	}
}
