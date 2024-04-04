using System.Diagnostics;
using System.Xml.Linq;
using Xunit.Abstractions;

namespace Mono.ApiTools.Tests;

public class AllTestCases : IDisposable
{
	private readonly string workingDirectory;

	public AllTestCases(ITestOutputHelper output)
	{
		Output = output;

		workingDirectory = Path.Combine(Path.GetTempPath(), GetType().FullName!, Guid.NewGuid().ToString("N"));

		if (Directory.Exists(workingDirectory))
			Directory.Delete(workingDirectory, true);

		Directory.CreateDirectory(workingDirectory);

		Output.WriteLine("Working Directory:");
		Output.WriteLine(workingDirectory);
		Output.WriteLine("");
	}

	public ITestOutputHelper Output { get; }

	public void Dispose()
	{
		Directory.Delete(workingDirectory, true);
	}

	public static IEnumerable<object[]> GetTestCases() =>
		new DirectoryInfo("TestCases")
			.EnumerateDirectories()
			.Where(f => f.EnumerateFiles().Any())
			.Select(x => new object[] { x.Name });

	[Theory]
	[MemberData(nameof(GetTestCases))]
	public void Test(string name)
	{
		var testCasePath = Path.Combine(workingDirectory, name);
		var dllPath = Path.Combine(testCasePath, "bin/Debug/net8.0/Project.dll");
		var infoPath = Path.Combine(testCasePath, "ApiInfo.xml");

		// copy all the files to a temporary folder
		CopyDirectory(Path.Combine("TestCases", name), testCasePath);

		// run the build
		var build = Process.Start(new ProcessStartInfo
		{
			FileName = "dotnet",
			Arguments = "build -c Debug",
			RedirectStandardOutput = true,
			WorkingDirectory = testCasePath,
		})!;
		build.WaitForExit();

		var stdOut = build.StandardOutput.ReadToEnd();
		Output.WriteLine("Build Output:");
		Output.WriteLine(stdOut);
		Output.WriteLine("");

		Assert.Equal(0, build.ExitCode);

		// generate the API info
		using var writer = new Utf8StringWriter();
		using (var assemblyStream = File.OpenRead(dllPath))
		{
			ApiInfo.Generate(assemblyStream, writer, new());
		}
		var actualInfo = writer.ToString();

		Output.WriteLine("Actual Info:");
		Output.WriteLine(actualInfo);

		// read the expected API info
		var expectedInfo = File.ReadAllText(infoPath);

		// compare the generated API info with the expected one
		var xExpected = XDocument.Parse(expectedInfo);
		var xActual = XDocument.Parse(actualInfo);
		var isSame = XNode.DeepEquals(xExpected, xActual);
		if (!isSame)
		{
			Assert.Equal(expectedInfo, actualInfo);
		}
	}

	private static void CopyDirectory(string sourceDir, string destinationDir)
	{
		var dir = new DirectoryInfo(sourceDir);
		if (!dir.Exists)
			throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

		Directory.CreateDirectory(destinationDir);

		foreach (var file in dir.EnumerateFiles())
		{
			var targetFilePath = Path.Combine(destinationDir, file.Name);
			file.CopyTo(targetFilePath);
		}

		foreach (var subDir in dir.EnumerateDirectories())
		{
			var newDestinationDir = Path.Combine(destinationDir, subDir.Name);
			CopyDirectory(subDir.FullName, newDestinationDir);
		}
	}
}
