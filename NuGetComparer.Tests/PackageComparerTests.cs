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

namespace NuGetComparer.Tests
{
	public class PackageComparerTests
	{
		private const string TestPackageId = "Xamarin.Forms";

		private const string TestV15Number1 = "1.5.1.6471";
		private const string TestV20Number1 = "2.0.0.6482";
		private const string TestV25Number1 = "2.5.0.280555";
		private const string TestV30Number1 = "3.0.0.550146";
		private const string TestV30Number2 = "3.0.0.561731";
		private const string TestV30Number3 = "3.0.0.446417";
		private const string TestV31Number1 = "3.1.0.697729";

		private const string TestV20Url1 = "https://www.nuget.org/api/v2/package/Xamarin.Forms/2.0.0.6482";
		private const string TestV30Url1 = "https://www.nuget.org/api/v2/package/Xamarin.Forms/3.0.0.550146";
		private const string TestV3Url2 = "https://www.nuget.org/api/v2/package/Xamarin.Forms/3.0.0.561731";

		private static readonly string[] searchPaths;

		static PackageComparerTests()
		{
			searchPaths = GetSearchPaths().ToArray();
		}

		[Fact]
		public async Task TestComparePackageWithSameAssemblies()
		{
			var comparer = new PackageComparer();
			comparer.SearchPaths.AddRange(searchPaths);

			var diff = await comparer.GeneratePackageDiffAsync(TestPackageId, TestV30Number1, TestV30Number2);

			Assert.Equal(NuGetVersion.Parse(TestV30Number1), diff.OldIdentity.Version);
			Assert.Equal(NuGetVersion.Parse(TestV30Number2), diff.NewIdentity.Version);

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
			var comparer = new PackageComparer();
			comparer.SearchPaths.AddRange(searchPaths);

			var diff = await comparer.GeneratePackageDiffAsync(TestPackageId, TestV20Number1, TestV30Number2);

			Assert.Equal(NuGetVersion.Parse(TestV20Number1), diff.OldIdentity.Version);
			Assert.Equal(NuGetVersion.Parse(TestV30Number2), diff.NewIdentity.Version);

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
			var comparer = new PackageComparer();
			comparer.SearchPaths.AddRange(searchPaths);

			var diff = await comparer.GeneratePackageDiffAsync(TestPackageId, TestV20Number1, TestV20Number1);

			Assert.Equal(NuGetVersion.Parse(TestV20Number1), diff.OldIdentity.Version);
			Assert.Equal(NuGetVersion.Parse(TestV20Number1), diff.NewIdentity.Version);

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
			var newPath = Path.Combine(Path.GetTempPath(), "NuGetComparer", Path.GetRandomFileName());

			using (var wc = new WebClient())
			{
				await wc.DownloadFileTaskAsync(TestV3Url2, newPath);
			}

			var comparer = new PackageComparer();
			comparer.SearchPaths.AddRange(searchPaths);

			var diff = await comparer.GeneratePackageDiffAsync(TestPackageId, TestV20Number1, new PackageArchiveReader(newPath));

			Assert.Equal(NuGetVersion.Parse(TestV20Number1), diff.OldIdentity.Version);
			Assert.Equal(NuGetVersion.Parse(TestV30Number2), diff.NewIdentity.Version);

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
			var oldPath = Path.Combine(Path.GetTempPath(), "NuGetComparer", Path.GetRandomFileName());
			var newPath = Path.Combine(Path.GetTempPath(), "NuGetComparer", Path.GetRandomFileName());

			using (var wc = new WebClient())
			{
				await wc.DownloadFileTaskAsync(TestV20Url1, oldPath);
				await wc.DownloadFileTaskAsync(TestV3Url2, newPath);
			}

			var comparer = new PackageComparer();
			comparer.SearchPaths.AddRange(searchPaths);

			var diff = await comparer.GeneratePackageDiffAsync(oldPath, newPath);

			Assert.Equal(NuGetVersion.Parse(TestV20Number1), diff.OldIdentity.Version);
			Assert.Equal(NuGetVersion.Parse(TestV30Number2), diff.NewIdentity.Version);

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
			var diffDir = Path.Combine(Path.GetTempPath(), "NuGetComparer", Path.GetRandomFileName());

			var comparer = new PackageComparer();
			comparer.IgnoreResolutionErrors = true;

			await comparer.SaveCompletePackageDiffToDirectoryAsync(TestPackageId, TestV25Number1, TestV31Number1, diffDir);
		}

