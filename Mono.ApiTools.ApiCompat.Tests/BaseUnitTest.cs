namespace Mono.ApiTools.Tests;

public class BaseUnitTest
{
#if DEBUG
	const string CONFIGURATION = "Debug";
#else
	const string CONFIGURATION = "Release";
#endif

	public const string LibraryBuiltAgainstV1 = "../../../../TestAssemblies/LibraryBuiltAgainstV1/bin/" + CONFIGURATION + "/net8.0/LibraryBuiltAgainstV1.dll";

	public const string ClassLibraryV1 = "../../../../TestAssemblies/ClassLibraryV1/bin/" + CONFIGURATION + "/net8.0/ClassLibrary.dll";
	public const string ClassLibraryV2 = "../../../../TestAssemblies/ClassLibraryV2/bin/" + CONFIGURATION + "/net8.0/ClassLibrary.dll";

	public const string SkiaSharpV2Downloader = "../../../../TestAssemblies/SkiaSharpV2Downloader/bin/" + CONFIGURATION + "/net8.0/SkiaSharpV2Downloader.dll";
	public const string SkiaSharpV3Downloader = "../../../../TestAssemblies/SkiaSharpV3Downloader/bin/" + CONFIGURATION + "/net8.0/SkiaSharpV3Downloader.dll";
}
