using Mono.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Mono.ApiTools
{
	public class DiffCommand : BaseCommand
	{
		private const int DefaultSaveBufferSize = 1024;

		private static readonly Encoding UTF8NoBOM = new UTF8Encoding(false, true);

		public DiffCommand()
			: base("diff", "ASSEMBLY1 ASSEMBLY2", "Compare two assemblies.")
		{
		}

		public List<string> Assemblies { get; set; } = new List<string>();

		public string OutputDirectory { get; set; }

		protected override OptionSet OnCreateOptions() => new OptionSet
		{
			{ "output=", "The output directory", v => OutputDirectory = v },
		};

		protected override bool OnValidateArguments(IEnumerable<string> extras)
		{
			var hasError = false;

			var assemblies = extras.Where(p => !string.IsNullOrEmpty(p)).ToArray();

			foreach (var ass in assemblies)
			{
				if (File.Exists(ass))
				{
					Assemblies.Add(ass);
				}
				else
				{
					Console.Error.WriteLine($"{Program.Name}: Assembly does not exist: `{ass}`.");
					hasError = true;
				}
			}

			if (Assemblies.Count == 0)
			{
				Console.Error.WriteLine($"{Program.Name}: At least one assembly is required.");
				hasError = true;
			}

			if (Assemblies.Count != 2)
			{
				Console.Error.WriteLine($"{Program.Name}: Exactly two assemblies are required.");
				hasError = true;
			}

			if (string.IsNullOrEmpty(OutputDirectory))
				OutputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "diff");

			return !hasError;
		}

		protected override bool OnInvoke(IEnumerable<string> extras)
		{
			if (!Directory.Exists(OutputDirectory))
				Directory.CreateDirectory(OutputDirectory);

			Console.WriteLine($"Running a diff on '{Assemblies[0]}' vs '{Assemblies[1]}'...");

			using (var oldStream = File.OpenRead(Assemblies[0]))
			using (var newStream = File.OpenRead(Assemblies[1]))
			{
				DiffAssembliesAsync(newStream, oldStream).Wait();
			}

			return true;
		}

		private async Task DiffAssembliesAsync(Stream newStream, Stream oldStream)
		{
			// create the api xml
			var oldApiXml = GenerateAssemblyApiInfo(oldStream);
			var newApiXml = GenerateAssemblyApiInfo(newStream);

			// make sure the assembly names are the same for the comparison
			string assemblyName;
			(newApiXml, assemblyName) = await RenameAssemblyAsync(oldApiXml, newApiXml);

			using (var diffStream = GenerateDiff(oldApiXml, newApiXml, false))
			{
				await SaveDiffAsync(diffStream, $"{assemblyName}.diff.md");
			}

			using (var diffStream = GenerateDiff(oldApiXml, newApiXml, true))
			{
				await SaveDiffAsync(diffStream, $"{assemblyName}.breaking.md");
			}

			// we are done
			Console.WriteLine($"Diff complete of '{assemblyName}'.");
		}

		private async Task SaveDiffAsync(Stream diffStream, string filename)
		{
			// TODO: there are two bugs in this version of mono-api-html
			using var md = new StreamReader(diffStream);

			var contents = await md.ReadToEndAsync();

			// 1. the <h4> doesn't look pretty in the markdown
			contents = contents.Replace("<h4>", "> ");
			contents = contents.Replace("</h4>", Environment.NewLine);

			// 2. newlines are inccorrect on Windows: https://github.com/mono/mono/pull/9918
			contents = contents.Replace("\r\r", "\r");

			await File.WriteAllTextAsync(Path.Combine(OutputDirectory, filename), contents);
		}

		private Stream GenerateAssemblyApiInfo(Stream assemblyStream)
		{
			var config = new ApiInfoConfig
			{
				IgnoreResolutionErrors = true
			};

			var info = new MemoryStream();

			using (var writer = new StreamWriter(info, UTF8NoBOM, DefaultSaveBufferSize, true))
			{
				ApiInfo.Generate(assemblyStream, writer, config);
			}

			assemblyStream.Position = 0;
			info.Position = 0;

			return info;
		}

		private Stream GenerateDiff(Stream oldApiXml, Stream newApiXml, bool ignoreNonbreaking)
		{
			var config = new ApiDiffFormattedConfig
			{
				Formatter = ApiDiffFormatter.Markdown,
				IgnoreNonbreaking = ignoreNonbreaking
			};

			var diff = new MemoryStream();

			using (var writer = new StreamWriter(diff, UTF8NoBOM, DefaultSaveBufferSize, true))
			{
				ApiDiffFormatted.Generate(oldApiXml, newApiXml, writer, config);
			}

			oldApiXml.Position = 0;
			newApiXml.Position = 0;
			diff.Position = 0;

			return diff;
		}

		private async Task<(Stream, string)> RenameAssemblyAsync(Stream oldApiXml, Stream newApiXml, CancellationToken cancellationToken = default)
		{
			var oldDoc = await XDocument.LoadAsync(oldApiXml, LoadOptions.None, cancellationToken);
			var assemblyName = oldDoc.Root.Element("assembly").Attribute("name").Value;
			oldApiXml.Position = 0;

			var newDoc = await XDocument.LoadAsync(newApiXml, LoadOptions.None, cancellationToken);
			var newAssembly = newDoc.Root.Element("assembly");
			var newName = newAssembly.Attribute("name");

			if (newName.Value != assemblyName)
			{
				Console.WriteLine($"WARNING: Assembly name changed from '{assemblyName}' to '{newName.Value}'.");
				newName.Value = assemblyName;

				newApiXml.Dispose();

				newApiXml = new MemoryStream();
				await newDoc.SaveAsync(newApiXml, SaveOptions.None, cancellationToken);
			}

			newApiXml.Position = 0;

			return (newApiXml, assemblyName);
		}
	}
}
