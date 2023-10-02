using Mono.Cecil;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
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
using System.Xml.Linq;
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
		private const string SkiaV560Number = "1.56.0";
		private const string SkiaV561Number = "1.56.1";
		private const string SkiaV600Number = "1.60.0";
		private const string SkiaV601Number = "1.60.1";
		private const string SkiaV602Number = "1.60.2";

		private const string AndroidSupportPackageId = "Xamarin.Android.Support.Compat";
		private const string AndroidSupportV28Number = "28.0.0-preview5";
		private const string AndroidSupportV27Number = "27.0.2.1";

		private static readonly string[] searchPaths;

		static PackageComparerTests()
		{
			searchPaths = GetSearchPaths().ToArray();
		}

		[Fact]
		public async Task TestComparePackageWithNoOldVersion()
		{
			var comparer = new NuGetDiff();
			comparer.SearchPaths.AddRange(searchPaths);

			var diff = await comparer.GenerateAsync(FormsPackageId, null, FormsV30Number2);

			Assert.Null(diff.OldIdentity);
			Assert.Equal(NuGetVersion.Parse(FormsV30Number2), diff.NewIdentity.Version);

			Assert.NotEmpty(diff.AddedFrameworks);
			Assert.Empty(diff.RemovedFrameworks);
			Assert.Empty(diff.UnchangedFrameworks);

			Assert.NotEmpty(diff.AddedAssemblies);
			Assert.Empty(diff.RemovedAssemblies);
			Assert.Empty(diff.UnchangedAssemblies);
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
		public async Task TestComparePackageStructureAndMetadata()
		{
			var comparer = new NuGetDiff();
			comparer.SearchPaths.AddRange(searchPaths);
			comparer.SaveNuGetStructureDiff = true;
			comparer.IgnoreResolutionErrors = true;

			// Ensure diff is producing results
			var diff = await comparer.GenerateAsync(FormsPackageId, FormsV20Number1, FormsV30Number2);

			Assert.NotEmpty(diff.AddedFiles);
			Assert.NotEmpty(diff.RemovedFiles);
			Assert.NotEmpty(diff.MetadataDiff);

			// Check output markdown file
			var oldPackage = new PackageIdentity(FormsPackageId, NuGetVersion.Parse(FormsV20Number1));
			var newPackage = new PackageIdentity(FormsPackageId, NuGetVersion.Parse(FormsV30Number2));
			var tempOutput = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

			await comparer.SaveCompleteDiffToDirectoryAsync(oldPackage, newPackage, tempOutput);

			var results = await File.ReadAllLinesAsync(Path.Combine(tempOutput, "nuget-diff.md"));

			// Spot check a few expected lines
			Assert.Contains("### Changed Metadata", results);
			Assert.Contains("- <authors>Xamarin Inc.</authors>", results);
			Assert.Contains("+ <authors>Microsoft</authors>", results);
			Assert.Contains("### Added/Removed File(s)", results);
			Assert.Contains("- lib/portable-win+net45+wp80+win81+wpa81+MonoAndroid10+MonoTouch10+Xamarin.iOS10/Xamarin.Forms.Core.dll", results);
			Assert.Contains("+ lib/netstandard2.0/Xamarin.Forms.Core.dll", results);

			try
			{
				// Try to delete temp dir, but don't error if it fails
				Directory.Delete(tempOutput, true);
			} catch { }
		}

		[Fact]
		public async Task TestCompletePackageDiffIsGeneratedCorrectlyWithoutAllReferencesAndNoOldVersion()
		{
			var diffDir = GenerateTestOutputPath();

			var comparer = new NuGetDiff();
			comparer.IgnoreResolutionErrors = true;

			await comparer.SaveCompleteDiffToDirectoryAsync(FormsPackageId, null, FormsV31Number1, diffDir);
		}

		[Fact]
		public async Task TestCompletePackageDiffIsGeneratedCorrectlyWithAddedAssembliesButWithoutAllReferences()
		{
			var diffDir = GenerateTestOutputPath();

			var comparer = new NuGetDiff();
			comparer.IgnoreResolutionErrors = true;

			await comparer.SaveCompleteDiffToDirectoryAsync(FormsPackageId, FormsV25Number1, FormsV31Number1, diffDir);
		}

		[Fact]
		public async Task TestCompletePackageDiffIsGeneratedCorrectlyWithoutAllReferences()
		{
			var diffDir = GenerateTestOutputPath();

			var comparer = new NuGetDiff();
			comparer.IgnoreResolutionErrors = true;
			comparer.IgnoreAddedAssemblies = true;

			await comparer.SaveCompleteDiffToDirectoryAsync(FormsPackageId, FormsV25Number1, FormsV31Number1, diffDir);
		}

		[Fact]
		public async Task TestCompletePackageDiffIsGeneratedCorrectlyWithoutAllReferencesAndIgnoreNonBreaking()
		{
			var breakingDir = GenerateTestOutputPath();
			var ignoreBreakingDir = GenerateTestOutputPath();

			var comparer = new NuGetDiff();
			comparer.IgnoreResolutionErrors = true;
			comparer.IgnoreAddedAssemblies = true;
			comparer.SaveAssemblyMarkdownDiff = true;

			comparer.IgnoreNonBreakingChanges = false;
			await comparer.SaveCompleteDiffToDirectoryAsync(FormsPackageId, FormsV25Number1, FormsV31Number1, breakingDir);

			comparer.IgnoreNonBreakingChanges = true;
			await comparer.SaveCompleteDiffToDirectoryAsync(FormsPackageId, FormsV25Number1, FormsV31Number1, ignoreBreakingDir);
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
			comparer.SaveAssemblyMarkdownDiff = true;
			comparer.SearchPaths.AddRange(searchPaths);

			await comparer.SaveCompleteDiffToDirectoryAsync(SkiaPackageId, SkiaV600Number, SkiaV602Number, diffDir);
		}

		[Fact]
		public async Task TestMatchSameFrameworkButDifferentVersionFailsWhenSimilarIsDisabled()
		{
			var diffDir = GenerateTestOutputPath();

			var comparer = new NuGetDiff();
			comparer.SearchPaths.AddRange(searchPaths);
			comparer.SaveAssemblyMarkdownDiff = true;
			comparer.IgnoreSimilarFrameworks = true;

			await comparer.SaveCompleteDiffToDirectoryAsync(SkiaPackageId, SkiaV600Number, SkiaV601Number, diffDir);

			var xnupkg = XDocument.Load(Path.Combine(diffDir, "SkiaSharp.nupkg.diff.xml"));
			var xFws = xnupkg.Root
				.Element("package")
				.Element("frameworks")
				.Elements("framework");

			var xMacFws = xFws.Where(f => f.Attribute("name").Value == "Xamarin.Mac").ToArray();
			Assert.Equal(2, xMacFws.Length);

			var xMac0Fw = xMacFws.Single(f => f.Attribute("version").Value == "0.0.0.0");
			Assert.Equal("missing", xMac0Fw.Attribute("presence").Value);
			Assert.Empty(xMac0Fw.Descendants());

			var xMac2Fw = xMacFws.Single(f => f.Attribute("version").Value == "2.0.0.0");
			Assert.Equal("extra", xMac2Fw.Attribute("presence").Value);

			var xMac2Ass = xMac2Fw.Element("assemblies").Element("assembly");
			Assert.Equal("lib/Xamarin.Mac20/SkiaSharp.dll", xMac2Ass.Attribute("path").Value);
			Assert.Null(xMac2Ass.Attribute("old_path"));

			var netStdFile = File.ReadAllText(Path.Combine(diffDir, "netstandard1.3", "SkiaSharp.dll.diff.md"));
			var mac2File = File.ReadAllText(Path.Combine(diffDir, "Xamarin.Mac20", "SkiaSharp.dll.diff.md"));
			Assert.NotEqual(netStdFile, mac2File);
			Assert.True(netStdFile.Length < mac2File.Length);
		}

		[Fact]
		public async Task TestMatchSameFrameworkButDifferentVersionWhenSimilarIsEnabled()
		{
			var diffDir = GenerateTestOutputPath();

			var comparer = new NuGetDiff();
			comparer.SearchPaths.AddRange(searchPaths);
			comparer.SaveAssemblyMarkdownDiff = true;

			await comparer.SaveCompleteDiffToDirectoryAsync(SkiaPackageId, SkiaV600Number, SkiaV601Number, diffDir);

			var xnupkg = XDocument.Load(Path.Combine(diffDir, "SkiaSharp.nupkg.diff.xml"));
			var xFws = xnupkg.Root
				.Element("package")
				.Element("frameworks")
				.Elements("framework");

			var xMacFws = xFws.Where(f => f.Attribute("name").Value == "Xamarin.Mac").ToArray();
			Assert.Equal(2, xMacFws.Length);

			var xMac0Fw = xMacFws.Single(f => f.Attribute("version").Value == "0.0.0.0");
			Assert.Equal("missing", xMac0Fw.Attribute("presence").Value);
			Assert.Empty(xMac0Fw.Descendants());

			var xMac2Fw = xMacFws.Single(f => f.Attribute("version").Value == "2.0.0.0");
			Assert.Equal("extra", xMac2Fw.Attribute("presence").Value);

			var xMac2Ass = xMac2Fw.Element("assemblies").Element("assembly");
			Assert.Equal("lib/Xamarin.Mac20/SkiaSharp.dll", xMac2Ass.Attribute("path").Value);
			Assert.Equal("lib/XamarinMac/SkiaSharp.dll", xMac2Ass.Attribute("old_path").Value);

			var netStdFile = File.ReadAllText(Path.Combine(diffDir, "netstandard1.3", "SkiaSharp.dll.diff.md"));
			var mac2File = File.ReadAllText(Path.Combine(diffDir, "Xamarin.Mac20", "SkiaSharp.dll.diff.md"));
			Assert.Equal(netStdFile, mac2File);
		}

		[Fact]
		public async Task TestMatchSameFrameworkAndVersionButDifferentSpelling()
		{
			var diffDir = GenerateTestOutputPath();

			var comparer = new NuGetDiff();
			comparer.SearchPaths.AddRange(searchPaths);
			comparer.SaveAssemblyMarkdownDiff = true;
			comparer.IgnoreSimilarFrameworks = true;

			await comparer.SaveCompleteDiffToDirectoryAsync(SkiaPackageId, SkiaV600Number, SkiaV601Number, diffDir);

			var xnupkg = XDocument.Load(Path.Combine(diffDir, "SkiaSharp.nupkg.diff.xml"));
			var xFws = xnupkg.Root
				.Element("package")
				.Element("frameworks")
				.Elements("framework");

			var xIosFws = xFws.Single(f => f.Attribute("name").Value == "Xamarin.iOS");
			Assert.Null(xIosFws.Attribute("presence"));
			var xIosAss = xIosFws.Element("assemblies").Element("assembly");
			Assert.Equal("lib/Xamarin.iOS/SkiaSharp.dll", xIosAss.Attribute("path").Value);
			Assert.Equal("lib/XamariniOS/SkiaSharp.dll", xIosAss.Attribute("old_path").Value);
			var netStdFile = File.ReadAllText(Path.Combine(diffDir, "netstandard1.3", "SkiaSharp.dll.diff.md"));
			var iosFile = File.ReadAllText(Path.Combine(diffDir, "Xamarin.iOS", "SkiaSharp.dll.diff.md"));
			Assert.Equal(netStdFile, iosFile);
		}

		[Fact]
		public async Task TestCompleteMatchNetStandardPortableReusePortable()
		{
			var diffDir = GenerateTestOutputPath();

			var comparer = new NuGetDiff();
			comparer.SearchPaths.AddRange(searchPaths);
			comparer.SaveAssemblyMarkdownDiff = true;

			await comparer.SaveCompleteDiffToDirectoryAsync(SkiaPackageId, SkiaV560Number, SkiaV600Number, diffDir);

			var xnupkg = XDocument.Load(Path.Combine(diffDir, "SkiaSharp.nupkg.diff.xml"));
			var xFws = xnupkg.Root
				.Element("package")
				.Element("frameworks")
				.Elements("framework");

			var xPclFw = xFws.Single(f => f.Attribute("name").Value == ".NETPortable");
			Assert.Null(xPclFw.Attribute("presence"));

			var xStdFw = xFws.Single(f => f.Attribute("name").Value == ".NETStandard");
			var xStdAss = xStdFw.Element("assemblies").Element("assembly");
			Assert.Equal("lib/netstandard1.3/SkiaSharp.dll", xStdAss.Attribute("path").Value);
			Assert.Equal("lib/portable-net45+win8+wpa81+wp8/SkiaSharp.dll", xStdAss.Attribute("old_path").Value);
			var netStdFile = File.ReadAllText(Path.Combine(diffDir, "netstandard1.3", "SkiaSharp.dll.diff.md"));
			var netFile = File.ReadAllText(Path.Combine(diffDir, "portable-net45+win8+wpa81+wp8", "SkiaSharp.dll.diff.md"));
			Assert.Equal(netStdFile, netFile);
		}

		[Fact]
		public async Task TestMatchPortableUpgradeToNetStandard()
		{
			var diffDir = GenerateTestOutputPath();

			var comparer = new NuGetDiff();
			comparer.SearchPaths.AddRange(searchPaths);
			comparer.SaveAssemblyMarkdownDiff = true;

			await comparer.SaveCompleteDiffToDirectoryAsync(SkiaPackageId, SkiaV560Number, SkiaV601Number, diffDir);

			var xnupkg = XDocument.Load(Path.Combine(diffDir, "SkiaSharp.nupkg.diff.xml"));
			var xFws = xnupkg.Root
				.Element("package")
				.Element("frameworks")
				.Elements("framework");

			var xPclFw = xFws.Single(f => f.Attribute("name").Value == ".NETPortable");
			Assert.Equal("missing", xPclFw.Attribute("presence").Value);
			Assert.Empty(xPclFw.Descendants());

			var xStdFw = xFws.Single(f => f.Attribute("name").Value == ".NETStandard");
			var xStdAss = xStdFw.Element("assemblies").Element("assembly");
			Assert.Equal("lib/netstandard1.3/SkiaSharp.dll", xStdAss.Attribute("path").Value);
			Assert.Equal("lib/portable-net45+win8+wpa81+wp8/SkiaSharp.dll", xStdAss.Attribute("old_path").Value);
			var netStdFile = File.ReadAllText(Path.Combine(diffDir, "netstandard1.3", "SkiaSharp.dll.diff.md"));
			var netFile = File.ReadAllText(Path.Combine(diffDir, "net45", "SkiaSharp.dll.diff.md"));

			Assert.Equal(netStdFile, netFile);
		}

		[Fact]
		public async Task TestMatchFramework()
		{
			var comparer = new NuGetDiff();
			comparer.SearchPaths.AddRange(searchPaths);

			var diff = await comparer.GenerateAsync(SkiaPackageId, SkiaV600Number, SkiaV601Number);

			Assert.Equal(2, diff.AddedFrameworks.Length);
			Assert.Equal(2, diff.RemovedFrameworks.Length);
			Assert.Equal(7, diff.UnchangedFrameworks.Length);
			Assert.Equal(2, diff.SimilarFrameworks.Count);

			var mac2 = NuGetFramework.Parse("Xamarin.Mac,Version=v2.0");
			var mac0 = NuGetFramework.Parse("Xamarin.Mac,Version=v0.0");
			Assert.Contains(mac2, diff.SimilarFrameworks.Keys);
			Assert.Equal(mac0, diff.SimilarFrameworks[mac2]);

			const string mac2Dll = "lib/Xamarin.Mac20/SkiaSharp.dll";
			const string mac0Dll = "lib/XamarinMac/SkiaSharp.dll";
			Assert.Contains(mac2, diff.SimilarAssemblies.Keys);
			Assert.Contains((mac2Dll, mac0Dll), diff.SimilarAssemblies[mac2]);
		}

		[Fact]
		public async Task TestMatchFrameworksWithAllFrameworks()
		{
			var diffDir = GenerateTestOutputPath();

			var comparer = new NuGetDiff();
			comparer.SearchPaths.AddRange(searchPaths);
			comparer.IgnoreResolutionErrors = true;
			comparer.SaveAssemblyApiInfo = true;
			comparer.SaveAssemblyXmlDiff = true;
			comparer.SaveAssemblyMarkdownDiff = true;

			await comparer.SaveCompleteDiffToDirectoryAsync(AndroidSupportPackageId, AndroidSupportV27Number, AndroidSupportV28Number, diffDir);
		}

		[Fact]
		public async Task TestMatchNetStandardPortableReusePortable()
		{
			var comparer = new NuGetDiff();
			comparer.SearchPaths.AddRange(searchPaths);

			var diff = await comparer.GenerateAsync(SkiaPackageId, SkiaV560Number, SkiaV600Number);

			Assert.Equal(2, diff.AddedFrameworks.Length);
			Assert.Empty(diff.RemovedFrameworks);
			Assert.Equal(7, diff.UnchangedFrameworks.Length);
			Assert.Equal(2, diff.SimilarFrameworks.Count);

			var netstd = NuGetFramework.Parse(".NETStandard,Version=v1.3");
			var pcl = NuGetFramework.Parse(".NETPortable,Version=v0.0,Profile=Profile259");
			Assert.Contains(netstd, diff.SimilarFrameworks.Keys);
			Assert.Equal(pcl, diff.SimilarFrameworks[netstd]);

			const string netstdDll = "lib/netstandard1.3/SkiaSharp.dll";
			const string pclDll = "lib/portable-net45+win8+wpa81+wp8/SkiaSharp.dll";
			Assert.Contains(netstd, diff.SimilarAssemblies.Keys);
			Assert.Contains((netstdDll, pclDll), diff.SimilarAssemblies[netstd]);
		}

		[Fact]
		public async Task TestMatchNetStandardPortable()
		{
			var comparer = new NuGetDiff();
			comparer.SearchPaths.AddRange(searchPaths);

			var diff = await comparer.GenerateAsync(SkiaPackageId, SkiaV560Number, SkiaV601Number);

			Assert.Equal(4, diff.AddedFrameworks.Length);
			Assert.Equal(2, diff.RemovedFrameworks.Length);
			Assert.Equal(5, diff.UnchangedFrameworks.Length);
			Assert.Equal(3, diff.SimilarFrameworks.Count);

			var mac2 = NuGetFramework.Parse("Xamarin.Mac,Version=v2.0");
			var mac0 = NuGetFramework.Parse("Xamarin.Mac,Version=v0.0");
			Assert.Contains(mac2, diff.SimilarFrameworks.Keys);
			Assert.Equal(mac0, diff.SimilarFrameworks[mac2]);

			var netstd = NuGetFramework.Parse(".NETStandard,Version=v1.3");
			var pcl = NuGetFramework.Parse(".NETPortable,Version=v0.0,Profile=Profile259");
			Assert.Contains(netstd, diff.SimilarFrameworks.Keys);
			Assert.Equal(pcl, diff.SimilarFrameworks[netstd]);

			const string mac2Dll = "lib/Xamarin.Mac20/SkiaSharp.dll";
			const string mac0Dll = "lib/XamarinMac/SkiaSharp.dll";
			Assert.Contains(mac2, diff.SimilarAssemblies.Keys);
			Assert.Contains((mac2Dll, mac0Dll), diff.SimilarAssemblies[mac2]);

			const string netstdDll = "lib/netstandard1.3/SkiaSharp.dll";
			const string pclDll = "lib/portable-net45+win8+wpa81+wp8/SkiaSharp.dll";
			Assert.Contains(netstd, diff.SimilarAssemblies.Keys);
			Assert.Contains((netstdDll, pclDll), diff.SimilarAssemblies[netstd]);
		}

		[Fact]
		public async Task TestCompletePackageDiffIsGeneratedCorrectly()
		{
			var diffDir = GenerateTestOutputPath();

			var comparer = new NuGetDiff();
			comparer.SearchPaths.AddRange(searchPaths);
			comparer.SaveAssemblyMarkdownDiff = true;
			comparer.IgnoreSimilarFrameworks = true;

			// download extra dependencies
			await AddDependencyAsync(comparer, "Xamarin.Android.Support.v7.AppCompat", "25.4.0.2", "MonoAndroid70");
			await AddDependencyAsync(comparer, "Xamarin.Android.Support.Fragment", "25.4.0.2", "MonoAndroid70");
			await AddDependencyAsync(comparer, "Xamarin.Android.Support.Core.Utils", "25.4.0.2", "MonoAndroid70");
			await AddDependencyAsync(comparer, "Xamarin.Android.Support.Compat", "25.4.0.2", "MonoAndroid70");
			await AddDependencyAsync(comparer, "Xamarin.Android.Support.Core.UI", "25.4.0.2", "MonoAndroid70");
			await AddDependencyAsync(comparer, "Xamarin.Android.Support.v7.CardView", "25.4.0.2", "MonoAndroid70");
			await AddDependencyAsync(comparer, "Xamarin.Android.Support.Design", "25.4.0.2", "MonoAndroid70");
			await AddDependencyAsync(comparer, "Tizen.NET", "4.0.0", "netstandard2.0");

			await comparer.SaveCompleteDiffToDirectoryAsync(FormsPackageId, FormsV25Number1, FormsV31Number1, diffDir);

			var files = Directory.GetFiles(diffDir, "*.md", SearchOption.AllDirectories);
			Assert.Equal(27, files.Length);
		}

		[Fact]
		public async Task TestCompletePackageDiffIsGeneratedCorrectlyWithAllAssemblyInfo()
		{
			var diffDir = GenerateTestOutputPath();

			var comparer = new NuGetDiff();
			comparer.SearchPaths.AddRange(searchPaths);
			comparer.SaveAssemblyApiInfo = true;
			comparer.SaveAssemblyHtmlDiff = true;
			comparer.SaveAssemblyMarkdownDiff = true;
			comparer.SaveAssemblyXmlDiff = true;

			// download extra dependencies
			await AddDependencyAsync(comparer, "Xamarin.Android.Support.v7.AppCompat", "25.4.0.2", "MonoAndroid70");
			await AddDependencyAsync(comparer, "Xamarin.Android.Support.Fragment", "25.4.0.2", "MonoAndroid70");
			await AddDependencyAsync(comparer, "Xamarin.Android.Support.Core.Utils", "25.4.0.2", "MonoAndroid70");
			await AddDependencyAsync(comparer, "Xamarin.Android.Support.Compat", "25.4.0.2", "MonoAndroid70");
			await AddDependencyAsync(comparer, "Xamarin.Android.Support.Core.UI", "25.4.0.2", "MonoAndroid70");
			await AddDependencyAsync(comparer, "Xamarin.Android.Support.v7.CardView", "25.4.0.2", "MonoAndroid70");
			await AddDependencyAsync(comparer, "Xamarin.Android.Support.Design", "25.4.0.2", "MonoAndroid70");
			await AddDependencyAsync(comparer, "Tizen.NET", "4.0.0", "netstandard2.0");

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
			missing.IgnoreAddedAssemblies = true;
			missing.IgnoreSimilarFrameworks = true;
			await missing.SaveCompleteDiffToDirectoryAsync(FormsPackageId, FormsV25Number1, FormsV31Number1, missingDir);

			// generate diff with everything
			var all = new NuGetDiff();
			all.SearchPaths.AddRange(searchPaths);
			all.IgnoreInheritedInterfaces = true;
			all.SaveAssemblyMarkdownDiff = true;
			all.IgnoreAddedAssemblies = true;
			all.IgnoreSimilarFrameworks = true;
			await AddDependencyAsync(all, "Xamarin.Android.Support.v7.AppCompat", "25.4.0.2", "MonoAndroid70");
			await AddDependencyAsync(all, "Xamarin.Android.Support.Fragment", "25.4.0.2", "MonoAndroid70");
			await AddDependencyAsync(all, "Xamarin.Android.Support.Core.Utils", "25.4.0.2", "MonoAndroid70");
			await AddDependencyAsync(all, "Xamarin.Android.Support.Compat", "25.4.0.2", "MonoAndroid70");
			await AddDependencyAsync(all, "Xamarin.Android.Support.Core.UI", "25.4.0.2", "MonoAndroid70");
			await AddDependencyAsync(all, "Xamarin.Android.Support.v7.CardView", "25.4.0.2", "MonoAndroid70");
			await AddDependencyAsync(all, "Xamarin.Android.Support.Design", "25.4.0.2", "MonoAndroid70");
			await AddDependencyAsync(all, "Tizen.NET", "4.0.0", "netstandard2.0");
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

		[Fact]
		public async Task TestDoesIgnoreMembers()
		{
			var path = GenerateTestOutputPath();

			// generate diff with missing references
			var missing = new NuGetDiff
			{
				IgnoreResolutionErrors = true,
				IgnoreInheritedInterfaces = true,
				SaveAssemblyMarkdownDiff = true,
				IgnoreAddedAssemblies = true,
				IgnoreSimilarFrameworks = true,
				IgnoreMemberRegex =
				{
					@"\.IServiceProviderExtensions\:",
				}
			};

			await missing.SaveCompleteDiffToDirectoryAsync(FormsPackageId, FormsV25Number1, FormsV31Number1, path);

			var allFile = await File.ReadAllTextAsync(Path.Combine(path, "MonoAndroid10/Xamarin.Forms.Core.dll.diff.md"));

			Assert.DoesNotContain("IServiceProviderExtensions", allFile);
		}

		[Theory]
		[InlineData(".NETPortable", ".NETPortable", new[] { ".NETPortable" })]
		[InlineData(".NETStandard", ".NETPortable", new[] { ".NETStandard" })]
		[InlineData(".NETPortable", ".NETStandard", new[] { ".NETPortable" })]
		[InlineData(".NETStandard", ".NETStandard", new[] { ".NETPortable", ".NETStandard" })]
		[InlineData(".NETPortable", ".NETPortable", new[] { ".NETPortable", ".NETStandard" })]
		public void TryMatchFrameworkMatchesBest(string expected, string source, string[] choices)
		{
			var src = new NuGetFramework(source);
			var chs = choices.Select(c => new NuGetFramework(c)).ToArray();
			var exp = new NuGetFramework(expected);

			var match = NuGetDiff.TryMatchFramework(src, chs);

			Assert.Equal(exp, match);
		}

		[Theory]
		[InlineData("net45", "net45", new[] { "net45" })]
		[InlineData("net462", "net462", new[] { "net45", "net462" })]
		[InlineData("net461", "net462", new[] { "net461", "net45" })]
		[InlineData("net461", "net462", new[] { "net45", "net461" })]
		[InlineData("net45", "net462", new[] { "net45" })]
		[InlineData("net5.0", "net5.0", new[] { "net5.0" })]
		[InlineData("net5.0", "net6.0", new[] { "net5.0" })]
		[InlineData("net5.0-ios", "net5.0-ios", new[] { "net5.0-ios" })]
		[InlineData("net5.0-ios", "net6.0-ios", new[] { "net5.0-ios" })]
		[InlineData("net6.0-ios13.4", "net6.0-ios14.0", new[] { "net6.0-ios13.4" })]
		[InlineData("net6.0-android30.0", "net7.0-android33.0", new[] { "net6.0-ios13.6", "net6.0-android30.0", "net6.0-maccatalyst13.5" })]
		[InlineData("net6.0-android30.0", "net7.0-android33.0", new[] { "net7.0-ios15.4", "net6.0-maccatalyst13.5", "net6.0-android30.0" })]
		[InlineData("net6.0-maccatalyst13.5", "net7.0-maccatalyst13.5", new[] { "net7.0-ios15.4", "net6.0-maccatalyst13.5", "net6.0-android30.0" })]
		[InlineData("net6.0-android30.0", "net7.0-android33.0", new[] { "net6.0-android30.0", "monoandroid1.0" })]
		[InlineData("net6.0-android32.0", "net7.0-android33.0", new[] { "net6.0-android30.0", "net6.0-android31.0", "net6.0-android32.0", "monoandroid1.0" })]
		[InlineData("net6.0-android32.0", "net7.0-android33.0", new[] { "net6.0-android31.0", "net6.0-android30.0", "monoandroid1.0", "net6.0-android32.0" })]
		[InlineData("net6.0-android32.0", "net7.0-android33.0", new[] { "monoandroid1.0", "net6.0-android32.0", "net6.0-android30.0", "net6.0-android31.0" })]
		[InlineData("monoandroid1.0", "net6.0-android31.0", new[] { "monoandroid1.0" })]
		[InlineData("net6.0-android31.0", "net6.0-android32.0", new[] { "net6.0-android31.0", "monoandroid1.0" })]
		[InlineData("net6.0-android31.0", "monoandroid1.0", new[] { "net6.0-android31.0" })]
		[InlineData("Xamarin.tvOS1.0", "net7.0-tvos15.0", new[] { "Xamarin.tvOS1.0" })]
		[InlineData("net6.0-tvos14.0", "net7.0-tvos15.0", new[] { "net6.0-tvos14.0", "Xamarin.tvOS1.0" })]
		[InlineData("net6.0-tvos14.0", "Xamarin.tvOS1.0", new[] { "net6.0-tvos14.0" })]
		[InlineData("net6.0-tvos14.0", "net7.0-tvos15.0", new[] { "net6.0-tvos13.0", "net6.0-tvos13.1", "net6.0-tvos14.0", "Xamarin.tvOS1.0" })]
		[InlineData("net6.0-tvos14.0", "net7.0-tvos15.0", new[] { "net6.0-tvos13.1", "net6.0-tvos13.0", "Xamarin.tvOS1.0", "net6.0-tvos14.0" })]
		[InlineData("net6.0-tvos14.0", "net7.0-tvos15.0", new[] { "Xamarin.tvOS1.0", "net6.0-tvos14.0", "net6.0-tvos13.0", "net6.0-tvos13.1" })]
		[InlineData("Xamarin.ios1.0", "net7.0-ios15.0", new[] { "Xamarin.ios1.0" })]
		[InlineData("net6.0-ios14.0", "net7.0-ios15.0", new[] { "net6.0-ios14.0", "Xamarin.ios1.0" })]
		[InlineData("net6.0-ios14.0", "Xamarin.ios1.0", new[] { "net6.0-ios14.0" })]
		[InlineData("net6.0-ios14.0", "net7.0-ios15.0", new[] { "net6.0-ios13.0", "net6.0-ios13.1", "net6.0-ios14.0", "Xamarin.ios1.0" })]
		[InlineData("net6.0-ios14.0", "net7.0-ios15.0", new[] { "net6.0-ios13.1", "net6.0-ios13.0", "Xamarin.ios1.0", "net6.0-ios14.0" })]
		[InlineData("net6.0-ios14.0", "net7.0-ios15.0", new[] { "Xamarin.ios1.0", "net6.0-ios14.0", "net6.0-ios13.0", "net6.0-ios13.1" })]
		public void TryMatchFrameworkMatchesBestParsed(string expected, string source, string[] choices)
		{
			var src = NuGetFramework.Parse(source);
			var chs = choices.Select(c => NuGetFramework.Parse(c)).ToArray();
			var exp = NuGetFramework.Parse(expected);

			var match = NuGetDiff.TryMatchFramework(src, chs);

			Assert.Equal(exp, match);
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