		[Fact]
		public async Task TestCompletePackageDiffThrowsWithoutAllReferencesAndFlag()
		{
			var diffDir = Path.Combine(Path.GetTempPath(), "NuGetComparer", Path.GetRandomFileName());

			var comparer = new PackageComparer();
			comparer.IgnoreResolutionErrors = false;

			var task = comparer.SaveCompletePackageDiffToDirectoryAsync(TestPackageId, TestV25Number1, TestV31Number1, diffDir);
			await Assert.ThrowsAsync<AssemblyResolutionException>(() => task);
		}

		[Fact]
		public async Task TestCompletePackageDiffIsGeneratedCorrectly()
		{
			var diffDir = Path.Combine(Path.GetTempPath(), "NuGetComparer", Path.GetRandomFileName());

			var comparer = new PackageComparer();
			comparer.SearchPaths.AddRange(searchPaths);

			// download extra dependencies
			await AddDependencyAsync(comparer, "Xamarin.Android.Support.v7.AppCompat", "25.4.0.2", "MonoAndroid70");
			await AddDependencyAsync(comparer, "Xamarin.Android.Support.Fragment", "25.4.0.2", "MonoAndroid70");
			await AddDependencyAsync(comparer, "Xamarin.Android.Support.Core.Utils", "25.4.0.2", "MonoAndroid70");
			await AddDependencyAsync(comparer, "Xamarin.Android.Support.Compat", "25.4.0.2", "MonoAndroid70");
			await AddDependencyAsync(comparer, "Xamarin.Android.Support.Core.UI", "25.4.0.2", "MonoAndroid70");
			await AddDependencyAsync(comparer, "Xamarin.Android.Support.v7.CardView", "25.4.0.2", "MonoAndroid70");
			await AddDependencyAsync(comparer, "Xamarin.Android.Support.Design", "25.4.0.2", "MonoAndroid70");

			await comparer.SaveCompletePackageDiffToDirectoryAsync(TestPackageId, TestV25Number1, TestV31Number1, diffDir);
		}

		[Fact]
		public async Task TestCompletePackageDiffIsTheSameEvenWithoutReferences()
		{
			var missingDir = Path.Combine(Path.GetTempPath(), "NuGetComparer", Path.GetRandomFileName());
			var allDir = Path.Combine(Path.GetTempPath(), "NuGetComparer", Path.GetRandomFileName());

			// generate diff with missing references
			var missing = new PackageComparer();
			missing.IgnoreResolutionErrors = true;
			missing.IgnoreInheritedInterfaces = true;
			missing.SaveAssemblyMarkdownDiff = true;
			await missing.SaveCompletePackageDiffToDirectoryAsync(TestPackageId, TestV25Number1, TestV31Number1, missingDir);

			// generate diff with everything
			var all = new PackageComparer();
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
			await all.SaveCompletePackageDiffToDirectoryAsync(TestPackageId, TestV25Number1, TestV31Number1, allDir);

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
				//paths.AddRange($@"{NUGET_PACKAGES}/xamarin.forms/{GetVersion("Xamarin.Forms", "release")}/lib/*");
				//paths.AddRange($@"{NUGET_PACKAGES}/tizen.net/{GetVersion("Tizen.NET", "release")}/lib/*");
				//paths.AddRange($@"{NUGET_PACKAGES}/opentk.glcontrol/{GetVersion("OpenTK.GLControl", "release")}/lib/*");
			}
			else
			{
				// TODO
			}

			return paths;
		}

		private async Task AddDependencyAsync(PackageComparer comparer, string id, string version, string platform)
		{
			await comparer.ExtractCachedPackageAsync(id, version);
			comparer.SearchPaths.Add(Path.Combine(comparer.GetCachedPackageDirectory(id, version), "lib", platform));
		}
	}
}
