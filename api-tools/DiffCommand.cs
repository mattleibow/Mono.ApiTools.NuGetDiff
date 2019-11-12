using Mono.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
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

		public string OutputPath { get; set; }

		public bool IgnoreNonbreaking { get; set; }

		protected override OptionSet OnCreateOptions() => new OptionSet
		{
			{ "o|output=", "The output file path", v => OutputPath = v },
			{ "ignore-nonbreaking", "Ignore the non-breaking changes and just output breaking changes", v => IgnoreNonbreaking = true },
		};

		protected override bool OnValidateArguments(IEnumerable<string> extras)
		{
			var hasError = false;

			var assemblies = extras.Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();

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

			if (Assemblies.Count != 2)
			{
				Console.Error.WriteLine($"{Program.Name}: Exactly two assemblies are required.");
				hasError = true;
			}

			if (!string.IsNullOrWhiteSpace(OutputPath))
			{
				var dir = Path.GetDirectoryName(OutputPath);
				if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
					Directory.CreateDirectory(dir);
			}

			return !hasError;
		}

		protected override bool OnInvoke(IEnumerable<string> extras)
		{
			if (Program.Verbose)
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

			// generate the diff
			using var diffStream = GenerateDiff(oldApiXml, newApiXml, assemblyName);
			await FixBugsAsync(diffStream);

			if (!string.IsNullOrWhiteSpace(OutputPath))
			{
				// write the file
				using var file = File.Create(OutputPath);
				await diffStream.CopyToAsync(file);
			}
			else
			{
				// write to console out
				using var md = new StreamReader(diffStream);
				var contents = await md.ReadToEndAsync();
				await Console.Out.WriteAsync(contents);
			}

			// we are done
			if (Program.Verbose)
				Console.WriteLine($"Diff complete of '{assemblyName}'.");
		}

		private async Task FixBugsAsync(Stream diffStream)
		{
			// TODO: there are two bugs in this version of mono-api-html
			string contents;
			using (var md = new StreamReader(diffStream, UTF8NoBOM, false, DefaultSaveBufferSize, true))
			{
				contents = await md.ReadToEndAsync();
			}

			// 1. the <h4> doesn't look pretty in the markdown
			contents = contents.Replace("<h4>", "> ");
			contents = contents.Replace("</h4>", Environment.NewLine);

			// 2. newlines are inccorrect on Windows: https://github.com/mono/mono/pull/9918
			contents = contents.Replace("\r\r", "\r");

			// write the contents back to the stream
			diffStream.SetLength(0);
			using (var writer = new StreamWriter(diffStream, UTF8NoBOM, DefaultSaveBufferSize, true))
			{
				writer.Write(contents);
			}
			diffStream.Position = 0;
		}

		private Stream GenerateAssemblyApiInfo(Stream assemblyStream)
		{
			// try loading the file as an assembly, and then create the API info
			try
			{
				assemblyStream.Position = 0;

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
			catch (BadImageFormatException)
			{
			}

			// try loading as an API info
			try
			{
				assemblyStream.Position = 0;

				var xdoc = XDocument.Load(assemblyStream);

				assemblyStream.Position = 0;

				return assemblyStream;
			}
			catch (XmlException)
			{
			}

			throw new InvalidOperationException("Input was in an incorrect format.");
		}

		private Stream GenerateDiff(Stream oldApiXml, Stream newApiXml, string assemblyName)
		{
			var config = new ApiDiffFormattedConfig
			{
				Formatter = ApiDiffFormatter.Markdown,
				IgnoreNonbreaking = IgnoreNonbreaking
			};

			var diff = new MemoryStream();

			using (var writer = new StreamWriter(diff, UTF8NoBOM, DefaultSaveBufferSize, true))
			{
				ApiDiffFormatted.Generate(oldApiXml, newApiXml, writer, config);
			}

			if (diff.Length == 0)
			{
				using var writer = new StreamWriter(diff, UTF8NoBOM, DefaultSaveBufferSize, true);
				writer.WriteLine($"# API diff: {assemblyName}.dll");
				writer.WriteLine();
				writer.WriteLine($"## {assemblyName}.dll");
				writer.WriteLine();
				writer.WriteLine($"> No changes.");
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
				if (Program.Verbose)
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
